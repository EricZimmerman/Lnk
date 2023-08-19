using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X74 : ShellBag
{
    public ShellBag0X74(byte[] rawBytes, int codepage=1252)
    {
        FriendlyName = "Users Files Folder";

        ExtensionBlocks = new List<IExtensionBlock>();

        var index = 2;

        index += 2; // move past type  and an unknown

        var size = BitConverter.ToUInt16(rawBytes, index);

        index += 2;

        var sig74 = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, 4);

        if (sig74 == "CF\0\0")
        {
            if (rawBytes[0x28] == 0x2f ||
                rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41)
            {
                //we have a good date

                var zip = new ShellBagZipContents(rawBytes);
                FriendlyName = zip.FriendlyName;
                LastAccessTime = zip.LastAccessTime;

                Value = zip.Value;

                return;
            }
        }

        if (sig74 != "CFSF")
        {
            throw new Exception($"Invalid signature! Should be CFSF but was {sig74}");
        }

        index += 4;

        var subShellSize = BitConverter.ToUInt16(rawBytes, index);
        index += 2;

        var subClasstype = rawBytes[index];
        index += 1;

        index += 1; // skip unknown

        var filesize = BitConverter.ToUInt32(rawBytes, index);

        index += 4;

        FileSize = (int) filesize;

        var tempBytes = new byte[4];
        Array.Copy(rawBytes, index, tempBytes, 0, 4);

        index += 4;

        LastModificationTime = Utils.ExtractDateTimeOffsetFromBytes(tempBytes);

        index += 2; //skip file attribute flag

        var len = 0;

        while (rawBytes[index + len] != 0x0)
        {
            len += 1;
        }

        tempBytes = new byte[len];
        Array.Copy(rawBytes, index, tempBytes, 0, len);

        index += len;

        var primaryName = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(tempBytes);


        while (rawBytes[index] == 0x0)
        {
            index += 1;
        }

        var delegateGuidRaw = new byte[16];

        Array.Copy(rawBytes, index, delegateGuidRaw, 0, 16);


        var delegateGuid = Utils.ExtractGuidFromShellItem(delegateGuidRaw);

        if (delegateGuid != "5e591a74-df96-48d3-8d67-1733bcee28ba")
        {
            throw new Exception(
                $"Delegate guid not expected value of 5e591a74-df96-48d3-8d67-1733bcee28ba. Actual value: {delegateGuid}");
        }

        index += 16;

        var itemIdentifierGuidRaw = new byte[16];
        Array.Copy(rawBytes, index, itemIdentifierGuidRaw, 0, 16);

        var itemIdentifierGuid = Utils.ExtractGuidFromShellItem(itemIdentifierGuidRaw);

        var itemName = Utils.GetFolderNameFromGuid(itemIdentifierGuid);
        index += 16;

        //0xbeef0004 section

        //we are at extensnon blocks, so cut them up and process
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

            var signature = BitConverter.ToUInt32(bytes, 0x04);

            //TODO does this need to check if its a 0xbeef?? regex?
            var block = Utils.GetExtensionBlockFromBytes(signature, bytes);

            ExtensionBlocks.Add(block);

            var beef0004 = block as Beef0004;
            if (beef0004 != null)
            {
                Value = beef0004.LongName;
            }

            index += extsize;
        }
    }

    public int FileSize { get; }

    /// <summary>
    ///     last modified time of BagPath
    /// </summary>
    public DateTimeOffset? LastModificationTime { get; set; }


    /// <summary>
    ///     Last access time of BagPath
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }


    public override string ToString()
    {
        var sb = new StringBuilder();

        if (FileSize > 0)
        {
            sb.AppendLine($"File size: {FileSize:N0}");
        }

        if (LastModificationTime.HasValue)
        {
            sb.AppendLine($"Modified On: {LastModificationTime.Value}");
            sb.AppendLine();
        }

        sb.AppendLine(base.ToString());

        return sb.ToString();
    }
}