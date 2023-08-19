using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X1F : ShellBag
{
    private readonly List<PropertySheet> Sheets;


    public ShellBag0X1F(byte[] rawBytes, int codepage=1252)
    {
        ExtensionBlocks = new List<IExtensionBlock>();

        PropertyStore = new PropertyStore();

        Sheets = new List<PropertySheet>();

        var index = 0;

        if (rawBytes[0] == 0x14) //This is a GUID only
        {
            ProcessGuid(rawBytes);
            return;
        }

        if (rawBytes[4] == 0x2f)
        {
            index = 13;

            var dl = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, 3);

            FriendlyName = "Users property view: Drive letter";

            Value = dl;

            return;

            //There are GUIDs at the end but they arent useful
        }

        var off3 = rawBytes[3];
        var off3Bitmask = off3 & 0x70;

        switch (off3Bitmask)
        {
            case 0x00:

                break;
            case 0x40:
            case 0x50:


            {
                ProcessGuid(rawBytes);

                index += 20;
                var extsize = BitConverter.ToInt16(rawBytes, index);

                while (extsize>0)
                {
                    var signature = BitConverter.ToUInt32(rawBytes, index + 4);

                    var block = Utils.GetExtensionBlockFromBytes(signature, rawBytes.Skip(index).ToArray());

                    ExtensionBlocks.Add(block);
                    index += extsize;

                    if (index == rawBytes.Length)
                    {
                        return;
                    }


                    extsize = BitConverter.ToInt16(rawBytes, index);

                }

                if (index == rawBytes.Length)
                {
                    return;
                }

                var lastSize = BitConverter.ToInt16(rawBytes, index);

                if (lastSize != 0)
                {
                    Debug.WriteLine("remaining data!!!");
                }

                return;
            }


            case 0x60:

                break;
            case 0x70:
                ProcessGuid(rawBytes);
                return;

            default:
                throw new Exception($"unknown off3bitmask: 0x{off3Bitmask:X2}");
        }


        var dataSig = BitConverter.ToUInt32(rawBytes, 6);

        if (dataSig == 0xbeebee00)
        {
            ProcessPropertyViewDefault(rawBytes);
            return;
        }

        if (dataSig == 0x4c644970)
        {
            ProcessWindowsBackup(rawBytes);

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

        bin.ReadByte(); //skip indicator (0x1F)/

        var sortIndicator = bin.ReadByte();

        var dataSize = bin.ReadUInt16(); // BitConverter.ToUInt16(rawBytes, index);

        var dataSignature = bin.ReadUInt32(); //  BitConverter.ToUInt32(rawBytes, index);

        //Debug.WriteLine(dataSignature);
        //Debug.WriteLine(dataSignature.ToString("X"));

        var propertyStoreSize = bin.ReadUInt16(); //BitConverter.ToUInt16(rawBytes, index);

        var identifierSize = bin.ReadUInt16(); //BitConverter.ToUInt16(rawBytes, index);

        if (identifierSize > 0)
        {
            bin.ReadBytes(identifierSize);
            //index += identifierSize; // whts here?
        }

        if (propertyStoreSize > 0)
        {
            // var propStore = new PropertyStore(rawBytes.Skip(index).Take(propertyStoreSize).ToArray());

            var propertysheetBytes = bin.ReadBytes(propertyStoreSize);

            var propStore = new PropertyStore(propertysheetBytes);

            //TODO remove this hack once you have autolist parsing done. find it in all related classes as well
//                var p = propStore.Sheets.Where(t => t.PropertyNames.ContainsKey("AutoList"));
//
//                if (p.Any())
//                {
//                    //we can now look thry prop bytes for extension blocks
//                    //TODO this is a hack until we can process vectors natively
//
//                    var extOffsets = new List<int>();
//                    try
//                    {
//                        var regexObj = new Regex("([0-9A-F]{2})-00-EF-BE", RegexOptions.IgnoreCase);
//                        var matchResult = regexObj.Match(BitConverter.ToString(propertysheetBytes));
//                        while (matchResult.Success)
//                        {
//                            extOffsets.Add(matchResult.Index);
//                            matchResult = matchResult.NextMatch();
//                        }
//
//                        foreach (var extOffset in extOffsets)
//                        {
//                            var binaryOffset = extOffset / 3 - 4;
//                            var exSize = BitConverter.ToInt16(propertysheetBytes, binaryOffset);
//
//                            var exBytes = propertysheetBytes.Skip(binaryOffset).Take(exSize).ToArray();
//
//                            var signature1 = BitConverter.ToUInt32(exBytes, 4);
//
//                            var block1 = ShellBagUtils.GetExtensionBlockFromBytes(signature1, exBytes);
//
//                            ExtensionBlocks.Add(block1);
//                        }
//                    }
//                    catch (ArgumentException )
//                    {
//                        // Syntax error in the regular expression
//                    }
//
//                }

            PropertyStore = propStore;
        }

        //index += propertyStoreSize;

        bin.ReadBytes(2); //skip end of property sheet marker

        if (bin.BaseStream.Position == rawBytes.Length)
        {
            //hack
            index = 0x4;
            var gb = new byte[16];
            Buffer.BlockCopy(rawBytes, index, gb, 0, 16);
            var g = new Guid(gb);
            var gf = GuidMapping.GuidMapping.GetDescriptionFromGuid(g.ToString());

            Value = gf;

            index += 16;

            var exSize = BitConverter.ToInt16(rawBytes, index);

            var exBytes = rawBytes.Skip(index).Take(exSize).ToArray();

            var signature1 = BitConverter.ToUInt32(exBytes, 4);

            var block1 = Utils.GetExtensionBlockFromBytes(signature1, exBytes);

            ExtensionBlocks.Add(block1);

            return;
        }

        var rawguid = Utils.ExtractGuidFromShellItem(bin.ReadBytes(16));
        //    index += 16;

        rawguid = Utils.ExtractGuidFromShellItem(bin.ReadBytes(16));
        //   index += 16;

        var name = GuidMapping.GuidMapping.GetDescriptionFromGuid(rawguid);

        var extsize1 = bin.ReadUInt16(); // BitConverter.ToUInt16(rawBytes, index);

        if (extsize1 > 0)
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

                var block1 = Utils.GetExtensionBlockFromBytes(signature1, extBytes);

                ExtensionBlocks.Add(block1);
            }

            
        }

        Value = name;
    }

    public PropertyStore PropertyStore { get; private set; }

    /// <summary>
    ///     Last access time of BagPath
    /// </summary>
    public DateTimeOffset? ModifiedDateFromBackup { get; set; }

    public DateTimeOffset? CreatedDateFromBackup { get; set; }
    public DateTimeOffset? BackupDateTime { get; set; }
    public DateTimeOffset? BackupUnknownDateTime { get; set; }
    public DateTimeOffset? LastAccessTime { get; set; }

    private void ProcessWindowsBackup(byte[] rawBytes)
    {
        int index;
        FriendlyName = "Windows Backup";

        index = 0xc;

        BackupDateTime = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
        index += 8;
        ModifiedDateFromBackup =
            DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
        index += 8;
        CreatedDateFromBackup = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
        index += 8;
        BackupUnknownDateTime = DateTimeOffset.FromFileTime(BitConverter.ToInt64(rawBytes, index)).ToUniversalTime();
        index += 8;

        index += 12; //unknown

        var nameLen = BitConverter.ToInt16(rawBytes, index);
        index += 2;

        Value = Encoding.Unicode.GetString(rawBytes, index, nameLen * 2);
        index += nameLen * 2;

        index += 4; //unknown

        var rawGuidBytes = new byte[16];
        Buffer.BlockCopy(rawBytes, index, rawGuidBytes, 0, 16);

        var rawguid1 = Utils.ExtractGuidFromShellItem(rawGuidBytes);
        index += 16;

        rawGuidBytes = new byte[16];
        Buffer.BlockCopy(rawBytes, index, rawGuidBytes, 0, 16);

        var rawguid2 = Utils.ExtractGuidFromShellItem(rawGuidBytes);
        index += 16;

        var folder = GuidMapping.GuidMapping.GetDescriptionFromGuid(rawguid2);

        index += 2; //end of the line
    }

    private void ProcessGuid(byte[] rawBytes)
    {
        FriendlyName = "Root folder: GUID";

        var index = 2;

        index += 2; // move past index and a single unknown value

        var rawguid1 = new byte[16];

        Array.Copy(rawBytes, index, rawguid1, 0, 16);

        var rawguid = Utils.ExtractGuidFromShellItem(rawguid1);

        var foldername = GuidMapping.GuidMapping.GetDescriptionFromGuid(rawguid);

        index += 16;

        Value = foldername;
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
                catch (ArgumentException)
                {
                    // Syntax error in the regular expression
                }
            }
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
            valuestring = "No Property sheets found";
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

        if (ModifiedDateFromBackup.HasValue)
        {
            sb.AppendLine(
                $"Modified Date From Backup: {ModifiedDateFromBackup.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            sb.AppendLine();
        }

        if (CreatedDateFromBackup.HasValue)
        {
            sb.AppendLine(
                $"Created Date From Backup: {CreatedDateFromBackup.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            sb.AppendLine();
        }

        if (BackupDateTime.HasValue)
        {
            sb.AppendLine(
                $"Backup Date Time: {BackupDateTime.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            sb.AppendLine();
        }

        if (BackupUnknownDateTime.HasValue)
        {
            sb.AppendLine(
                $"Backup Unknown Date Time: {BackupUnknownDateTime.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
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