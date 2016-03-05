using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    public class ShellBag0X00 : ShellBag
    {
        private string _classId;
        private string _fileSystemName;
        private List<string> _guids;
        private object _mtpType1GuidName;
        private string _storageIdName;


        public ShellBag0X00(byte[] rawBytes)
        {
            _guids = new List<string>();

            ShortName = string.Empty;

            FriendlyName = "Variable";

            PropertyStore = new PropertyStore();

            ExtensionBlocks = new List<IExtensionBlock>();

            // There are a few special cases for 0x00 items, so pull a special sig and see if we have one of those
            var specialDataSig = BitConverter.ToUInt32(rawBytes, 4);

            switch (specialDataSig)
            {
                case 0xc001b000: //00-B0-01-C0-
                    ProcessUrlContainer(rawBytes);

                    return;

                case 0x49534647: // this is a game folder shell item “GFSI”
                    ProcessGameFolderShellItem(rawBytes);

                    return;


                case 0xffffff38: // Control panel CPL file shell item

                    throw new Exception("Send this hive to saericzimmerman@gmail.com so support can be added!");

                //  return;
            }

            //if we are here this should be a users property view

            var index = 0;

            var shellItemSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            index += 1; // move past signature
            index += 1; // move past unknown

            var dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;
            //   SiAuto.Main.LogMessage("dataSize: {0}", dataSize);

            var dataSig = BitConverter.ToUInt32(rawBytes, index);
            index += 4;
            //   SiAuto.Main.LogMessage("dataSig: {0:X}", dataSig);

            var identifierSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;
            //    SiAuto.Main.LogMessage("identifierSize: {0}", identifierSize);

            switch (dataSig)
            {
                case 0x00030005:
                case 0x00000005:

                    ProcessFtpSubItem(rawBytes);

                    break;
                case 0x23febbee:
                    ProcessPropertyViewGuid(rawBytes);

                    return;

                case 0x10312005:

                    ProcessMtpType2(rawBytes);

                    return;

                case 0x00:
                    //this is the contents of a zip file?

                    ProcessZipFileContents(rawBytes);

                    return;

                case 0x7192006:

                    ProcessMtpType1(rawBytes);

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

        public DateTimeOffset? FtpFolderTime { get; set; }


        public int FileSize { get; private set; }

        public string Miscellaneous { get; private set; }

        public string FullUrl { get; set; }

        public string ShortName { get; private set; }

        private void ProcessUrlContainer(byte[] rawBytes)
        {
            FriendlyName = "Variable: HTTP URI";

            var index = 0x14;

            var size = BitConverter.ToUInt32(rawBytes, index);
            index += 4;

            var url = Encoding.Unicode.GetString(rawBytes, index, (int) size);
            index += (int) size;


            Value = Uri.UnescapeDataString(url.Replace("\0", ""));

            index += 16;

            size = BitConverter.ToUInt32(rawBytes, index);
            index += 4;

            url = Encoding.Unicode.GetString(rawBytes, index, (int) size);
            index += (int) size;

            FullUrl = url.Replace("\0", "");
        }

        private void ProcessFtpSubItem(byte[] rawBytes)
        {
            FriendlyName = "Variable: FTP URI";

            var index = 0x16;

            var ft1 = DateTimeOffset.FromFileTime((long) BitConverter.ToUInt64(rawBytes, index));

            index += 8;

            var fileTime1 = ft1.ToUniversalTime();

            if (fileTime1.Year > 1601)
            {
                FtpFolderTime = fileTime1;
            }

            index += 8;

            var len1 = 0;

            while (rawBytes[index + len1] != 0x00)
            {
                len1 += 1;
            }

            var s1 = Encoding.GetEncoding(1252).GetString(rawBytes, index, len1);

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


        private void ProcessMtpType2(byte[] rawBytes)
        {
            FriendlyName = "Variable: MTP type 2";

            var index = 4;

            var dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            index += 4; //skip signature

            index += 4; //skip unknown

            index += 2; //skip unknown

            index += 2; //skip unknown

            index += 4; //skip unknown

            index += 12; //skip unknown empty

            index += 4; //skip unknown size

            var storageStringNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var storageIdStringLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var fileSystemNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var numGuiDs = BitConverter.ToInt32(rawBytes, index);

            index += 4; // skip unknown

            var storageName = Encoding.Unicode.GetString(rawBytes, index, storageStringNameLen*2 - 2);

            index += storageStringNameLen*2;

            _storageIdName = Encoding.Unicode.GetString(rawBytes, index, storageIdStringLen*2 - 2);

            index += storageIdStringLen*2;

            _fileSystemName = Encoding.Unicode.GetString(rawBytes, index, fileSystemNameLen*2 - 2);

            index += fileSystemNameLen*2;

            //index += 6; //get to beginning of GUIDs


            _guids = new List<string>();
            for (var i = 0; i < numGuiDs; i++)
            {
                var rawGuid = rawBytes.Skip(index).Take(78).ToArray();
                index += 78;
                var guid = Encoding.Unicode.GetString(rawGuid).Replace("\0", "");
                _guids.Add(guid);
            }

            index += 4; //unknown

            var classIdRaw = rawBytes.Skip(index).Take(16).ToArray();

            var clasIdguid = Utils.ExtractGuidFromShellItem(classIdRaw);
            _classId = Utils.GetFolderNameFromGuid(clasIdguid);

            index += 16;

            var numProps = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            //at property array, but what is it?


            Value = storageName;
        }

        private void ProcessMtpType1(byte[] rawBytes)
        {
            FriendlyName = "Variable: MTP type 1";

            //if (rawBytes.Length == 732)
            //{
            //    Debug.WriteLine("Pausing for documented info from http://nicoleibrahim.com/part-5-usb-device-research-directory-traversal-artifacts-shell-bagmru-entries/");
            //}

            var index = 0x1a;

            var modified = DateTimeOffset.FromFileTime((long) BitConverter.ToUInt64(rawBytes, index));

            LastModificationTime = modified.ToUniversalTime();

            index += 8;

            var created = DateTimeOffset.FromFileTime((long) BitConverter.ToUInt64(rawBytes, index));

            CreatedOnTime = created.ToUniversalTime();

            index += 8;

            var rawGuid = rawBytes.Skip(index).Take(16).ToArray();
            var guidString = Utils.ExtractGuidFromShellItem(rawGuid);
            _mtpType1GuidName = Utils.GetFolderNameFromGuid(guidString);

            index = 0x3e;

            var storageStringNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var storageIdStringLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var fileSystemNameLen = BitConverter.ToInt32(rawBytes, index);

            index += 4;

            var storageName = Encoding.Unicode.GetString(rawBytes, index, storageStringNameLen*2 - 2);

            index += storageStringNameLen*2;

            _storageIdName = Encoding.Unicode.GetString(rawBytes, index, storageIdStringLen*2 - 2);

            index += storageIdStringLen*2;

            _fileSystemName = Encoding.Unicode.GetString(rawBytes, index, fileSystemNameLen*2 - 2);

            index += fileSystemNameLen*2;

            //TODO pull out modified time/created for OLE?

            if (storageName.Length > 0)
            {
                if (storageName == _storageIdName)
                {
                    Value = storageName;
                }
                else
                {
                    Value = $"{storageName} ({_storageIdName})";
                }
            }
            else
            {
                Value = _storageIdName;
            }
        }

        private void ProcessZipFileContents(byte[] rawBytes)
        {
            FriendlyName = "Variable: Zip file contents";

            if (rawBytes[0x28] == 0x2f || (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41) ||
                rawBytes[0x1c] == 0x2f || (rawBytes[0x18] == 0x4e && rawBytes[0x1a] == 0x2f && rawBytes[0x1c] == 0x41))
            {
                //we have a good date

                var zip = new ShellBagZipContents(rawBytes);
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
            var index = 10;

            var shellPropertySheetListSize = BitConverter.ToInt16(rawBytes, index);

            index += 2;

            var identifiersize = BitConverter.ToInt16(rawBytes, index);

            index += 2;

            var identifierData = new byte[identifiersize];

            Array.Copy(rawBytes, index, identifierData, 0, identifiersize);

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

                    var zip = new ShellBagZipContents(rawBytes);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }

                //41-75-67-4D is AugM

                if (rawBytes[4] == 0x41 && rawBytes[5] == 0x75 && rawBytes[6] == 0x67 && rawBytes[7] == 0x4D)
                {
                    var cdb = new ShellBagCDBurn(rawBytes);

                    Value = cdb.Value;
                    FriendlyName = cdb.FriendlyName;
                    CreatedOnTime = cdb.CreatedOnTime;
                    LastModificationTime = cdb.LastModificationTime;
                    LastAccessTime = cdb.LastAccessTime;
                    

                    return;
                }

                

                Debug.Write("Oh no! No property sheets!");

                Value = "!!! Unable to determine Value !!!";
            }

            index += shellPropertySheetListSize;

            index += 2; //move past end of property sheet terminator

            if (shellPropertySheetListSize > 0 && index < rawBytes.Length)
            {
                var extBlockSize = BitConverter.ToInt16(rawBytes, index);

                if (extBlockSize > 0)
                {
                    //process extension blocks

                    while (extBlockSize > 0)
                    {
                        var extBytes = rawBytes.Skip(index).Take(extBlockSize).ToArray();

                        index += extBlockSize;

                        var signature1 = BitConverter.ToUInt32(extBytes, 4);


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

            var valuestring = (from propertySheet in PropertyStore.Sheets
                from propertyName in propertySheet.PropertyNames
                where propertyName.Key == "10"
                select propertyName.Value).FirstOrDefault();

            if (valuestring == null)
            {
                var namesList =
                    (from propertySheet in PropertyStore.Sheets
                        from propertyName in propertySheet.PropertyNames
                        select propertyName.Value)
                        .ToList();

                valuestring = string.Join("::", namesList.ToArray());
            }

            if (valuestring == "")
            {
                valuestring = "No Property sheet value found";
            }

            Value = valuestring;
        }

        private void ProcessPropertyViewGuid(byte[] rawBytes)
        {
            // this is a guid

            var index = 4;

            var dataSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            index += 4; // move past signature

            var propertyStoreSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            Trace.Assert(propertyStoreSize == 0, "propertyStoreSize size > 0!");

            var identifierSize = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            if (identifierSize > 0)
            {
                var raw00Guid = new byte[16];

                Array.Copy(rawBytes, index, raw00Guid, 0, 16);


                var preguid = Utils.ExtractGuidFromShellItem(raw00Guid);


                var tempString = Utils.GetFolderNameFromGuid(preguid);

                Value = tempString;

                index += 16;
            }

            index += 2; // skip 2 unknown in common

            if (index >= rawBytes.Length)
            {
                return;
            }

            var extBlockSize = BitConverter.ToInt16(rawBytes, index);

            if (extBlockSize > 0)
            {
                //process extension blocks

                while (extBlockSize > 0)
                {
                    var extBytes = rawBytes.Skip(index).Take(extBlockSize).ToArray();

                    index += extBlockSize;

                    var signature1 = BitConverter.ToUInt32(extBytes, 4);

                    var block1 = Utils.GetExtensionBlockFromBytes(signature1, extBytes);

                    ExtensionBlocks.Add(block1);

                    if (index >= rawBytes.Length)
                    {
                        break;
                    }
                    extBlockSize = BitConverter.ToInt16(rawBytes, index);
                }
            }
//
//            int terminator = BitConverter.ToInt16(rawBytes, index);
//
//            if (terminator > 0)
//            {
//                throw new Exception($"Expected terminator of 0, but got {terminator}");
//            }
        }

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

            if (FtpFolderTime.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine($"FTP folder time: {FtpFolderTime.Value}");
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

            if (_fileSystemName != null)
            {
                sb.AppendLine($"File system name: {_fileSystemName}");
            }
            if (_storageIdName != null)
            {
                sb.AppendLine($"Storage ID name: {_storageIdName}");
            }
            if (_classId != null)
            {
                sb.AppendLine($"Class ID: {_classId}");
            }
            if (_mtpType1GuidName != null)
            {
                sb.AppendLine($"GUID: {_mtpType1GuidName}");
            }


            sb.AppendLine();
            sb.AppendLine(base.ToString());


            return sb.ToString();
        }
    }
}