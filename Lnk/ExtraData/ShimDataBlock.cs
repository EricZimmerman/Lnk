using System;

namespace Lnk.ExtraData;

public class ShimDataBlock : ExtraDataBase
{
    public ShimDataBlock(byte[] rawBytes)
    {
        RequireSize(rawBytes, 0x88, "ShimDataBlock");

        Signature = ExtraDataTypes.ShimDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        LayerName = Helpers.ReadUnicodeZ(rawBytes, 8, "LayerName");
    }

    public string LayerName { get; }

    public override string ToString()
    {
        return $"Shimcache data block" +
               $"\r\nLayerName: {LayerName}";
    }
}