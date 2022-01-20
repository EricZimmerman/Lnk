using System;
using System.Linq;
using System.Text;

namespace Lnk.ExtraData;

public class ShimDataBlock : ExtraDataBase
{
    public ShimDataBlock(byte[] rawBytes)
    {
        Signature = ExtraDataTypes.ShimDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        LayerName = Encoding.Unicode.GetString(rawBytes, 8, rawBytes.Length - 8).Split('\0').First();
    }

    public string LayerName { get; }

    public override string ToString()
    {
        return $"Shimcache data block" +
               $"\r\nLayerName: {LayerName}";
    }
}