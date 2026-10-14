using System;

namespace Lnk.ExtraData;

public class EnvironmentVariableDataBlock : ExtraDataBase
{
    public EnvironmentVariableDataBlock(byte[] rawBytes, int codepage=1252)
    {
        RequireSize(rawBytes, 0x314, "EnvironmentVariableDataBlock");

        Signature = ExtraDataTypes.EnvironmentVariableDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        EnvironmentVariablesAscii = Helpers.ReadAnsiZ(rawBytes, 8, Helpers.GetEncoding(codepage), "TargetAnsi", 8 + 260);
        EnvironmentVariablesUnicode = Helpers.ReadUnicodeZ(rawBytes, 268, "TargetUnicode", 268 + 520);
    }

    public string EnvironmentVariablesAscii { get; }
    public string EnvironmentVariablesUnicode { get; }


    public override string ToString()
    {
        return $"Environment variable data block" +
               $"\r\nEnvironment variables Ascii: {EnvironmentVariablesAscii}" +
               $"\r\nEnvironment variables unicode: {EnvironmentVariablesUnicode}";
    }
}