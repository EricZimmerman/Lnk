using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
public    class ShimDataBlock:ExtraDataBase
    {
        public string LayerName { get; }

    public ShimDataBlock(byte[] rawBytes)
    {
            Signature = ExtraDataTypes.ShimDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

        LayerName = Encoding.Unicode.GetString(rawBytes, 8, rawBytes.Length - 8).Split('\0').First();

    }
        public override string ToString()
        {
            return $"Size: {Size}, LayerName: {LayerName}";
        }
    }
}
