using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    public class ShellBag0X1F : ShellBag
    {
        private readonly List<PropertySheet> _sheets;


        public ShellBag0X1F(byte[] rawBytes)
        {
            ExtensionBlocks = new List<IExtensionBlock>();

            PropertyStore = new PropertyStore();

            _sheets = new List<PropertySheet>();

            var index = 0;


            var dataSig = BitConverter.ToUInt32(rawBytes, 6);

            if (dataSig == 0xbeebee00)
            {
                ProcessPropertyViewDefault(rawBytes);
                return;
            }

            if (dataSig == 0xF5A6B710)
            {
                // this is a strange one. it contains a drive letter and other unknown items

                index = 13;

                var dl = Encoding.GetEncoding(1252).GetString(rawBytes, index, 3);

                FriendlyName = "Users property view?: Drive letter";

                Value = dl;


                return;
            }


            if (rawBytes[0] == 0x14) //This is a GUID only
            {
                ProcessGuid(rawBytes);
                return;
            }

            if (rawBytes[0] == 50 || rawBytes[0] == 58) //This is a GUID and a beefXX, usually 25
            {
                ProcessGuid(rawBytes);

                index += 20;

                var extsize = BitConverter.ToInt16(rawBytes, index);

                if (extsize > 0)
                {
                    var signature = BitConverter.ToUInt32(rawBytes, index + 4);

                    var block = Utils.GetExtensionBlockFromBytes(signature, rawBytes.Skip(index).ToArray());

                    ExtensionBlocks.Add(block);
                }

                index += extsize;

                if (index != rawBytes[0])
                {
                    Debug.WriteLine("remaining data!!!");
                }

                return;
            }

            FriendlyName = "Users property view";

            var bin = new BinaryReader(new MemoryStream(rawBytes));

            bin.ReadBytes(2); // skip size

            bin.ReadByte(); //skip indicator (0x1F)

            var sortIndicator = bin.ReadByte();

            var dataSize = bin.ReadUInt16(); // BitConverter.ToUInt16(rawBytes, index);

            var dataSignature = bin.ReadUInt32(); //  BitConverter.ToUInt32(rawBytes, index);

            var propertyStoreSize = bin.ReadUInt16(); //BitConverter.ToUInt16(rawBytes, index);

            var identifierSize = bin.ReadUInt16(); //BitConverter.ToUInt16(rawBytes, index);

            if (identifierSize > 0)
            {
                bin.ReadBytes(identifierSize);
                //index += identifierSize; // whats here?
            }

            if (propertyStoreSize > 0)
            {
                var propertysheetBytes = bin.ReadBytes(propertyStoreSize);

                var propStore = new PropertyStore(propertysheetBytes);

                var p = propStore.Sheets.Where(t => t.PropertyNames.ContainsKey("AutoList"));

                if (p.Any())
                {
                    //we can now look thry prop bytes for extension blocks
                    //TODO this is a hack until we can process vectors natively

                    var extOffsets = new List<int>();
                    try
                    {
                        var regexObj = new Regex("([0-9A-F]{2})-00-EF-BE", RegexOptions.IgnoreCase);
                        var matchResult = regexObj.Match(BitConverter.ToString(propertysheetBytes));
                        while (matchResult.Success)
                        {
                            extOffsets.Add(matchResult.Index);
                            matchResult = matchResult.NextMatch();
                        }

                        foreach (var extOffset in extOffsets)
                        {
                            var binaryOffset = extOffset / 3 - 4;
                            var exSize = BitConverter.ToInt16(propertysheetBytes, binaryOffset);

                            var exBytes = propertysheetBytes.Skip(binaryOffset).Take(exSize).ToArray();

                            var signature1 = BitConverter.ToUInt32(exBytes, 4);


                            var block1 = Utils.GetExtensionBlockFromBytes(signature1, exBytes);

                            ExtensionBlocks.Add(block1);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        throw ex;
                        // Syntax error in the regular expression
                    }
                }

                PropertyStore = propStore;
            }


            bin.ReadBytes(2); //skip end of property sheet marker

            var rawguid = Utils.ExtractGuidFromShellItem(bin.ReadBytes(16));
            //    index += 16;

            rawguid = Utils.ExtractGuidFromShellItem(bin.ReadBytes(16));
            //   index += 16;

            var name = Utils.GetFolderNameFromGuid(rawguid);

            var extsize1 = bin.ReadUInt16(); // BitConverter.ToUInt16(rawBytes, index);

            if (extsize1 > 4)
            {
                //TODO is it ever bigger than one block? if so loop it

                //move position back 2 so we get the entire block of data below
                bin.BaseStream.Position -= 2;

                while (bin.BaseStream.Position != bin.BaseStream.Length)
                {
                    extsize1 = bin.ReadUInt16();

                    if (extsize1 == 0)
                    {
                        break;
                    }

                    bin.BaseStream.Position -= 2;

                    var extBytes = bin.ReadBytes(extsize1);

                    var signature1 = BitConverter.ToUInt32(extBytes, 4);

                    //Debug.WriteLine(" 0x1f bag sig: " + signature1.ToString("X8"));

                    var block1 = Utils.GetExtensionBlockFromBytes(signature1, extBytes);

                    ExtensionBlocks.Add(block1);
                }

                //Trace.Assert(bin.BaseStream.Position == bin.BaseStream.Length);
            }

            Value = name;
        }

        public PropertyStore PropertyStore { get; private set; }

        /// <summary>
        ///     Last access time of BagPath
        /// </summary>
        public DateTimeOffset? LastAccessTime { get; set; }

        private void ProcessGuid(byte[] rawBytes)
        {
            FriendlyName = "Root folder: GUID";

            var index = 2;

            index += 2; // move past index and a single unknown value

            var rawguid1 = new byte[16];

            Array.Copy(rawBytes, index, rawguid1, 0, 16);

            var rawguid = Utils.ExtractGuidFromShellItem(rawguid1);

            var foldername = Utils.GetFolderNameFromGuid(rawguid);

            index += 16;

            Value = foldername;

            if (rawBytes.Length == index)
            {
            }

//            var size = BitConverter.ToInt16(rawBytes, index);
//            if (size == 0)
//            {
//                index += 2;
//            }
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
                            var binaryOffset = extOffset / 3 - 4;
                            var exSize = BitConverter.ToInt16(propBytes, binaryOffset);

                            var exBytes = propBytes.Skip(binaryOffset).Take(exSize).ToArray();

                            var signature1 = BitConverter.ToUInt32(exBytes, 4);

                            var block1 = Utils.GetExtensionBlockFromBytes(signature1, exBytes);

                            ExtensionBlocks.Add(block1);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        throw ex;
                        // Syntax error in the regular expression
                    }
                }
            }
            else
            {
                if (rawBytes[0x28] == 0x2f ||
                    rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41)
                {
                    //we have a good date

                    var zip = new ShellBagZipContents(rawBytes);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }
                Debug.Write("Oh no! No property sheets!");


                Value = "!!! Unable to determine Value !!!";
            }

            index += shellPropertySheetListSize;

            index += 2; //move past end of property sheet terminator

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

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (LastAccessTime.HasValue)
            {
                sb.AppendLine(
                    $"Accessed On: {LastAccessTime.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
                sb.AppendLine();
            }

            if (PropertyStore.Sheets.Count > 0)
            {
                sb.AppendLine("Property Sheets");

                sb.AppendLine(PropertyStore.ToString());
            }

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}