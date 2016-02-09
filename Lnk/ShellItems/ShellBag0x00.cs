using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0x00 : ShellBag
    {
        private string _ClassID;
        private string _FileSystemName;
        private Object _MTPType1GuidName;
        private string _StorageIDName;
        private List<string> _guids;


        public ShellBag0x00(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            ChildShellBags = new List<IShellBag>();

            //if (bagPath.Contains(@"BagMRU\13") && slot == 2)
            //{
            //    Debug.WriteLine("At trap for certain bag in 0x00 bag");
            //}

            _guids = new List<string>();

            ShortName = string.Empty;

            FriendlyName = "Variable";

            PropertyStore = new PropertyStore();

            InternalId = Guid.NewGuid().ToString();

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            // There are a few special cases for 0x00 items, so pull a special sig and see if we have one of those
            uint specialDataSig = BitConverter.ToUInt32(rawBytes, 4);

            switch (specialDataSig)
            {
                case 0xc001b000:  //00-B0-01-C0-
                    ProcessURLContainer(rawBytes);

                    return;

                case 0x49534647: // this is a game folder shell item “GFSI”
                    ProcessGameFolderShellItem(rawBytes);

                    return;

              

                case 0xffffff38: // Control panel CPL file shell item

                    throw new Exception("Send this hive to saericzimmerman@gmail.com so support can be added!");

                  //  return;
            }

            //if we are here this should be a users property view

            int index = 0;

            ushort shellItemSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            index += 1; // move past signature
            index += 1; // move past unknown

            ushort dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;
            //   SiAuto.Main.LogMessage("dataSize: {0}", dataSize);

            uint dataSig = BitConverter.ToUInt32(rawBytes, index);
            index += 4;
            //   SiAuto.Main.LogMessage("dataSig: {0:X}", dataSig);

            ushort identifierSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;
            //    SiAuto.Main.LogMessage("identifierSize: {0}", identifierSize);

            switch (dataSig)
            {
                    case 0x00030005:
                    case 0x00000005:

                    ProcessFTPSubItem(rawBytes);

                    break;
                case 0x23febbee:
                    ProcessPropertyViewGUID(rawBytes);

                    return;

                case 0x10312005:

                    ProcessMTPType2(rawBytes);

                    return;

                case 0x00:
                    //this is the contents of a zip file?

                    ProcessZipFileContents(rawBytes);

                    return;

                case 0x7192006:

                    ProcessMTPType1(rawBytes);

                    return;

                default: // includes known 0xbeebee00
                    if (rawBytes.Length <= 0x64)
                    {
                        FriendlyName = "Server name";
                        Value = Encoding.Unicode.GetString(rawBytes, 6, rawBytes.Length - 6).Replace("\0", string.Empty);
                    }
                    else
                    {
                        ProcessPropertyViewDefault(rawBytes);
                    }

                    break;
            }
        }

        private void ProcessURLContainer(byte[] rawBytes)
        {
            FriendlyName = "Variable: HTTP URI";

            int index = 0x14;

            var size = BitConverter.ToUInt32(rawBytes, index);
            index += 4;

            var url = Encoding.Unicode.GetString(rawBytes, index, (int) size);
            index += (int)size;


            Value = Uri.UnescapeDataString(url.Replace("\0", ""));

            index += 16;

            size = BitConverter.ToUInt32(rawBytes, index);
            index += 4;

             url = Encoding.Unicode.GetString(rawBytes, index, (int)size);
            index += (int)size;

            FullUrl = url.Replace("\0", "");

        }

        private void ProcessFTPSubItem(byte[] rawBytes)
        {
            FriendlyName = "Variable: FTP URI";
            
            int index = 0x16;

            var ft1 = DateTimeOffset.FromFileTime((long)BitConverter.ToUInt64(rawBytes, index));

            index += 8;

           var fileTime1 = ft1.ToUniversalTime();

            if (fileTime1.Year > 1601)
            {
                FTPFolderTime = fileTime1;
            }

            index += 8;

            var len1 = 0;

            while (rawBytes[index + len1] != 0x00)
            {
                len1 += 1;
            }

           var s1 = Encoding.ASCII.GetString(rawBytes, index, len1);

            ShortName = s1;
           
            index += len1;

            while (rawBytes[index] == 0)
            {
                index += 1;
            }

           

            len1 = 0;

            while (rawBytes[index + len1] != 0x00 || rawBytes[index + len1 + 1] != 0x00)
            {
                len1 += 1;
            }

             s1 = Encoding.Unicode.GetString(rawBytes, index, len1 + 1);


            index += len1 + 1;

            Value = s1;
        }

        public PropertyStore PropertyStore { get; private set; }

        /// <summary>
        ///     last modified time of BagPath
        /// </summary>
        public DateTimeOffset? LastModificationTime { get; set; }

        /// <summary>
        ///     Created time of BagPath
        /// </summary>
        public DateTimeOffset? CreatedOnTime { get; set; }

        /// <summary>
        ///     Last access time of BagPath
        /// </summary>
        public DateTimeOffset? LastAccessTime { get; set; }

        public DateTimeOffset? FTPFolderTime { get; set; }

        ///// <summary>
        /////     For files and directories, the MFT entry #
        ///// </summary>
        //public long? MFTEntryNumber { get; set; }

        ///// <summary>
        /////     For files and directories, the MFT sequence #
        ///// </summary>
        //public int? MFTSequenceNumber { get; set; }

        public int FileSize { get; private set; }

        public string Miscellaneous { get; private set; }

        public string FullUrl { get; set; }

    

        private void ProcessMTPType2(byte[] rawBytes)
        {
            FriendlyName = "Variable: MTP type 2";

            int index = 4;

            ushort dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            index += 4; //skip signature

            index += 4; //skip unknown

            index += 2; //skip unknown

            index += 2; //skip unknown

            index += 4; //skip unknown

            index += 12; //skip unknown empty

            index += 4; //skip unknown size

            int storageStringNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            int storageIDStringLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            int fileSystemNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var numGUIDs = BitConverter.ToInt32(rawBytes, index);

            index += 4; // skip unknown

            string storageName = Encoding.Unicode.GetString(rawBytes, index, storageStringNameLen * 2 - 2);

            index += storageStringNameLen * 2;

            _StorageIDName = Encoding.Unicode.GetString(rawBytes, index, storageIDStringLen * 2 - 2);

            index += storageIDStringLen * 2;

            _FileSystemName = Encoding.Unicode.GetString(rawBytes, index, fileSystemNameLen * 2 - 2);

            index += fileSystemNameLen * 2;

            //index += 6; //get to beginning of GUIDs


            _guids = new List<string>();
            for (int i = 0; i < numGUIDs; i++)
            {
                var rawGUID = rawBytes.Skip(index).Take(78).ToArray();
                index += 78;
                var guid = Encoding.Unicode.GetString(rawGUID).Replace("\0","");
                _guids.Add(guid);
            }

            index += 4; //unknown

            var classIDRaw = rawBytes.Skip(index).Take(16).ToArray();

            var clasIDGUID = Utils.ExtractGuidFromShellItem(classIDRaw);
            _ClassID = Utils.GetFolderNameFromGuid(clasIDGUID);

            index += 16;

            var numProps = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            //at property array, but what is it?


            Value = storageName;
        }

        private void ProcessMTPType1(byte[] rawBytes)
        {
            FriendlyName = "Variable: MTP type 1";

            //if (rawBytes.Length == 732)
            //{
            //    Debug.WriteLine("Pausing for documented info from http://nicoleibrahim.com/part-5-usb-device-research-directory-traversal-artifacts-shell-bagmru-entries/");
            //}

            int index = 0x1a;

            DateTimeOffset modified = DateTimeOffset.FromFileTime((long)BitConverter.ToUInt64(rawBytes, index));

            LastModificationTime = modified.ToUniversalTime();

            index += 8;

            DateTimeOffset created = DateTimeOffset.FromFileTime((long)BitConverter.ToUInt64(rawBytes, index));

            CreatedOnTime = created.ToUniversalTime();

            index += 8;

            var rawGuid = rawBytes.Skip(index).Take(16).ToArray();
            var guidString = Utils.ExtractGuidFromShellItem(rawGuid);
            _MTPType1GuidName = Utils.GetFolderNameFromGuid(guidString);

            index = 0x3e;

            int storageStringNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            int storageIDStringLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            int fileSystemNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            string storageName = Encoding.Unicode.GetString(rawBytes, index, storageStringNameLen * 2 - 2);

            index += storageStringNameLen * 2;

            _StorageIDName = Encoding.Unicode.GetString(rawBytes, index, storageIDStringLen * 2 - 2);

            index += storageIDStringLen * 2;

            _FileSystemName = Encoding.Unicode.GetString(rawBytes, index, fileSystemNameLen * 2 - 2);

            index += fileSystemNameLen * 2;

            //TODO pull out modified time/created for OLE?

            if (storageName.Length > 0)
            {
                if (storageName == _StorageIDName)
                {
                    Value = storageName;
                }
                else
                {
                    Value = $"{storageName} ({_StorageIDName})";
                }
                    
            }
            else
            {
                Value = _StorageIDName;
            }
        }

        private void ProcessZipFileContents(byte[] rawBytes)
        {
            FriendlyName = "Variable: Zip file contents";

            if ((rawBytes[0x28] == 0x2f || (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41)) || (rawBytes[0x1c] == 0x2f || (rawBytes[0x18] == 0x4e && rawBytes[0x1a] == 0x2f && rawBytes[0x1c] == 0x41)))
            {
                //we have a good date

                var zip = new ShellBagZipContents(Slot, MruPosition, rawBytes, BagPath);
                FriendlyName = zip.FriendlyName;
                LastAccessTime = zip.LastAccessTime;

                Value = zip.Value;
            }
            else
            {
                Value = "!!! Unable to determine Value !!!";
            }
        }

        private void ProcessPropertyViewDefault(byte[] rawBytes)
        {
            FriendlyName = "Variable: Users property view";
            int index = 10;

            short shellPropertySheetListSize = BitConverter.ToInt16(rawBytes, index);

            //       SiAuto.Main.LogMessage("shellPropertySheetListSize: {0}", shellPropertySheetListSize);

            index += 2;

            short identifiersize = BitConverter.ToInt16(rawBytes, index);

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

              var p =  propStore.Sheets.Where(t => t.PropertyNames.ContainsKey("32"));

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

                if (rawBytes[0x28] == 0x2f || (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41))
                {
                    //we have a good date

                    var zip = new ShellBagZipContents(Slot, MruPosition, rawBytes, BagPath);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }
                else
                {
                    Debug.Write("Oh no! No property sheets!");
                    

                    Value = "!!! Unable to determine Value !!!";
                }
            }

            index += shellPropertySheetListSize;

            index += 2; //move past end of property sheet terminator

            if (index < rawBytes.Length)
            {
                
            

            short extBlockSize = BitConverter.ToInt16(rawBytes, index);

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

            string valuestring = (from propertySheet in PropertyStore.Sheets
                                  from propertyName in propertySheet.PropertyNames
                                  where propertyName.Key == "10"
                                  select propertyName.Value).FirstOrDefault();

            if (valuestring == null)
            {
                List<string> namesList =
                    (from propertySheet in PropertyStore.Sheets
                     from propertyName in propertySheet.PropertyNames
                     select propertyName.Value)
                        .ToList();

                valuestring = string.Join("::", namesList.ToArray());
            }

            if (valuestring == "")
            {
                valuestring = "No Property sheets found";
            }

            Value = valuestring;
        }

        private void ProcessPropertyViewGUID(byte[] rawBytes)
        {
            // this is a guid

            int index = 4;

            ushort dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            index += 4; // move past signature

            ushort propertyStoreSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            Trace.Assert(propertyStoreSize == 0, "propertyStoreSize size > 0!");

            ushort identifierSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            if (identifierSize > 0)
            {
                var raw00guid = new byte[16];

                Array.Copy(rawBytes, index, raw00guid, 0, 16);

                //    SiAuto.Main.LogArray("Raw00guid", raw00guid);

                string preguid = Utils.ExtractGuidFromShellItem(raw00guid);

                //   SiAuto.Main.LogMessage("preguid after ExtractGUIDFromShellItem: {0}", preguid);

                string tempString = Utils.GetFolderNameFromGuid(preguid);

                //  SiAuto.Main.LogMessage("tempString after GetFolderNameFromGUID: {0}", tempString);

                //dfd5009b-23a3-008d-0400-000000008900
                if (tempString == preguid)
                {
                    //SiAuto.Main.LogWarning("GUID did not map to name! {0}", preguid);
                }

                Value = tempString;

                //   SiAuto.Main.LogMessage("set value to {0}", tempString);

                index += 16;
            }

            index += 2; // skip 2 unknown in common

            if (index >= rawBytes.Length)
            {
                return;
            }

            short extBlockSize = BitConverter.ToInt16(rawBytes, index);

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

        public string ShortName { get; private set; }

        private void ProcessGameFolderShellItem(byte[] rawBytes)
        {
            FriendlyName = "Variable: Game folder";
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (ShortName.Length > 0)
            {
                sb.AppendLine($"Short name: {ShortName}");
            }

            if (FTPFolderTime.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine($"FTP folder time: {FTPFolderTime.Value}");
            }

            if (FullUrl != null)
            {
                sb.AppendLine($"Full URL: {FullUrl}");
            }

                //TODO denote custom properties vs standard ones
                if (CreatedOnTime.HasValue)
                {
                    sb.AppendLine($"Created On: {CreatedOnTime.Value}");
                }

            //TODO denote custom properties vs standard ones
            if (LastModificationTime.HasValue)
            {
                sb.AppendLine($"Modified On: {LastModificationTime.Value}");
            }

            if (LastAccessTime.HasValue)
            {
                sb.AppendLine(
                    $"Accessed On: {LastAccessTime.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            }

            if (PropertyStore.Sheets.Count > 0)
            {
                sb.AppendLine("Property Sheets");

                sb.AppendLine(PropertyStore.ToString());
                sb.AppendLine();
            }
      
                if (_guids.Count > 0)
                {
                    sb.AppendLine("MTP GUIDs");

                    foreach (var guid in _guids)
                    {
                        sb.AppendLine(guid);
                    }

                 
                }
                sb.AppendLine();

                if (_FileSystemName != null)
                {
                    sb.AppendLine($"File system name: {_FileSystemName}");
                }
                if (_StorageIDName != null)
                {
                    sb.AppendLine($"Storage ID name: {_StorageIDName}");
                }
                if (_ClassID != null)
                {
                    sb.AppendLine($"Class ID: {_ClassID}");
                }
                if (_MTPType1GuidName != null)
                {
                    sb.AppendLine($"GUID: {_MTPType1GuidName}");
                }

            
           
                sb.AppendLine();
                sb.AppendLine(base.ToString());


            return sb.ToString();
        }
    }
}