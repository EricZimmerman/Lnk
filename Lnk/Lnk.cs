using System;
using System.IO;

namespace Lnk;

public static class Lnk
{
    /// <summary>
    /// Helper to load lnk files
    /// </summary>
    /// <param name="lnkFile">Path of the LNK file</param>
    /// <param name="codepage">Code page used for non-Unicode strings</param>
    /// <returns>LnkFile</returns>
    /// <exception cref="LnkParseException">The file is not a valid LNK file or cannot be parsed</exception>
    public static LnkFile LoadFile(string lnkFile, int codepage=1252)
    {
        var raw = File.ReadAllBytes(lnkFile);

        // validate the source file has at least 76 bytes of header data
        if (raw.Length < Header.HeaderSize)
        {
            throw new LnkParseException($"File ({lnkFile}) is less than 76 bytes which is too small to be a valid LNK file!", 0, "ShellLinkHeader");
        }

        // HeaderSize MUST be 0x0000004C
        if (BitConverter.ToUInt32(raw, 0) != Header.HeaderSize)
        {
            throw new LnkParseException($"File ({lnkFile}) has an invalid signature! Is it a valid LNK file?", 0, "HeaderSize");
        }

        return new LnkFile(raw, lnkFile, codepage);
    }
}
