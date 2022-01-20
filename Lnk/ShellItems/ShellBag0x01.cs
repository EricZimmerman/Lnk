using System;
using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X01 : ShellBag
{
    public ShellBag0X01(byte[] rawBytes)
    {
        DriveLetter = string.Empty;

        FriendlyName = "Control Panel Category";

        ExtensionBlocks = new List<IExtensionBlock>();

        // another special case when dealing with at least Hyper-V browsing
        if (rawBytes[8] == 0x3A && rawBytes[9] == 0x00)
        {
            FriendlyName = "Hyper-V storage volume";

            DriveLetter = Encoding.Unicode.GetString(rawBytes, 0x6, 4).Replace("\0", string.Empty);

            Value = Encoding.Unicode.GetString(rawBytes, 0x32, rawBytes.Length - 0x32).Replace("\0", string.Empty);

            return;
        }

        var specialDataSig = BitConverter.ToUInt32(rawBytes, 4);

        if (specialDataSig != 0x39de2184)
        {
            Value = Encoding.Unicode.GetString(rawBytes, 14, rawBytes.Length - 14).Replace("\0", string.Empty);
            return;
        }


        //this is the general case, for actual control panel categories

        //The key value is in offset 8
        switch (rawBytes[8])
        {
            case 0x00:
                Value = "All Control Panel Items";

                break;

            case 0x01:
                Value = "Appearance and Personalization";

                break;

            case 0x02:
                Value = "Hardware and Sound";

                break;

            case 0x03:
                Value = "Network and Internet";

                break;

            case 0x04:
                Value = "Sound, Speech and Audio Devices";

                break;

            case 0x05:
                Value = "System and Security";

                break;

            case 0x06:
                Value = "Clock, Language, and Region";

                break;

            case 0x07:
                Value = "Ease of Access";

                break;

            case 0x08:
                Value = "Programs";

                break;

            case 0x09:
                Value = "User Accounts";

                break;

            case 0x10:
                Value = "Security Center";

                break;

            case 0x11:
                Value = "Mobile PC";

                break;

            default:
                Value = $"Unknown category! Category ID: {rawBytes[8]}";

                break;
        }
    }

    public string DriveLetter { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        if (DriveLetter.Length > 0)
        {
            sb.AppendLine($"Drive Letter: {DriveLetter}");
            sb.AppendLine();
        }

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}