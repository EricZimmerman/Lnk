using System;
using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0Xc3 : ShellBag
{
    public ShellBag0Xc3(byte[] rawBytes, int codepage=1252)
    {
        FriendlyName = "Network location";

        ExtensionBlocks = new List<IExtensionBlock>();

        var index = 0;

        var size = BitConverter.ToUInt16(rawBytes, index);
        index += 2;

        var classtypeIndicator = rawBytes[index] & 0x70;
        index += 1;

        var unknown1 = rawBytes[index];
        index += 1;

        var flags = rawBytes[index];
        index += 1;

        var len = 0;

        while (rawBytes[index + len] != 0x0)
        {
            len += 1;
        }

        var tempBytes = new byte[len];
        Array.Copy(rawBytes, index, tempBytes, 0, len);

        index += len;

        var location = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(tempBytes);

        while (rawBytes[index] == 0x0)
        {
            index += 1;
            if (index >= rawBytes.Length)
            {
                break;
            }
        }

        //TODO there is still more here. finish this
        //4D-69-63-72-6F-73-6F-66-74-20-4E-65-74-77-6F-72-6B-00-00-02-00-00-00
        //M-i-c-r-o-s-o-f-t- -N-e-t-w-o-r-k------

        Value = location;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}