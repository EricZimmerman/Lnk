using System;
using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X4C : ShellBag
{
    public ShellBag0X4C(byte[] rawBytes)
    {
        ExtensionBlocks = new List<IExtensionBlock>();

        FriendlyName = "Sharepoint directory";

        var index = 0x1c;

        var len = BitConverter.ToInt16(rawBytes, index);
        index += 2;

        var name = Encoding.Unicode.GetString(rawBytes, index, len * 2).Trim('\0');
        index += len * 2;

        index += 2; //terminator

        len = BitConverter.ToInt16(rawBytes, index);
        index += 2;

        var name2 = Encoding.Unicode.GetString(rawBytes, index, len * 2).Trim('\0');
        index += len * 2;

        if (name2.Length == 0)
        {
            name2 = "URL not specified";
        }

        Value = $"{name} ({name2})";
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}