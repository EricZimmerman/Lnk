using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X2E : ShellBag
{
    public ShellBag0X2E(byte[] rawBytes)
    {

        ExtensionBlocks = new List<IExtensionBlock>();


        var index = 0;

        if (rawBytes[3] == 0x80 || rawBytes.Length == 0x16)
        {
            ProcessGuid(rawBytes);
        }
        else
        {
            var postSig = BitConverter.ToUInt64(rawBytes, rawBytes.Length - 8);

            if (postSig == 0x0000ee306bfe9555 || postSig == 0xee306bfe9555c589)
            {
                FriendlyName = "User profile";

                var la =
                    Utils.ExtractDateTimeOffsetFromBytes(
                        Helpers.SliceClamped(rawBytes, rawBytes.Length - 14, 4));

                LastAccessTime = la;

                index = 10;

                var tempString = Encoding.Unicode.GetString(Helpers.SliceClamped(rawBytes, index, int.MaxValue)).Split('\0')[0];

                if (tempString == "")
                {
                    tempString = "(None)";
                }

                Value = tempString;

                return;
            }


            var testSig2 = BitConverter.ToInt32(rawBytes, 5);

            if (testSig2 >= 0x15032601)
            {
                FriendlyName = "Control panel category";

                if (rawBytes.Length < 0x48)
                {
                    var guidb1 = new byte[16];
                    index = 4; //beginning of guids

                    Buffer.BlockCopy(rawBytes, index, guidb1, 0, 16);

                    var g1 = new Guid(guidb1);
                    var gf1 = GuidMapping.GuidMapping.GetDescriptionFromGuid(g1.ToString());

                    index += 16;

                    var b26 = new Beef0026(Helpers.SliceClamped(rawBytes, index, int.MaxValue));
                    
                    Value = b26.ToString();

                    DevicePath = gf1;

                    Category = gf1;
                    
                    return;
                }
                
                index = 0x12;

                var val1 = Encoding.Unicode.GetString(rawBytes, index, 0x48 * 2).Trim('\0');

                index = 0x116;

                var val2 = Encoding.Unicode.GetString(rawBytes, index, rawBytes.Length - 0x22 - index).Trim('\0');

                var guidb = new byte[16];
                index = rawBytes.Length - 0x22; //beginning of guids

                Buffer.BlockCopy(rawBytes, index, guidb, 0, 16);

                var g = new Guid(guidb);
                var gf = GuidMapping.GuidMapping.GetDescriptionFromGuid(g.ToString());

                index += 16;

                guidb = new byte[16];
                Buffer.BlockCopy(rawBytes, index, guidb, 0, 16);

                var g2 = new Guid(guidb);
                var g2f = GuidMapping.GuidMapping.GetDescriptionFromGuid(g2.ToString());

                Value = val2;

                DevicePath = val1;

                Category = g2f;

                return;
            }


            ProcessPropertyViewDefault(rawBytes);
        }
    }

    /// <summary>
    ///     Last access time of BagPath
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    public PropertyStore PropertyStore { get; private set; }


    public string Category { get; }
    public string DevicePath { get; }

    private void ProcessPropertyViewDefault(byte[] rawBytes)
    {
        FriendlyName = "Users property view";
        var index = 10;

        if (rawBytes.Length < index + 2)
        {
            throw new Exception($"Encountered an error attempting to calculate PropertySheetList size - the source array is too small - is this file corrupt?");
        }

        var shellPropertySheetListSize = BitConverter.ToInt16(rawBytes, index);

        if (shellPropertySheetListSize <= 0 || shellPropertySheetListSize > rawBytes.Length - (index + 2)) // if the shellPropertySheetListSize is less than 0 OR larger than the amount of remaining data in the array - throw
        {
            throw new Exception($"Encountered an invalid property sheetlist size ({shellPropertySheetListSize}) - is this file corrupt?");
        }

        index += 2;

        if (rawBytes.Length < index + 2)
        {
            throw new Exception($"Encountered an error attempting to calculate identifier size - the source array is too small - is this file corrupt?");
        }

        var identifiersize = BitConverter.ToInt16(rawBytes, index);

        if (identifiersize <= 0 || identifiersize > rawBytes.Length - (index + 2)) // if the identifiersize is less than 0 OR larger than the amount of remaining data in the array
        {
            throw new Exception($"Encountered an invalid identifier size ({identifiersize}) - is this file corrupt?");
        }

        index += 2;

        var identifierData = new byte[identifiersize];

        Array.Copy(rawBytes, index, identifierData, 0, identifiersize);

        index += identifiersize;

        if (shellPropertySheetListSize > 0)
        {
            var propBytes = Helpers.SliceClamped(rawBytes, index, shellPropertySheetListSize);
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
                    extOffsets.AddRange(Helpers.FindExtensionBlockSignatures(propBytes));

                    foreach (var extOffset in extOffsets)
                    {
                        var binaryOffset = extOffset - 4;
                        var exSize = BitConverter.ToInt16(propBytes, binaryOffset);

                        var exBytes = Helpers.SliceClamped(propBytes, binaryOffset, exSize);

                        var signature1 = BitConverter.ToUInt32(exBytes, 4);


                        var block1 = Utils.GetExtensionBlockFromBytes(signature1, exBytes);

                        ExtensionBlocks.Add(block1);
                    }
                }
                catch (ArgumentException)
                {
                    // Syntax error in the regular expression
                }
            }
        }
        else


        {
            index += shellPropertySheetListSize;
        }

        index += 2; //move past end of property sheet terminator

        var rawguid = Utils.ExtractGuidFromShellItem(Helpers.SliceClamped(rawBytes, index, 16));
        index += 16;

        rawguid = Utils.ExtractGuidFromShellItem(Helpers.SliceClamped(rawBytes, index, 16));
        index += 16;

        var name = GuidMapping.GuidMapping.GetDescriptionFromGuid(rawguid);

        Value = name;

        var extBlockSize = BitConverter.ToInt16(rawBytes, index);

        if (extBlockSize > 0)
        {
            //process extension blocks

            while (extBlockSize > 0)
            {
                var extBytes = Helpers.SliceClamped(rawBytes, index, extBlockSize);

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
            var block = Utils.GetExtensionBlockFromBytes(signature, Helpers.SliceClamped(rawBytes, index, int.MaxValue));

            ExtensionBlocks.Add(block);

            index += size;
        }

        Value = foldername;
    }

    private void ProcessGuid2(byte[] rawBytes)
    {
        FriendlyName = "Root folder: GUID";

        var delegateGuid = Utils.ExtractGuidFromShellItem(Helpers.SliceClamped(rawBytes, 20, 16));

        var folderGuid = Utils.ExtractGuidFromShellItem(Helpers.SliceClamped(rawBytes, 36, 16));

        var foldername = Utils.GetFolderNameFromGuid(folderGuid);

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