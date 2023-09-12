using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X2F : ShellBag
{
    public ShellBag0X2F(byte[] rawBytes, int codepage=1252)
    {
        FriendlyName = "Drive letter";

        ExtensionBlocks = new List<IExtensionBlock>();

        var driveLetter = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, 3, 2);

        Value = driveLetter;

        if (rawBytes.Length > 0x30)
        {
            var index = 0x19;

            var signature1 = BitConverter.ToUInt32(rawBytes, index + 4);

            var block1 = Utils.GetExtensionBlockFromBytes(signature1, rawBytes.Skip(index).ToArray());

            ExtensionBlocks.Add(block1);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}