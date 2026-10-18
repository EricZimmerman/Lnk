using System;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ExtraData;

public class PropertyStoreDataBlock : ExtraDataBase
{
    public PropertyStoreDataBlock(byte[] rawBytes)
    {
        RequireSize(rawBytes, 0x0C, "PropertyStoreDataBlock");

        Signature = ExtraDataTypes.PropertyStoreDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        var propBytes = new byte[rawBytes.Length - 8];
        Buffer.BlockCopy(rawBytes, 8, propBytes, 0, propBytes.Length);

        PropertyStore = new PropertyStore(propBytes);
    }

    public PropertyStore PropertyStore { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (PropertyStore.Sheets.Count > 0)
        {
            sb.AppendLine("Property store data block (Format: GUID\\ID Description ==> Value)");

            var propCount = 0;

            foreach (var prop in PropertyStore.Sheets)
            {
                foreach (var propertyName in prop.PropertyNames)
                {
                    propCount += 1;

                    var prefix = $"{prop.GUID}\\{propertyName.Key}".PadRight(43);

                    var suffix =
                        //named sheets have the name as the key (int.Parse threw, and ToString with it)
                        $"{(int.TryParse(propertyName.Key, out var key) ? Utils.GetDescriptionFromGuidAndKey(prop.GUID, key) : propertyName.Key)}"
                            .PadRight(35);

                    sb.AppendLine($"{prefix} {suffix} ==> {propertyName.Value}");
                }
            }

            if (propCount == 0)
            {
                sb.AppendLine("(Property store is empty)");
            }
        }
        return sb.ToString();
    }
}