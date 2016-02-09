using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0X31 : ShellBag
    {
        public ShellBag0X31(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            FriendlyName = "Directory";

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

            if (bagPath.Contains(@"BagMRU\0\11\20") && slot == 98)
            {
                Debug.WriteLine("In 0x31 trap");
            }

            //if (bagPath.Contains(@"BagMRU\0\11\20\97") && slot == 2)
            //{
            //    Debug.WriteLine(1);
            //}

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            int index = 2;
            if ((rawBytes[0x27] == 0x00 && rawBytes[0x28] == 0x2f && rawBytes[0x29] == 0x00) || (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41))
            {
                //we have a good date

                try
                {
                    var zip = new ShellBagZipContents(Slot, MruPosition, rawBytes, BagPath);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }
                catch (Exception)
                {
                    //this isnt really this kind of entry despite our best efforts to make sure it is, so fall thru and process normally
                }
            }

            index += 1;

            //skip unknown byte
            index += 1;

            index += 4; // skip file size since always 0 for directory

            LastModificationTime = Utils.ExtractDateTimeOffsetFromBytes(rawBytes.Skip(index).Take(4).ToArray());

            //    SiAuto.Main.LogMessage("Got last modified time: {0}", LastModificationTime);

            index += 4;

            //    string fileAttribues = BitConverter.ToString(rawBytes, index, 2);

            //     SiAuto.Main.LogMessage("fileAttribues: {0}", fileAttribues);

            index += 2;

            int len = 0;

            //SiAuto.Main.LogMessage("Walking out 0s");

            var beefPos = BitConverter.ToString(rawBytes).IndexOf("04-00-EF-BE", StringComparison.InvariantCulture) / 3;
            beefPos = beefPos - 4; //add header back for beef

            var strLen = beefPos - index;

            if (rawBytes[2] == 0x35)
            {
//                while (rawBytes[index + len] != 0x0 || rawBytes[index + len + 1] != 0x0)
//                {
//                    len += 1;
//                }
                len = strLen;
            }
            else
            {
             
            while (rawBytes[index + len] != 0x0)
            {
                len += 1;
            }   
            }


            var tempBytes = new byte[len];
            Array.Copy(rawBytes, index, tempBytes, 0, len);

            index += len;

            string shortName = "";

            if (rawBytes[2] == 0x35)
            {
                shortName = Encoding.Unicode.GetString(tempBytes);
            }
            else
            {
                shortName = Encoding.ASCII.GetString(tempBytes);
            }
            
            ShortName = shortName;

            Value = shortName;

            //       SiAuto.Main.LogMessage("shortName: {0} in {1}", shortName, bagPath);

            //SiAuto.Main.LogMessage("Walking out 0s");
            while (rawBytes[index] == 0x0)
            {
                index += 1;
            }

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

                if (block.Signature.ToString("X").StartsWith("BEEF00"))
                {
                    ExtensionBlocks.Add(block);    
                }
                else
                {
                    //SiAuto.Main.LogWarning("In 0x31, found a signature for extension block not containing beef. {0}",block.Signature.ToString("X"));
                }

                

                var beef0004 = block as Beef0004;
                if (beef0004 != null)
                {
                    Value = beef0004.LongName;
                }

                var beef0005 = block as Beef0005;
                if (beef0005 != null)
                {
                    //TODO Resolve this
//                    foreach (var internalBag in beef0005.InternalBags)
//                    {
//                        ExtensionBlocks.Add(new BeefPlaceHolder(null));
//
//
//                    }

                   
                }

                index += extsize;
            }
        }

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

        public string ShortName { get; private set; }

    

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (ShortName.Length > 0)
            {
                sb.AppendLine($"Short name: {ShortName}");
            }

            if (LastModificationTime.HasValue)
            {
                sb.AppendLine($"Modified: {LastModificationTime.Value}");
            }

            if (LastAccessTime.HasValue)
            {
                sb.AppendLine($"Last Access: {LastAccessTime.Value}");
            }

            sb.AppendLine();
            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}