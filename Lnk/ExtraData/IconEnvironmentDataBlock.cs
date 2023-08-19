using System;
using System.Linq;
using System.Text;

namespace Lnk.ExtraData;

public class IconEnvironmentDataBlock : ExtraDataBase
{
    public IconEnvironmentDataBlock(byte[] rawBytes, int codepage)
    {
        Signature = ExtraDataTypes.IconEnvironmentDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        IconPathAscii = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, 8, 260).Split('\0').First();
        IconPathUni = Encoding.Unicode.GetString(rawBytes, 268, 520).Split('\0').First();
    }

    public string IconPathAscii { get; }
    public string IconPathUni { get; }


    public override string ToString()
    {
        return $"Icon environment data block" +
               $"\r\nIcon path: {IconPathAscii}";
    }
}