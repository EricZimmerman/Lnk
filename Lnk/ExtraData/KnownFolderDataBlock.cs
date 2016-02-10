using System;
using ExtensionBlocks;

namespace Lnk.ExtraData
{
    public class KnownFolderDataBlock : ExtraDataBase
    {
        public KnownFolderDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.KnownFolderDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            var kfBytes = new byte[16];
            Buffer.BlockCopy(rawBytes, 8, kfBytes, 0, 16);

            KnownFolderID = new Guid(kfBytes);

            KnownFolderName = Utils.GetFolderNameFromGuid(KnownFolderID.ToString());

            Offset = BitConverter.ToUInt32(rawBytes, 24);
        }

        public uint Offset { get; }

        public Guid KnownFolderID { get; }
        public string KnownFolderName { get; }

        public override string ToString()
        {
            return $"Known folder data block" +
                   $"\r\nKnown folder GUID: {KnownFolderID} ({KnownFolderName})";
            }
    }
}