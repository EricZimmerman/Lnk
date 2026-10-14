using System;

namespace Lnk.ExtraData;

public class IconEnvironmentDataBlock : ExtraDataBase
{
    public IconEnvironmentDataBlock(byte[] rawBytes, int codepage)
    {
        RequireSize(rawBytes, 0x314, "IconEnvironmentDataBlock");

        Signature = ExtraDataTypes.IconEnvironmentDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        IconPathAscii = Helpers.ReadAnsiZ(rawBytes, 8, Helpers.GetEncoding(codepage), "TargetAnsi", 8 + 260);
        IconPathUni = Helpers.ReadUnicodeZ(rawBytes, 268, "TargetUnicode", 268 + 520);
    }

    public string IconPathAscii { get; }
    public string IconPathUni { get; }


    public override string ToString()
    {
        return $"Icon environment data block" +
               $"\r\nIcon path: {IconPathAscii}" +
               $"\r\nIcon path (Unicode): {IconPathUni}";
    }
}