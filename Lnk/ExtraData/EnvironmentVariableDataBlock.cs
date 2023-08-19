using System;
using System.Linq;
using System.Text;

namespace Lnk.ExtraData;

public class EnvironmentVariableDataBlock : ExtraDataBase
{
    public EnvironmentVariableDataBlock(byte[] rawBytes, int codepage=1252)
    {
        Signature = ExtraDataTypes.EnvironmentVariableDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        EnvironmentVariablesAscii = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, 8, 260).Split('\0').First();
        EnvironmentVariablesUnicode = Encoding.Unicode.GetString(rawBytes, 268, 520).Split('\0').First();
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