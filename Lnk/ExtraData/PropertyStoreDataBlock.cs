using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtensionBlocks;

namespace Lnk.ExtraData
{
public    class PropertyStoreDataBlock:ExtraDataBase
    {

        public PropertyStore PropertyStore { get; }

    public PropertyStoreDataBlock(byte[] rawBytes)
    {
            Signature = ExtraDataTypes.PropertyStoreDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

        var propBytes = new byte[Size - 8];
            Buffer.BlockCopy(rawBytes,8,propBytes,0,(int) Size - 8);

            PropertyStore = new PropertyStore(propBytes);

    }

    public override string ToString()
    {
        return $"Size: {Size}, Property store: {PropertyStore}";
    }
    }
}
