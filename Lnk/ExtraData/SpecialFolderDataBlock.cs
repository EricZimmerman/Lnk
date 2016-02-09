using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
public    class SpecialFolderDataBlock:ExtraDataBase
    {

        public uint SpecialFolderID { get; }
        public uint Offset { get; }

    public SpecialFolderDataBlock(byte[] rawBytes)
    {
            Signature = ExtraDataTypes.SpecialFolderDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

        SpecialFolderID = BitConverter.ToUInt32(rawBytes, 8);
        Offset = BitConverter.ToUInt32(rawBytes, 12);

    }

        public override string ToString()
        {
            return $"Size: {Size}, SpecialFolderID: {SpecialFolderID}, Offset: {Offset}";
        }

    }
}
