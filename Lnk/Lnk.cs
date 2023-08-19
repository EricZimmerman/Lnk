using System;
using System.IO;

namespace Lnk;

public static class Lnk
{
    /// <summary>
    /// Helper to load lnk files
    /// </summary>
    /// <param name="lnkFile"></param>
    /// <returns>LnkFile</returns>
    /// <exception cref="Exception"></exception>
    public static LnkFile LoadFile(string lnkFile, int codepage=1252)
    {
        var raw = File.ReadAllBytes(lnkFile);

        if (raw[0] != 0x4c)
        {
            throw new Exception($"Invalid signature!");
        }

        return new LnkFile(raw, lnkFile, codepage);
    }
}