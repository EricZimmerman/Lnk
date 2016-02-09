using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
    public class ConsoleFEDataBlock:ExtraDataBase
    {
        public uint CodePage { get; }
        public ConsoleFEDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.ConsoleFEDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            CodePage = BitConverter.ToUInt32(rawBytes, 8);
        }

        public override string ToString()
        {
            return $"Size: {Size}, CodePage: {CodePage}";
        }
    }
}
