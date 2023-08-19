using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

internal class ShellBagCDBurn : ShellBag
{
    public ShellBagCDBurn(byte[] rawBytes, int codepage=1252)
    {
        ShortName = string.Empty;

        PropertyStore = new PropertyStore();


        ExtensionBlocks = new List<IExtensionBlock>();


        FriendlyName = "CDBurn";

        var index = 8; //reset index to after signature

        index += 4; //skip 4 unknown
        index += 4; //skip 4 unknown
        index += 4; //skip 4 unknown

        var chunks = new List<byte[]>();

        while (index < rawBytes.Length)
        {
            var subshellitemdatasize = BitConverter.ToInt16(rawBytes, index);
            index += 2;

            if (subshellitemdatasize <= 0)
            {
                break;
            }

            if (subshellitemdatasize == 1)
            {
                //some kind of separator
                index += 2;
            }
            else
            {
                chunks.Add(rawBytes.Skip(index).Take(subshellitemdatasize).ToArray());
                index += subshellitemdatasize;
            }
        }

        var oldIndex = index;

        //if (chunks.Count != 2)
        //{
        //    Debug.WriteLine(chunks.Count);
        //}

        foreach (var bytes in chunks)
        {
            index = 0;

            var typeIndicator = bytes[index];
            index += 1;
            index += 1; //skip unknown empty value

            var filesize = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            FileSize = filesize;

            var modDate =
                Utils.ExtractDateTimeOffsetFromBytes(bytes.Skip(index).Take(4).ToArray());

            LastModificationTime = modDate;

            index += 4;

            index += 2; //skip 2 bytes for file attributes

            var len = 0;

            var shortName = string.Empty;

            //get position of beef0004

            var beefPos = BitConverter.ToString(bytes).IndexOf("04-00-EF-BE", StringComparison.InvariantCulture) /
                          3;
            beefPos = beefPos - 4; //add header back for beef

            var strLen = beefPos - index;

            if (typeIndicator == 0x35)
            {
                //unicode
                var tempString = Encoding.Unicode.GetString(bytes, index, strLen - 2);

                shortName = tempString;
                index += strLen;
            }
            else
            {
                //ascii

                while (bytes[index + len] != 0x0)
                {
                    len += 1;
                }

                var tempBytes = new byte[len];
                Array.Copy(bytes, index, tempBytes, 0, len);

                index += len;
                shortName = Encoding.ASCII.GetString(tempBytes);
            }


            ShortName = shortName;

            while (bytes[index] == 0x0)
            {
                index += 1;
            }

            var extsize = BitConverter.ToInt16(bytes, index);

            var signature = BitConverter.ToUInt32(bytes, index + 4);

            //TODO does this need to check if its a 0xbeef?? regex?
            var block = Utils.GetExtensionBlockFromBytes(signature, bytes.Skip(index).ToArray());

            ExtensionBlocks.Add(block);

            var beef0004 = block as Beef0004;
            if (beef0004 != null)
            {
                CreatedOnTime = beef0004.CreatedOnTime;
                LastAccessTime = beef0004.LastAccessTime;
                Value = beef0004.LongName;
            }
            else
            {
                Value = "!!! Unable to determine Value !!!";
            }
        }

        if (oldIndex + 5 < rawBytes.Length)
        {
            index = oldIndex + 2;

            _extraBag = new ShellBag0X31(rawBytes.Skip(index).ToArray(), codepage);

            foreach (var ex in _extraBag.ExtensionBlocks)
            {
                ExtensionBlocks.Add(new BeefPlaceHolder(null));
            }
        }
    }

    public PropertyStore PropertyStore { get; }

    private IShellBag _extraBag { get; }

    /// <summary>
    ///     last modified time of BagPath
    /// </summary>
    public DateTimeOffset? LastModificationTime { get; set; }

    /// <summary>
    ///     Created time of BagPath
    /// </summary>
    public DateTimeOffset? CreatedOnTime { get; set; }

    /// <summary>
    ///     Last access time of BagPath
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    ///// <summary>
    /////     For files and directories, the MFT entry #
    ///// </summary>
    //public long? MFTEntryNumber { get; set; }

    ///// <summary>
    /////     For files and directories, the MFT sequence #
    ///// </summary>
    //public int? MFTSequenceNumber { get; set; }

    public int FileSize { get; }

    public string Miscellaneous { get; private set; }


    public string ShortName { get; }


    public override string ToString()
    {
        var sb = new StringBuilder();

        if (ShortName.Length > 0)
        {
            sb.AppendLine($"Short name: {ShortName}");
        }


        //TODO denote custom properties vs standard ones
        if (LastModificationTime.HasValue)
        {
            sb.AppendLine($"Modified On: {LastModificationTime.Value}");
        }


        if (PropertyStore.Sheets.Count > 0)
        {
            sb.AppendLine("Property Sheets");

            sb.AppendLine(PropertyStore.ToString());
        }
        else
        {
            sb.AppendLine();
        }


        sb.AppendLine(base.ToString());


        if (_extraBag != null)
        {
            sb.AppendLine();
            sb.AppendLine("This CDBurn ShellBag contains an additional ShellBag as shown below");
            sb.AppendLine();
            sb.AppendLine(_extraBag.ToString());

            sb.AppendLine();
            sb.AppendLine("End additional ShellBag");
        }

        return sb.ToString();
    }
}