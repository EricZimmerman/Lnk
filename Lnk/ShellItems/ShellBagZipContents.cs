using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBagZipContents : ShellBag
{
    public ShellBagZipContents(byte[] rawBytes)
    {
        ExtensionBlocks = new List<IExtensionBlock>();

        FriendlyName = "Zip file contents";

        //we have a good date or an N/A date

        //for xp it looks like this
        //N / A
        //0x18 == 4e
        //0x1A == 2F
        //0x1c == 41
        // or with a date
        //0x1c == 2f
        //0x1d == 00
        //0x22 == 2f
        //0x23 == 00

        //else this
        //N / A
        //0x24 == 4e
        //0x26 == 2F
        //0x28 == 41
        //or with a date
        //


        var rawdate = rawBytes.Skip(0x24).Take(40).ToArray();

        var rawdatestring = Encoding.Unicode.GetString(rawdate).Split('\0').First();
        LastAccessTimeString = rawdatestring;

        DateTimeOffset lastaccess;

        if (DateTimeOffset.TryParse(rawdatestring, out lastaccess))
        {
            LastAccessTime = lastaccess;
        }
        else
        {
            rawdate = rawBytes.Skip(0x18).Take(40).ToArray();
            rawdatestring = Encoding.Unicode.GetString(rawdate).Split('\0').First();

            LastAccessTimeString = rawdatestring;

            if (DateTimeOffset.TryParse(rawdatestring, out lastaccess))
            {
                LastAccessTime = lastaccess;
            }
        }

        var index = 84;

        if (rawBytes[0x14] == 0x10) //xp hackz
        {
            index = 60;
        }

        try
        {
            var nameSize1 = BitConverter.ToUInt16(rawBytes, index);
            index += 4;

            var nameSize2 = BitConverter.ToUInt16(rawBytes, index);
            index += 4;

            Value = "!!!Unable to determine value!!!";

            if (nameSize1 > 0)
            {
                var folderName1 = Encoding.Unicode.GetString(rawBytes, index, nameSize1 * 2);
                index += nameSize1 * 2;

                Value = folderName1;

                index += 2; // skip end of unicode string
            }

            if (nameSize2 > 0)
            {
                var folderName2 = Encoding.Unicode.GetString(rawBytes, index, nameSize2 * 2);
                index += nameSize2 * 2;

                //  Value = string.Format("{0}/{1}", Value, folderName2);

                index += 2; // skip end of unicode string
            }
        }
        catch (Exception)
        {
            index = 60;
            var nameSize1 = BitConverter.ToUInt16(rawBytes, index);
            index += 4;

            var nameSize2 = BitConverter.ToUInt16(rawBytes, index);
            index += 4;

            if (nameSize1 > 0)
            {
                var folderName1 = Encoding.Unicode.GetString(rawBytes, index, nameSize1 * 2);
                index += nameSize1 * 2;

                Value = folderName1;

                index += 2; // skip end of unicode string
            }

            if (nameSize2 > 0)
            {
                var folderName2 = Encoding.Unicode.GetString(rawBytes, index, nameSize2 * 2);
                index += nameSize2 * 2;

                index += 2; // skip end of unicode string
            }
        }


        // testing shows this is not usable data:
        //while (index < rawBytes.Length)
        //{
        //    if (rawBytes[index] != 0x0)
        //    {
        //        SiAuto.Main.LogWarning("Found data where there shouldnt be any?? In Variable: Zip file contents: {0}",BitConverter.ToString(rawBytes,index));
        //    }
        //    index += 1;
        //}
    }

    /// <summary>
    ///     Last access time of BagPath
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; }

    public string LastAccessTimeString { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();


        if (LastAccessTimeString.Equals("N/A") == false)
        {
            sb.AppendLine($"Last access internal value: {LastAccessTimeString}");
            sb.AppendLine();
        }

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}