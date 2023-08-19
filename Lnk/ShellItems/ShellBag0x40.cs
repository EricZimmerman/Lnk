using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X40 : ShellBag
{
    private string _desc;

    public ShellBag0X40(byte[] rawBytes, int codepage=1252)
    {
        ExtensionBlocks = new List<IExtensionBlock>();

        switch (rawBytes[2])
        {
            case 0x47:
                FriendlyName = "Entire Network";
                break;
            case 0x46:
                FriendlyName = "Microsoft Windows Network";
                break;
            case 0x41:
                FriendlyName = "Domain/Workgroup name";
                break;

            case 0x42:
                FriendlyName = "Server UNC path";
                break;

            case 0x43:
                FriendlyName = "Share UNC path";
                break;

            default:
                FriendlyName = "Network location";
                break;
        }


        var temp = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, 5, rawBytes.Length - 5).Split('\0');

        _desc = temp[1];

        Value = temp[0];
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}