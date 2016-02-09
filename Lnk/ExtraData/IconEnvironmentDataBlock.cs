using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
   public class IconEnvironmentDataBlock:ExtraDataBase
    {
        public string IconPathAscii { get; }
        public string IconPathUni { get; }

        public IconEnvironmentDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.IconEnvironmentDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            IconPathAscii = Encoding.GetEncoding(1252).GetString(rawBytes, 8, 260).Split('\0').First();
            IconPathUni = Encoding.Unicode.GetString(rawBytes, 268, 520).Split('\0').First();
        }


        public override string ToString()
        {
            return $"Size: {Size}, Icon path Ascii: {IconPathAscii}, Icon path Unicode: {IconPathUni}";
        }
    }
}
