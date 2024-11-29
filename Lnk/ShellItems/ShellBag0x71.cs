using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X71 : ShellBag
{
    public ShellBag0X71(byte[] rawBytes)
    {
        FriendlyName = "GUID: Control panel";

        ExtensionBlocks = new List<IExtensionBlock>();

        var index = 2;

        index += 2; // move past index and a single unknown value

        index += 10; //10 zeros

        var dataSig = BitConverter.ToUInt32(rawBytes, 6);

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


        var rawguid = Utils.ExtractGuidFromShellItem(rawguid1);


        var foldername = Utils.GetFolderNameFromGuid(rawguid);

        Value = foldername;

        if (rawBytes.Length > 32)
        {
            index = 0x1e;

            var signature1 = BitConverter.ToUInt32(rawBytes, index + 4);

            var block1 = Utils.GetExtensionBlockFromBytes(signature1, rawBytes.Skip(index).ToArray());

            ExtensionBlocks.Add(block1);
        }
    }

    public PropertyStore PropertyStore { get; private set; }

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
                //we can now look thru prop bytes for extension blocks
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
                    throw;
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
                //   LastAccessTime = zip.LastAccessTime;

                Value = zip.Value;

                return;
            }
        }

        index += shellPropertySheetListSize;

        index += 2; //move past end of property sheet terminator


        var rawguid = Utils.ExtractGuidFromShellItem(rawBytes.Skip(index).Take(16).ToArray());
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

        if (PropertyStore?.Sheets.Count > 0)
        {
            sb.AppendLine("Property Sheets");

            sb.AppendLine(PropertyStore.ToString());
        }

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}