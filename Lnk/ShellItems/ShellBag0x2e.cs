using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    public class ShellBag0x2e : ShellBag
    {
        public ShellBag0x2e(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            //if (bagPath.Contains(@"BagMRU\2") && slot == 5)
            //{
            //    Debug.WriteLine("At trap for certain bag in 0x71 bag");
            //}

            BagPath = bagPath;

            var index = 0;

            var postSig = BitConverter.ToInt64(rawBytes, rawBytes.Length - 8);

            if (postSig == 0x0000ee306bfe9555)
            {
                FriendlyName = "User profile";

                var la = Utils.ExtractDateTimeOffsetFromBytes(rawBytes.Skip(rawBytes.Length - 14).Take(4).ToArray());

                LastAccessTime = la;

                index = 10;

                var tempString = Encoding.Unicode.GetString(rawBytes.Skip(index).ToArray()).Split('\0')[0];

                Value = tempString;

                return;
            }

            //this needs to change to be the default
            if (rawBytes[0] == 20 || rawBytes[0] == 50 || rawBytes[0] == 0x3a)
            {
                ProcessGuid(rawBytes);
                return;
            }

            //this needs to change to be the default
            if (rawBytes[2] == 0x53)
            {
                ProcessGuid2(rawBytes);
                return;
            }

            //zip file contents check
            if (rawBytes[0x28] == 0x2f || (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41))
            {
                //we have a good date

                var zip = new ShellBagZipContents(Slot, MruPosition, rawBytes, BagPath);
                FriendlyName = zip.FriendlyName;
                LastAccessTime = zip.LastAccessTime;

                Value = zip.Value;

                return;
            }


            try
            {
                ProcessPropertyViewDefault(rawBytes);

                return;
            }
            catch (Exception)
            {
            }

            //this is a different animal,

            FriendlyName = "Root folder: MPT device";

            index = 0x1e;

            var storageStringNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var storageIDStringLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var fileSystemNameLen = BitConverter.ToInt32(rawBytes, index);

            index = 0x28;

            var storageName = Encoding.Unicode.GetString(rawBytes, index, storageStringNameLen*2 - 2);

            index += storageStringNameLen*2;

            var storageIDName = Encoding.Unicode.GetString(rawBytes, index, storageIDStringLen*2 - 2);

            index += storageIDStringLen*2;

            Value = storageName;
        }

        /// <summary>
        ///     Last access time of BagPath
        /// </summary>
        public DateTimeOffset? LastAccessTime { get; set; }

        public PropertyStore PropertyStore { get; private set; }

        private void ProcessPropertyViewDefault(byte[] rawBytes)
        {
            FriendlyName = "Variable: Users property view";
            var index = 10;

            var shellPropertySheetListSize = BitConverter.ToInt16(rawBytes, index);

            //       SiAuto.Main.LogMessage("shellPropertySheetListSize: {0}", shellPropertySheetListSize);

            index += 2;

            var identifiersize = BitConverter.ToInt16(rawBytes, index);

            //    SiAuto.Main.LogMessage("identifiersize: {0}", identifiersize);

            index += 2;

            var identifierData = new byte[identifiersize];

            Array.Copy(rawBytes, index, identifierData, 0, identifiersize);

            //   SiAuto.Main.LogArray("identifierData", identifierData);

            index += identifiersize;

            if (shellPropertySheetListSize > 0)
            {
                var propBytes = rawBytes.Skip(index).Take(shellPropertySheetListSize).ToArray();
                var propStore = new PropertyStore(propBytes);

                PropertyStore = propStore;

                var p = propStore.Sheets.Where(t => t.PropertyNames.ContainsKey("32"));

                if (p.Any())
                {
                    //we can now look thry prop bytes for extension blocks
                    //TODO this is a hack until we can process vectors natively

                    var extOffsets = new List<int>();
                    try
                    {
                        var regexObj = new Regex("([0-9A-F]{2})-00-EF-BE", RegexOptions.IgnoreCase);
                        var matchResult = regexObj.Match(BitConverter.ToString(propBytes));
                        while (matchResult.Success)
                        {
                            extOffsets.Add(matchResult.Index);
                            matchResult = matchResult.NextMatch();
                        }

                        foreach (var extOffset in extOffsets)
                        {
                            var binaryOffset = extOffset/3 - 4;
                            var exSize = BitConverter.ToInt16(propBytes, binaryOffset);

                            var exBytes = propBytes.Skip(binaryOffset).Take(exSize).ToArray();

                            var signature1 = BitConverter.ToUInt32(exBytes, 4);

                            //Debug.WriteLine(" 0x1f bag sig: " + signature1.ToString("X8"));

                            var block1 = Utils.GetExtensionBlockFromBytes(signature1, exBytes);

                            ExtensionBlocks.Add(block1);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        throw ex;
                        // Syntax error in the regular expression
                    }

                    //     Debug.WriteLine("Found 32 key");
                }
            }
            else
            {
                //   Debug.Write("Oh no! No property sheets!");
                // SiAuto.Main.LogWarning("Oh no! No property sheets!");

                if (rawBytes[0x28] == 0x2f ||
                    (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41))
                {
                    //we have a good date

                    var zip = new ShellBagZipContents(Slot, MruPosition, rawBytes, BagPath);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }
                Debug.Write("Oh no! No property sheets!");
            }

            index += shellPropertySheetListSize;

            index += 2; //move past end of property sheet terminator

            var rawguid = Utils.ExtractGuidFromShellItem(rawBytes.Skip(index).Take(16).ToArray());
            //var rawguid = ShellBagUtils.ExtractGuidFromShellItem(bin.ReadBytes(16));
            index += 16;

            rawguid = Utils.ExtractGuidFromShellItem(rawBytes.Skip(index).Take(16).ToArray());
            index += 16;

            var name = Utils.GetFolderNameFromGuid(rawguid);

            Value = name;

            var extBlockSize = BitConverter.ToInt16(rawBytes, index);

            if (extBlockSize > 0)
            {
                //process extension blocks

                while (extBlockSize > 0)
                {
                    var extBytes = rawBytes.Skip(index).Take(extBlockSize).ToArray();

                    index += extBlockSize;

                    var signature1 = BitConverter.ToUInt32(extBytes, 4);

                    //Debug.WriteLine(" 0x1f bag sig: " + signature1.ToString("X8"));

                    var block1 = Utils.GetExtensionBlockFromBytes(signature1, extBytes);

                    ExtensionBlocks.Add(block1);

                    extBlockSize = BitConverter.ToInt16(rawBytes, index);
                }
            }

            int terminator = BitConverter.ToInt16(rawBytes, index);

            if (terminator > 0)
            {
                throw new Exception($"Expected terminator of 0, but got {terminator}");
            }
        }

        private void ProcessGuid(byte[] rawBytes)
        {
            FriendlyName = "Root folder: GUID";

            //TODO split this out or at least use a different icon?

            var index = 2;

            index += 2; // move past index and a single unknown value

            var rawguid1 = new byte[16];

            Array.Copy(rawBytes, index, rawguid1, 0, 16);

            var rawguid = Utils.ExtractGuidFromShellItem(rawguid1);

            var foldername = Utils.GetFolderNameFromGuid(rawguid);

            index += 16;

            if (index >= rawBytes.Length)
            {
                Value = foldername;
                return;
            }

            var size = BitConverter.ToInt16(rawBytes, index);
            if (size == 0)
            {
                index += 2;
            }

            if (size > rawBytes.Length)
            {
                Value = foldername;
                return;
            }

            //TODO this should be the looping cut up thing

            if (index < rawBytes.Length)
            {
                var signature = BitConverter.ToUInt32(rawBytes, index + 4);

                //TODO does this need to check if its a 0xbeef?? regex?
                var block = Utils.GetExtensionBlockFromBytes(signature, rawBytes.Skip(index).ToArray());

                ExtensionBlocks.Add(block);

                index += size;
            }

            Value = foldername;
        }

        private void ProcessGuid2(byte[] rawBytes)
        {
            FriendlyName = "Root folder: GUID";

            var delegateGUID = Utils.ExtractGuidFromShellItem(rawBytes.Skip(20).Take(16).ToArray());

            var folderGUID = Utils.ExtractGuidFromShellItem(rawBytes.Skip(36).Take(16).ToArray());

            var foldername = Utils.GetFolderNameFromGuid(folderGUID);

            Value = foldername;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (PropertyStore != null)
            {
                if (PropertyStore.Sheets.Count > 0)
                {
                    sb.AppendLine("Property Sheets");

                    sb.AppendLine(PropertyStore.ToString());
                }
            }

            if (LastAccessTime.HasValue)
            {
                sb.AppendLine(
                    $"Accessed On: {LastAccessTime.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
                sb.AppendLine();
            }

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}