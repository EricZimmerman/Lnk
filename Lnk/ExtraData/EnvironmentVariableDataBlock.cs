using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
    public class EnvironmentVariableDataBlock:ExtraDataBase
    {
        public string EnvironmentVariablesAscii { get; }
        public string EnvironmentVariablesUnicode { get; }

        public EnvironmentVariableDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.EnvironmentVariableDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            EnvironmentVariablesAscii = Encoding.GetEncoding(1252).GetString(rawBytes, 8, 260).Split('\0').First();
            EnvironmentVariablesUnicode = Encoding.Unicode.GetString(rawBytes, 268, 520).Split('\0').First();
        }


        public override string ToString()
        {
            return $"Size: {Size}, EnvVarsAscii: {EnvironmentVariablesAscii}, EnvVarsdUnicode: {EnvironmentVariablesUnicode}";
        }
    }
}
