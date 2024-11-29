using System;
using System.IO;

namespace Lnk;

public static class Lnk
{
    /// <summary>
    /// Helper to load lnk files
    /// </summary>
    /// <param name="lnkFile"></param>
    /// <param name="codepage"></param>
    /// <returns>LnkFile</returns>
    /// <exception cref="Exception"></exception>
    public static LnkFile LoadFile(string lnkFile, int codepage=1252)
    {
        var raw = File.ReadAllBytes(lnkFile);

        if (raw[0] != 0x4c)
        {
            throw new Exception($"File ({lnkFile}) has an invalid signature! Is it a valid LNK file?");
        }

        // validate the source file has at least 76 bytes of header data
        if (raw.Length < 76)
        {
            throw new Exception($"File ({lnkFile}) is less than 76 bytes which is too small to be a valid LNK file!");
        }

        return new LnkFile(raw, lnkFile, codepage);
    }
}