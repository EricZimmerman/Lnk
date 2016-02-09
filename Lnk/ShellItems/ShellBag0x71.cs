using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0x71 : ShellBag
    {
        public PropertyStore PropertyStore { get; private set; }

        public ShellBag0x71(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            FriendlyName = "GUID: Control panel";

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

//            if (bagPath.Contains(@"BagMRU\1\3") && slot == 3)
//            {
//                Debug.WriteLine("At trap for certain bag in 0x71 bag");
//            }

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            int index = 2;

            index += 2; // move past index and a single unknown value

            index += 10; //10 zeros

             uint dataSig = BitConverter.ToUInt32(rawBytes, 6);

            if (dataSig == 0xbeebee00)
            {
                ProcessPropertyViewDefault(rawBytes);

                return;
            }

            // int dataLength1 = rawBytes.Length - index;

            if (rawBytes[2] == 0x4d)
            {
                index -= 10;
            }

            if (rawBytes.Length == 0x16)
            {
                index = 4;
            }

            var rawguid1 = new byte[16];

            Array.Copy(rawBytes, index, rawguid1, 0, 16);

            index += 16;

            //     SiAuto.Main.LogArray("rawguid1", rawguid1);

            string rawguid = Utils.ExtractGuidFromShellItem(rawguid1);

            //  SiAuto.Main.LogMessage("preguid71 after ExtractGUIDFromShellItem: {0}", rawguid);

            string foldername = Utils.GetFolderNameFromGuid(rawguid);

            //     SiAuto.Main.LogMessage("foldername after GetFolderNameFromGUID: {0}", foldername);

            if (foldername.Contains(rawguid))
            {
              //  SiAuto.Main.LogWarning("GUID did not map to name! {0}", rawguid);

            
            }

            Value = foldername;

            if (rawBytes.Length > 32)
            {
                index = 0x1e;

                var signature1 = BitConverter.ToUInt32(rawBytes, index + 4);

                //Debug.WriteLine(" 0x1f bag sig: " + signature1.ToString("X8"));

                var block1 = Utils.GetExtensionBlockFromBytes(signature1, rawBytes.Skip(index).ToArray());

                ExtensionBlocks.Add(block1);

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
                    //   LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    return;
                }
                else
                {
                    Debug.Write("Oh no! No property sheets!");
                   
                }
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

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}