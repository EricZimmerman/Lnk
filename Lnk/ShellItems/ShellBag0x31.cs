using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems;

public class ShellBag0X31 : ShellBag
{
    public ShellBag0X31(byte[] rawBytes, int codepage = 1252)
    {
        FriendlyName = "Directory";
        ExtensionBlocks = new List<IExtensionBlock>();

        // Basic validation of rawBytes length
        if (rawBytes == null || rawBytes.Length < 10)
        {
            Value = "Invalid or corrupted data";
            return;
        }

        var index = 2;

        // ShellBag check
        if (rawBytes.Length > 0x29 &&
            IsValidShellBagCheck(rawBytes))
        {
            if (HasForwardSlashInDate(rawBytes))
            {
                try
                {
                    var _shellBag = new ShellBagZipContents(rawBytes);
                    FriendlyName = _shellBag.FriendlyName;
                    LastAccessTime = _shellBag.LastAccessTime;
                    Value = _shellBag.Value;

                    if (!string.IsNullOrEmpty(Value) && !Value.Contains("Unable to determine value"))
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // continue if parsing process failed!
                }
            }
        }

        // Index validation
        if (!IsValidIndex(rawBytes, index + 9))
        {
            Value = "Corrupted data - insufficient bytes";
            return;
        }

        index += 1; // skip unknown byte
        index += 1;
        index += 4; // skip file size

        // LastModificationTime extraction
        if (IsValidIndex(rawBytes, index + 3))
        {
            LastModificationTime = Utils.ExtractDateTimeOffsetFromBytes(
                rawBytes.Skip(index).Take(4).ToArray());
        }
        index += 4;
        index += 2;

        // Find BEEF position and calculate string length
        var beefPos = FindBeefPosition(rawBytes);
        var strLen = CalculateStringLength(rawBytes, index, beefPos);

        if (strLen <= 0 || !IsValidIndex(rawBytes, index + strLen - 1))
        {
            Value = "Invalid string data";
            return;
        }

        // String extraction
        var shortName = ExtractShortName(rawBytes, index, strLen, codepage);
        ShortName = shortName;
        Value = shortName;

        index += strLen;

        // Boundary check after string extraction
        if (index >= rawBytes.Length)
        {
            return;
        }

        // Skip Null bytes
        index = SkipNullBytes(rawBytes, index);

        if (index >= rawBytes.Length)
        {
            return;
        }

        // Process Extension blocks
        ProcessExtensionBlocks(rawBytes, index);
    }

    /// <summary>
    /// Checks if the index is valid for the byte array
    /// </summary>
    private bool IsValidIndex(byte[] bytes, int index)
    {
        return bytes != null && index >= 0 && index < bytes.Length;
    }

