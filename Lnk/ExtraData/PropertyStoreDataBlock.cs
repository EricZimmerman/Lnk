using System;
using System.Runtime.Remoting.Messaging;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ExtraData
{
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

            sb.AppendLine("Property store data block");

            foreach (var prop in PropertyStore.Sheets)
            {
                foreach (var propertyName in prop.PropertyNames)
                {
                    sb.AppendLine(
                        $"{prop.GUID}\\{propertyName.Key} {Utils.GetDescriptionFromGuidAndKey(prop.GUID, int.Parse(propertyName.Key))} ==> {propertyName.Value}");
                }

            }

            return sb.ToString();
        }
    }
}