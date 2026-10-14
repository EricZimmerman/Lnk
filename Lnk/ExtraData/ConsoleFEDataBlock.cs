using System;

namespace Lnk.ExtraData;

public class ConsoleFeDataBlock : ExtraDataBase
{
    public ConsoleFeDataBlock(byte[] rawBytes)
    {
        RequireSize(rawBytes, 0x0C, "ConsoleFEDataBlock");

        Signature = ExtraDataTypes.ConsoleFeDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        CodePage = BitConverter.ToUInt32(rawBytes, 8);
    }

    public uint CodePage { get; }

    public override string ToString()
    {
        return $"Console FE data block" +
               $"\r\nCodePage: {CodePage}";
    }
}