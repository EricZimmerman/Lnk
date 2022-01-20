using System;
using System.IO;

namespace Lnk;

public static class Lnk
{
    public static LnkFile LoadFile(string lnkFile)
    {
        var raw = File.ReadAllBytes(lnkFile);

        if (raw[0] != 0x4c)
        {
            throw new Exception($"Invalid signature!");
        }

        return new LnkFile(raw, lnkFile);
    }
}