    /// <summary>
    /// Adjusted ShellBag check for byte contents
    /// </summary>
    private bool IsValidShellBagCheck(byte[] rawBytes)
    {
        try
        {
            return (rawBytes[0x27] == 0x00 && rawBytes[0x28] == 0x2f && rawBytes[0x29] == 0x00) ||
                   (rawBytes[0x24] == 0x4e && rawBytes[0x26] == 0x2f && rawBytes[0x28] == 0x41);
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Forward slash checks in date fields
    /// </summary>
    private bool HasForwardSlashInDate(byte[] rawBytes)
    {
        try
        {
            return rawBytes[0x28] == 0x2f || rawBytes[0x26] == 0x2f ||
                   rawBytes[0x1a] == 0x2f || rawBytes[0x1c] == 0x2f;
        }
        catch (IndexOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// The finds the position of the BEEF marker in the byte array
    /// </summary>
    private int FindBeefPosition(byte[] rawBytes)
    {
        try
        {
            var beefPos = BitConverter.ToString(rawBytes)
                .IndexOf("04-00-EF-BE", StringComparison.InvariantCulture) / 3;
            return beefPos > 4 ? beefPos - 4 : -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Calculate the string length based on the BEEF position
    /// </summary>
    private int CalculateStringLength(byte[] rawBytes, int index, int beefPos)
    {
        try
        {
            var strLen = beefPos - index;

            if (strLen < 0 || beefPos == -1)
            {
                // Manuel olarak null terminator ara
                var len = rawBytes[2] == 0x35 || rawBytes[2] == 0x36 ? 2 : 1;
                var maxSearch = Math.Min(rawBytes.Length - index, 256); // Maksimum arama limiti

                while (len < maxSearch && IsValidIndex(rawBytes, index + len) && rawBytes[index + len] != 0x0)
                {
                    len += rawBytes[2] == 0x35 || rawBytes[2] == 0x36 ? 2 : 1;
                }

                return len;
            }

            return strLen;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Extracts the short name from the byte array
    /// </summary>
    private string ExtractShortName(byte[] rawBytes, int index, int length, int codepage)
    {
        try
        {
            if (length <= 0 || !IsValidIndex(rawBytes, index + length - 1))
            {
                return string.Empty;
            }

            var tempBytes = new byte[length];
            Array.Copy(rawBytes, index, tempBytes, 0, length);

            if (rawBytes.Length > 2 && (rawBytes[2] == 0x35 || rawBytes[2] == 0x36))
            {
                return Encoding.Unicode.GetString(tempBytes).Trim('\0');
            }
            else
            {
                var encoding = CodePagesEncodingProvider.Instance.GetEncoding(codepage);
                return encoding?.GetString(tempBytes).Trim('\0') ?? string.Empty;
            }
        }
        catch
        {
            return "Corrupted string data";
        }
    }

    /// <summary>
    /// Skip null bytes in the byte array
    /// </summary>
    private int SkipNullBytes(byte[] rawBytes, int startIndex)
    {
        var index = startIndex;
        while (IsValidIndex(rawBytes, index) && rawBytes[index] == 0x0)
        {
            index++;
        }
        return index;
    }

    /// <summary>
    /// Processes the extension blocks from the byte array
    /// </summary>
    private void ProcessExtensionBlocks(byte[] rawBytes, int startIndex)
    {
        try
        {
            var chunks = new List<byte[]>();
            var index = startIndex;

            while (IsValidIndex(rawBytes, index + 1))
            {
                var subshellitemdatasize = BitConverter.ToInt16(rawBytes, index);

                if (subshellitemdatasize <= 0)
                {
                    break;
                }

                if (subshellitemdatasize == 1)
                {
                    index += 2;
                }
                else if (IsValidIndex(rawBytes, index + subshellitemdatasize - 1))
                {
                    chunks.Add(rawBytes.Skip(index).Take(subshellitemdatasize).ToArray());
                    index += subshellitemdatasize;
                }
                else
                {
                    break; // Out of bounds, stop processing
                }
            }

            ProcessChunks(chunks);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Extension block processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes the chunks of byte arrays to extract extension blocks
    /// </summary>
    private void ProcessChunks(List<byte[]> chunks)
    {
        foreach (var bytes in chunks)
        {
            try
            {
                if (bytes.Length < 8)
                {
                    continue;
                }

                var extsize = BitConverter.ToInt16(bytes, 0);
                var signature = BitConverter.ToUInt32(bytes, 0x04);

                var block = Utils.GetExtensionBlockFromBytes(signature, bytes);

                if (block?.Signature.ToString("X").StartsWith("BEEF00") == true)
                {
                    ExtensionBlocks.Add(block);
                }

                UpdateValueFromExtensionBlock(block);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chunk processing error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Updates the Value property based on the extension block
    /// </summary>
    private void UpdateValueFromExtensionBlock(IExtensionBlock block)
    {
        try
        {
            if (block is Beef0004 beef0004)
            {
                Value = !string.IsNullOrEmpty(beef0004.LongName) ?
                        beef0004.LongName : beef0004.LocalisedName;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Value update error: {ex.Message}");
        }
    }

    /// <summary>
    /// last modified time of BagPath
    /// </summary>
    public DateTimeOffset? LastModificationTime { get; set; }

    /// <summary>
    /// Last access time of BagPath
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    public string ShortName { get; private set; } = string.Empty;

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine(base.ToString());

        if (!string.IsNullOrEmpty(ShortName))
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
