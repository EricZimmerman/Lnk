using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0x74 : ShellBag
    {
        public ShellBag0x74(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            FriendlyName = "Users Files Folder";

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            int index = 2;

            index += 2; // move past type  and an unknown

            ushort size = BitConverter.ToUInt16(rawBytes, index);

            index += 2;

            string sig74 = Encoding.ASCII.GetString(rawBytes, index, 4);

            if (sig74 == "CF\0\0")
            {
                if (rawBytes[0x28] == 0x2f || (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41))
                {
                    //we have a good date

                    var zip = new ShellBagZipContents(Slot, MruPosition, rawBytes, BagPath);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }
            }

            if (sig74 != "CFSF")
            {
                throw new Exception($"Invalid signature! Should be CFSF but was {sig74}");
            }

            index += 4;

            ushort subShellSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            byte subClasstype = rawBytes[index];
            index += 1;

            index += 1; // skip unknown

            uint filesize = BitConverter.ToUInt32(rawBytes, index);

            index += 4;

            FileSize = (int)filesize;

            var tempBytes = new byte[4];
            Array.Copy(rawBytes, index, tempBytes, 0, 4);

            index += 4;

            LastModificationTime = Utils.ExtractDateTimeOffsetFromBytes(tempBytes);

            index += 2; //skip file attribute flag

            int len = 0;

            //       SiAuto.Main.LogMessage("Walking out 0s to find end of name");
            while (rawBytes[index + len] != 0x0)
            {
                len += 1;
            }

            tempBytes = new byte[len];
            Array.Copy(rawBytes, index, tempBytes, 0, len);

            index += len;

            string primaryName = Encoding.ASCII.GetString(tempBytes);

            //   SiAuto.Main.LogMessage("shortName: {0}", primaryName);

            //     SiAuto.Main.LogMessage("Walking out 0s to next section");
            while (rawBytes[index] == 0x0)
            {
                index += 1;
            }

            var delegateGuidRaw = new byte[16];

            Array.Copy(rawBytes, index, delegateGuidRaw, 0, 16);

            //     SiAuto.Main.LogArray("delegateGuid", delegateGuidRaw);

            string delegateGuid = Utils.ExtractGuidFromShellItem((delegateGuidRaw));

            //      SiAuto.Main.LogMessage("delegateGuid after ExtractGUIDFromShellItem: {0}", delegateGuid);

            //5e591a74-df96-48d3-8d67-1733bcee28ba

            if (delegateGuid != "5e591a74-df96-48d3-8d67-1733bcee28ba")
            {
                throw new Exception(
                    $"Delegate guid not expected value of 5e591a74-df96-48d3-8d67-1733bcee28ba. Actual value: {delegateGuid}");
            }

            index += 16;

            var itemIdentifierGuidRaw = new byte[16];
            Array.Copy(rawBytes, index, itemIdentifierGuidRaw, 0, 16);

            string itemIdentifierGuid = Utils.ExtractGuidFromShellItem((itemIdentifierGuidRaw));

            string itemName = Utils.GetFolderNameFromGuid(itemIdentifierGuid);
            index += 16;

            //0xbeef0004 section

            //we are at extensnon blocks, so cut them up and process
            // here is where we need to cut up the rest into extension blocks
            var chunks = new List<byte[]>();

            while (index < rawBytes.Length)
            {
                short subshellitemdatasize = BitConverter.ToInt16(rawBytes, index);

                if (subshellitemdatasize == 0)
                {
                    break;
                }

                if (subshellitemdatasize == 1)
                {
                    //some kind of separator
                    index += 2;
                }
                else
                {
                    chunks.Add(rawBytes.Skip(index).Take(subshellitemdatasize).ToArray());
                    index += subshellitemdatasize;
                }
            }

            foreach (var bytes in chunks)
            {
                index = 0;

                short extsize = BitConverter.ToInt16(bytes, index);

                var signature = BitConverter.ToUInt32(bytes, 0x04);

                //TODO does this need to check if its a 0xbeef?? regex?
                var block = Utils.GetExtensionBlockFromBytes(signature, bytes);

                ExtensionBlocks.Add(block);

                var beef0004 = block as Beef0004;
                if (beef0004 != null)
                {
                    Value = beef0004.LongName;
                }

                index += extsize;
            }
        }

        public int FileSize { get; private set; }

        /// <summary>
        ///     last modified time of BagPath
        /// </summary>
        public DateTimeOffset? LastModificationTime { get; set; }

        ///// <summary>
        /////     Created time of BagPath
        ///// </summary>
        //public DateTimeOffset? CreatedOnTime { get; set; }

        /// <summary>
        ///     Last access time of BagPath
        /// </summary>
        public DateTimeOffset? LastAccessTime { get; set; }

        ///// <summary>
        /////     For files and directories, the MFT entry #
        ///// </summary>
        //public long? MFTEntryNumber { get; set; }

        ///// <summary>
        /////     For files and directories, the MFT sequence #
        ///// </summary>
        //public int? MFTSequenceNumber { get; set; }

        

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (FileSize > 0)
            {
                sb.AppendLine($"File size: {FileSize:N0}");
            }

            if (LastModificationTime.HasValue)
            {
                sb.AppendLine($"Modified On: {LastModificationTime.Value}");
                sb.AppendLine();
            }

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}