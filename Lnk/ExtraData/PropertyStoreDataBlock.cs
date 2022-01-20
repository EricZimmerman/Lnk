using System;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ExtraData;

public class PropertyStoreDataBlock : ExtraDataBase
{
    public PropertyStoreDataBlock(byte[] rawBytes)
    {
        Signature = ExtraDataTypes.PropertyStoreDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        var propBytes = new byte[Size - 8];
        Buffer.BlockCopy(rawBytes, 8, propBytes, 0, (int) Size - 8);

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
                        $"{Utils.GetDescriptionFromGuidAndKey(prop.GUID, int.Parse(propertyName.Key))}"
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