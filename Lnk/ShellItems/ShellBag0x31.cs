using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X31 : ShellBag
{
    public ShellBag0X31(byte[] rawBytes, int codepage=1252)
    {
        FriendlyName = "Directory";


        ExtensionBlocks = new List<IExtensionBlock>();


        var index = 2;
        if (rawBytes.Length>0x29 && ( rawBytes[0x27] == 0x00 && rawBytes[0x28] == 0x2f && rawBytes[0x29] == 0x00 ||
                                      rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41))
        {
            //we have a good date

            if (rawBytes[0x28] == 0x2f || rawBytes[0x26] == 0x2f || rawBytes[0x1a] == 0x2f ||
                rawBytes[0x1c] == 0x2f)
                // forward slash in date or N / A
            {
                //zip?
                    

                try
                {
                    var zip = new ShellBagZipContents(rawBytes);
                    FriendlyName = zip.FriendlyName;
                    LastAccessTime = zip.LastAccessTime;

                    Value = zip.Value;

                    if (Value.Contains("Unable to determine value") == false)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                }


            }
        }

        index += 1;

        //skip unknown byte
        index += 1;

        index += 4; // skip file size since always 0 for directory

        LastModificationTime = Utils.ExtractDateTimeOffsetFromBytes(rawBytes.Skip(index).Take(4).ToArray());

        index += 4;

        index += 2;

        var len = 0;

        var beefPos = BitConverter.ToString(rawBytes).IndexOf("04-00-EF-BE", StringComparison.InvariantCulture) / 3;

        beefPos = beefPos - 4; //add header back for beef

        var strLen = beefPos - index;

        if (strLen < 0)
        {
            len = 2;
            while (rawBytes[index + len] != 0x0)
            {
                len += 2;
            }
        }
        else if (rawBytes[2] == 0x35|| rawBytes[2] == 0x36)
        {
            len = strLen;
        }
        else
        {
            while (rawBytes[index + len] != 0x0)
            {
                len += 1;
            }
        }

        var tempBytes = new byte[len];
        Array.Copy(rawBytes, index, tempBytes, 0, len);

        index += len;

        var shortName = "";

        if (rawBytes[2] == 0x35 || rawBytes[2] == 0x36)
        {
            shortName = Encoding.Unicode.GetString(tempBytes).Trim('\0');
        }
        else
        {
            shortName = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(tempBytes).Trim('\0');
        }

        ShortName = shortName;

        Value = shortName;

        if (rawBytes.Length == index)
        {
            return;
        }

        while (rawBytes[index] == 0x0)
        {
              
                
            index += 1;

            if (rawBytes.Length == index)
            {
                return;
            }
        }

        // here is where we need to cut up the rest into extension blocks
        var chunks = new List<byte[]>();

        while (index < rawBytes.Length)
        {
            var subshellitemdatasize = BitConverter.ToInt16(rawBytes, index);

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

        foreach (var bytes in chunks)
        {
            index = 0;

            var extsize = BitConverter.ToInt16(bytes, index);

            if (bytes.Length < 8)
            {
                return;
            }

            var signature = BitConverter.ToUInt32(bytes, 0x04);

            //TODO does this need to check if its a 0xbeef?? regex?
            var block = Utils.GetExtensionBlockFromBytes(signature, bytes);

            if (block.Signature.ToString("X").StartsWith("BEEF00"))
            {
                ExtensionBlocks.Add(block);
            }


            var beef0004 = block as Beef0004;
            if (beef0004 != null)
            {
                Value = beef0004.LongName;
                if (beef0004.LongName.Length == 0)
                {
                    Value = beef0004.LocalisedName;
                }
            
            }

            var beef0005 = block as Beef0005;
            if (beef0005 != null)
            {
                //TODO Resolve this
//                    foreach (var internalBag in beef0005.InternalBags)
//                    {
//                        ExtensionBlocks.Add(new BeefPlaceHolder(null));
//
//
//                    }
            }

            index += extsize;
        }
    }

    /// <summary>
    ///     last modified time of BagPath
    /// </summary>
    public DateTimeOffset? LastModificationTime { get; set; }


    /// <summary>
    ///     Last access time of BagPath
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    public string ShortName { get; }


    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(base.ToString());

        if (ShortName.Length > 0)
        {
            sb.AppendLine($"Short name: {ShortName}");
        }

        if (LastModificationTime.HasValue)
        {
            sb.AppendLine($"Modified: {LastModificationTime.Value}");
        }

        if (LastAccessTime.HasValue)
        {
            sb.AppendLine($"Last Access: {LastAccessTime.Value}");
        }

        sb.AppendLine();

        return sb.ToString();
    }
}