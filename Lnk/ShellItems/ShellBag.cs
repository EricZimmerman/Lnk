using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public abstract class ShellBag : IShellBag
{
    public string FriendlyName { get; set; }


    public string Value { get; set; }


    public List<IExtensionBlock> ExtensionBlocks { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Type: {FriendlyName}, Value: {Value}");

        if (ExtensionBlocks.Count > 0)
        {
            var extensionNumber = 0;

            sb.AppendLine();
            sb.AppendLine($"Extension blocks found: {ExtensionBlocks.Count}");

            foreach (var extensionBlock in ExtensionBlocks)
            {
                if (extensionBlock is BeefPlaceHolder)
                {
                    continue;
                }

                sb.AppendLine(
                    $"---------------------- Block {extensionNumber:N0} ({extensionBlock.GetType().Name})----------------------");

                if (extensionBlock is Beef0004)
                {
                    var b4 = extensionBlock as Beef0004;

                    var b4Sb = new StringBuilder();

                    b4Sb.AppendLine($"Long name: {b4.LongName}");
                    if (b4.LocalisedName.Length > 0)
                    {
                        b4Sb.AppendLine($"Localized name: {b4.LocalisedName}");
                    }

                    b4Sb.AppendLine($"Created: {b4.CreatedOnTime}");
                    b4Sb.AppendLine($"Last access: {b4.LastAccessTime}");
                    if (b4.MFTInformation.MFTEntryNumber > 0)
                    {
                        b4Sb.AppendLine(
                            $"MFT entry/sequence #: {b4.MFTInformation.MFTEntryNumber}/{b4.MFTInformation.MFTSequenceNumber} (0x{b4.MFTInformation.MFTEntryNumber:X}/0x{b4.MFTInformation.MFTSequenceNumber:X})");
                        if (b4.MFTInformation.Note.Length > 0)
                        {
                            b4Sb.AppendLine($"File system hint: {b4.MFTInformation.Note}");
                        }
                    }


                    sb.Append(b4Sb);
                }
                else if (extensionBlock is Beef0025)
                {
                    var b25 = extensionBlock as Beef0025;

                    var b25Sb = new StringBuilder();

                    b25Sb.AppendLine($"Filetime 1: {b25.FileTime1}");
                    b25Sb.AppendLine($"Filetime 2: {b25.FileTime2}");

                    sb.Append(b25Sb);
                }
                else if (extensionBlock is Beef0003)
                {
                    var b3 = extensionBlock as Beef0003;

                    var b3Sb = new StringBuilder();

                    b3Sb.AppendLine($"GUID: {b3.GUID1} ({b3.GUID1Folder})");

                    sb.Append(b3Sb);
                }
                else
                {
                    sb.AppendLine(extensionBlock.ToString());
                }

                extensionNumber += 1;
                sb.AppendLine();
            }

            sb.Append("--------------------------------------------------");
        }


        return sb.ToString();
    }
}