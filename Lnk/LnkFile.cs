using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Lnk.ExtraData;
using Lnk.ShellItems;

namespace Lnk;

public class LnkFile
{
    [Flags]
    public enum LocationFlag
    {
        [Description("The linked file is on a volume")] VolumeIdAndLocalBasePath = 0x0001,

        [Description("The linked file is on a network share")] CommonNetworkRelativeLinkAndPathSuffix = 0x0002
    }

    /// <summary>
    ///     Parses LNK data that does not come from a file on disk (carved data, jump list entries, etc.).
    ///     <see cref="SourceFile" /> and the Source* timestamps are null.
    /// </summary>
    public LnkFile(byte[] rawBytes, int codepage = 1252) : this(rawBytes, null, codepage)
    {
    }

    /// <summary>
    ///     Parses LNK data read from <paramref name="sourceFile" />. The file's own timestamps are recorded in
    ///     <see cref="SourceCreated" />, <see cref="SourceModified" /> and <see cref="SourceAccessed" /> when it exists.
    /// </summary>
    public LnkFile(byte[] rawBytes, string sourceFile, int codepage = 1252)
    {
        if (rawBytes == null)
        {
            throw new ArgumentNullException(nameof(rawBytes));
        }

        var errors = new List<string>();
        ParseErrors = errors;
        TargetIDs = new List<ShellBag>();
        ExtraBlocks = new List<ExtraDataBase>();

        try
        {
            RawBytes = rawBytes;

            Header = new Header(rawBytes);

            if (!string.IsNullOrEmpty(sourceFile))
            {
                SourceFile = Path.GetFullPath(sourceFile);

                var fi = new FileInfo(sourceFile);
                if (fi.Exists)
                {
                    SourceCreated = NullIf1601(fi.CreationTimeUtc);
                    SourceModified = NullIf1601(fi.LastWriteTimeUtc);
                    SourceAccessed = NullIf1601(fi.LastAccessTimeUtc);
                }
            }

            if (Header.TargetCreationDate == DateTimeOffset.MinValue)
            {
                errors.Add($"Target creation time is out of range (raw 0x{Header.TargetCreationDateRaw:X16})");
            }

            if (Header.TargetModificationDate == DateTimeOffset.MinValue)
            {
                errors.Add($"Target modification time is out of range (raw 0x{Header.TargetModificationDateRaw:X16})");
            }

            if (Header.TargetLastAccessedDate == DateTimeOffset.MinValue)
            {
                errors.Add($"Target last access time is out of range (raw 0x{Header.TargetLastAccessedDateRaw:X16})");
            }

            if (!Enum.IsDefined(typeof(Header.ShowWindowOption), Header.ShowWindow))
            {
                // Windows treats this as SW_SHOWNORMAL, but no legitimate shortcut writer produces it
                errors.Add($"ShowCommand 0x{(int) Header.ShowWindow:X} is not a defined SW_* value (treated as SW_SHOWNORMAL)");
            }

            var index = Header.HeaderSize;
            var flags = Header.DataFlags;
            var isUnicode = (flags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode;
            var encoding = Helpers.GetEncoding(codepage);

            // 2.2 LinkTargetIDList
            if ((flags & Header.DataFlag.HasTargetIdList) == Header.DataFlag.HasTargetIdList)
            {
                var idListSize = Helpers.ReadUInt16(rawBytes, index, "LinkTargetIDList size");
                index += 2;
                Helpers.EnsureRange(rawBytes, index, idListSize, "LinkTargetIDList");

                TargetIDs.AddRange(ShellItemFactory.ParseIdList(rawBytes, index, index + idListSize, codepage, errors));

                //TODO tie back extra block for SpecialFolderDataBlock and KnownFolderDataBlock??

                index += idListSize;
            }

            // 2.3 LinkInfo
            if ((flags & Header.DataFlag.HasLinkInfo) == Header.DataFlag.HasLinkInfo)
            {
                var linkInfoSize = Helpers.ReadUInt32(rawBytes, index, "LinkInfo size");
                if (linkInfoSize < 0x1C)
                {
                    throw new LnkParseException($"Invalid location item data size ({linkInfoSize}) - is this file corrupt?", index, "LinkInfoSize");
                }

                Helpers.EnsureRange(rawBytes, index, linkInfoSize, "LinkInfo");

                ParseLinkInfo(rawBytes, index, (int) linkInfoSize, codepage, encoding, errors, out var locationFlags,
                    out var volumeInfo, out var localPath, out var networkShareInfo, out var commonPath);

                LocationFlags = locationFlags;
                VolumeInfo = volumeInfo;
                LocalPath = localPath;
                NetworkShareInfo = networkShareInfo;
                CommonPath = commonPath;

                index += (int) linkInfoSize;
            }

            // 2.4 StringData. CountCharacters is an unsigned 16 bit count of characters (not bytes) and the
            // strings are not NUL terminated. Values are never truncated: padding a field such as Arguments
            // past 260 characters is a known way of hiding a payload from tools that do.
            if ((flags & Header.DataFlag.HasName) == Header.DataFlag.HasName)
            {
                Name = ReadStringData(rawBytes, ref index, isUnicode, encoding, "NAME_STRING");
            }

            if ((flags & Header.DataFlag.HasRelativePath) == Header.DataFlag.HasRelativePath)
            {
                RelativePath = ReadStringData(rawBytes, ref index, isUnicode, encoding, "RELATIVE_PATH");
            }

            if ((flags & Header.DataFlag.HasWorkingDir) == Header.DataFlag.HasWorkingDir)
            {
                WorkingDirectory = ReadStringData(rawBytes, ref index, isUnicode, encoding, "WORKING_DIR");
            }

            if ((flags & Header.DataFlag.HasArguments) == Header.DataFlag.HasArguments)
            {
                Arguments = ReadStringData(rawBytes, ref index, isUnicode, encoding, "COMMAND_LINE_ARGUMENTS");
            }

            if ((flags & Header.DataFlag.HasIconLocation) == Header.DataFlag.HasIconLocation)
            {
                IconLocation = ReadStringData(rawBytes, ref index, isUnicode, encoding, "ICON_LOCATION");
            }

            // 2.5 ExtraData: blocks until a TerminalBlock (BlockSize < 4)
            while (index + 4 <= rawBytes.Length)
            {
                var blockSize = BitConverter.ToUInt32(rawBytes, index);

                if (blockSize < 4)
                {
                    ExtraDataTerminated = true;
                    index += 4;
                    break;
                }

                var available = rawBytes.Length - index;
                byte[] blockBytes;

                if (blockSize < 8 || blockSize > available)
                {
                    // Malformed or truncated block: parse what is there if possible, report it, and stop.
                    blockBytes = Helpers.Slice(rawBytes, index, available, "ExtraData block");
                    errors.Add($"Extra data block at offset {index} declares {blockSize} bytes but {available} remain");
                    ExtraBlocks.Add(ParseExtraBlock(blockBytes, codepage));
                    index = rawBytes.Length;
                    break;
                }

                blockBytes = Helpers.Slice(rawBytes, index, blockSize, "ExtraData block");
                ExtraBlocks.Add(ParseExtraBlock(blockBytes, codepage));
                index += (int) blockSize;
            }

            // Anything after the TerminalBlock is not part of the format (and is sometimes used to smuggle a payload).
            TrailingDataSize = rawBytes.Length - index;
            if (!ExtraDataTerminated && TrailingDataSize == 0)
            {
                errors.Add("ExtraData has no terminal block");
            }
            else if (TrailingDataSize > 0)
            {
                errors.Add(ExtraDataTerminated
                    ? $"{TrailingDataSize:N0} bytes of data follow the ExtraData terminal block (offset {index})"
                    : $"ExtraData has no terminal block; {TrailingDataSize:N0} unparsed bytes at offset {index}");
            }

            foreach (var damaged in ExtraBlocks.OfType<DamagedDataBlock>())
            {
                errors.Add($"Extra data block {damaged.OriginalSignature}: {damaged.ErrorMessage}");
            }

            foreach (var vista in ExtraBlocks.OfType<VistaAndAboveIdListDataBlock>())
            {
                errors.AddRange(vista.ParseErrors.Select(e => $"VistaAndAboveIDListDataBlock: {e}"));
            }
        }
        catch (Exception ex)
        {
            throw new LnkParseException($"Error processing file ({sourceFile ?? "(raw data)"}) - the error was ({ex.Message})",
                (ex as LnkParseException)?.Offset ?? -1, (ex as LnkParseException)?.Field, ex);
        }
    }

    private static DateTimeOffset? NullIf1601(DateTime utc)
    {
        return utc.Year == 1601 ? null : new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static string ReadStringData(byte[] rawBytes, ref int index, bool isUnicode, Encoding encoding, string field)
    {
        var count = Helpers.ReadUInt16(rawBytes, index, field + " size");
        index += 2;

        var byteCount = isUnicode ? count * 2 : count;
        Helpers.EnsureRange(rawBytes, index, byteCount, field);

        var value = isUnicode
            ? Encoding.Unicode.GetString(rawBytes, index, byteCount)
            : encoding.GetString(rawBytes, index, byteCount);

        index += byteCount;
        return value;
    }

    // 2.3 LinkInfo. All offsets are relative to the start of the LinkInfo structure and must stay inside it.
    private static void ParseLinkInfo(byte[] rawBytes, int start, int size, int codepage, Encoding encoding,
        List<string> errors, out LocationFlag locationFlags, out VolumeInfo volumeInfo, out string localPath,
        out NetworkShareInfo networkShareInfo, out string commonPath)
    {
        var end = start + size;

        var headerSize = Helpers.ReadUInt32(rawBytes, start + 4, "LinkInfoHeaderSize", end);
        locationFlags = (LocationFlag) Helpers.ReadUInt32(rawBytes, start + 8, "LinkInfoFlags", end);
        var volumeIdOffset = Helpers.ReadUInt32(rawBytes, start + 12, "VolumeIDOffset", end);
        var localBasePathOffset = Helpers.ReadUInt32(rawBytes, start + 16, "LocalBasePathOffset", end);
        var networkOffset = Helpers.ReadUInt32(rawBytes, start + 20, "CommonNetworkRelativeLinkOffset", end);
        var commonPathSuffixOffset = Helpers.ReadUInt32(rawBytes, start + 24, "CommonPathSuffixOffset", end);

        // LocalBasePathOffsetUnicode / CommonPathSuffixOffsetUnicode exist only when LinkInfoHeaderSize >= 0x24
        uint localBasePathOffsetUnicode = 0;
        uint commonPathSuffixOffsetUnicode = 0;
        if (headerSize >= 0x24)
        {
            localBasePathOffsetUnicode = Helpers.ReadUInt32(rawBytes, start + 28, "LocalBasePathOffsetUnicode", end);
            commonPathSuffixOffsetUnicode = Helpers.ReadUInt32(rawBytes, start + 32, "CommonPathSuffixOffsetUnicode", end);
        }

        volumeInfo = null;
        localPath = null;
        networkShareInfo = null;
        commonPath = string.Empty;

        if ((locationFlags & LocationFlag.VolumeIdAndLocalBasePath) == LocationFlag.VolumeIdAndLocalBasePath)
        {
            try
            {
                if (volumeIdOffset > 0)
                {
                    var volumeIdSize = Helpers.ReadUInt32(rawBytes, start + volumeIdOffset, "VolumeIDSize", end);
                    if (volumeIdSize <= 0x10)
                    {
                        throw new LnkParseException($"Invalid volume data size ({volumeIdSize}) - is this file corrupt?",
                            (int) (start + volumeIdOffset), "VolumeIDSize");
                    }

                    volumeInfo = new VolumeInfo(
                        Helpers.Slice(rawBytes, start + volumeIdOffset, volumeIdSize, "VolumeID", end), codepage);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"VolumeID: {ex.Message}");
            }

            try
            {
                if (localBasePathOffset > 0)
                {
                    localPath = Helpers.ReadAnsiZ(rawBytes, start + localBasePathOffset, encoding, "LocalBasePath", end);
                }

                if (localBasePathOffsetUnicode > 0)
                {
                    localPath = Helpers.ReadUnicodeZ(rawBytes, start + localBasePathOffsetUnicode, "LocalBasePathUnicode", end);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"LocalBasePath: {ex.Message}");
            }
        }

        if ((locationFlags & LocationFlag.CommonNetworkRelativeLinkAndPathSuffix) ==
            LocationFlag.CommonNetworkRelativeLinkAndPathSuffix)
        {
            try
            {
                var networkSize = Helpers.ReadUInt32(rawBytes, start + networkOffset, "CommonNetworkRelativeLinkSize", end);
                if (networkOffset == 0 || networkSize < 0x14)
                {
                    throw new LnkParseException($"Invalid network share data size ({networkSize}) - is this file corrupt?",
                        (int) (start + networkOffset), "CommonNetworkRelativeLinkSize");
                }

                networkShareInfo = new NetworkShareInfo(
                    Helpers.Slice(rawBytes, start + networkOffset, networkSize, "CommonNetworkRelativeLink", end), codepage);
            }
            catch (Exception ex)
            {
                errors.Add($"CommonNetworkRelativeLink: {ex.Message}");
            }
        }

        try
        {
            if (commonPathSuffixOffset > 0)
            {
                commonPath = Helpers.ReadAnsiZ(rawBytes, start + commonPathSuffixOffset, encoding, "CommonPathSuffix", end);
            }

            if (commonPathSuffixOffsetUnicode > 0)
            {
                commonPath = Helpers.ReadUnicodeZ(rawBytes, start + commonPathSuffixOffsetUnicode, "CommonPathSuffixUnicode", end);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"CommonPathSuffix: {ex.Message}");
        }
    }

    private static ExtraDataBase ParseExtraBlock(byte[] block, int codepage)
    {
        try
        {
            if (block.Length < 8)
            {
                throw new LnkParseException($"Extra data block is only {block.Length} bytes", 0, "ExtraData block");
            }

            var sig = (ExtraDataTypes) BitConverter.ToUInt32(block, 4);

            return sig switch
            {
                ExtraDataTypes.TrackerDataBlock => new TrackerDataBaseBlock(block, codepage),
                ExtraDataTypes.ConsoleDataBlock => new ConsoleDataBlock(block),
                ExtraDataTypes.ConsoleFeDataBlock => new ConsoleFeDataBlock(block),
                ExtraDataTypes.DarwinDataBlock => new DarwinDataBlock(block, codepage),
                ExtraDataTypes.EnvironmentVariableDataBlock => new EnvironmentVariableDataBlock(block, codepage),
                ExtraDataTypes.IconEnvironmentDataBlock => new IconEnvironmentDataBlock(block, codepage),
                ExtraDataTypes.KnownFolderDataBlock => new KnownFolderDataBlock(block),
                ExtraDataTypes.PropertyStoreDataBlock => new PropertyStoreDataBlock(block),
                ExtraDataTypes.ShimDataBlock => new ShimDataBlock(block),
                ExtraDataTypes.SpecialFolderDataBlock => new SpecialFolderDataBlock(block),
                ExtraDataTypes.VistaAndAboveIdListDataBlock => new VistaAndAboveIdListDataBlock(block, codepage),
                _ => throw new LnkParseException(
                    $"Unknown extra data block signature: 0x{(uint) sig:X}. Please send lnk file to saericzimmerman@gmail.com so support can be added",
                    4, "BlockSignature")
            };
        }
        catch (Exception e)
        {
            return new DamagedDataBlock(block, e.Message);
        }
    }

    public List<ShellBag> TargetIDs { get; }
    public List<ExtraDataBase> ExtraBlocks { get; }


    public DateTimeOffset? SourceCreated { get; }
    public DateTimeOffset? SourceModified { get; }
    public DateTimeOffset? SourceAccessed { get; }

    public string CommonPath { get; }
    public string LocalPath { get; }
    public VolumeInfo VolumeInfo { get; }
    public NetworkShareInfo NetworkShareInfo { get; }
    public string SourceFile { get; }

    public byte[] RawBytes { get; }
    public Header Header { get; }

    public string Name { get; }
    public string RelativePath { get; }
    public string WorkingDirectory { get; }
    public string Arguments { get; }
    public string IconLocation { get; }

    public LocationFlag LocationFlags { get; }

    /// <summary>
    ///     Problems found while parsing. The file was still parsed, but the affected structures are null,
    ///     <see cref="ShellBagDamaged" /> or <see cref="DamagedDataBlock" />.
    /// </summary>
    public IReadOnlyList<string> ParseErrors { get; }

    /// <summary>
    ///     True when the ExtraData section ended with a TerminalBlock, as the format requires.
    /// </summary>
    public bool ExtraDataTerminated { get; }

    /// <summary>
    ///     Number of bytes after the end of the LNK structure (after the ExtraData terminal block).
    ///     Legitimate shortcuts have none; a non-zero value can indicate appended content.
    /// </summary>
    public int TrailingDataSize { get; }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Source file: {SourceFile}");
        sb.AppendLine($"Source created: {SourceCreated}");
        sb.AppendLine($"Source modified: {SourceModified}");
        sb.AppendLine($"Source accessed: {SourceAccessed}");
        sb.AppendLine();
        sb.AppendLine("--- Header ---");
        sb.AppendLine($"  File size: {Header.FileSize:N0}");
        sb.AppendLine($"  Flags: {Header.DataFlags}");
        sb.AppendLine($"  File attributes: {Header.FileAttributes}");

        if (Header.HotKey.Length > 0)
        {
            sb.AppendLine($"  Hot key: {Header.HotKey}");
        }

        sb.AppendLine($"  Icon index: {Header.IconIndex}");
        sb.AppendLine(
            $"  Show window: {Header.ShowWindow} ({Helpers.GetDescriptionFromEnumValue(Header.ShowWindow)})");
        sb.AppendLine($"  Target created: {Header.TargetCreationDate}");
        sb.AppendLine($"  Target modified: {Header.TargetModificationDate}");
        sb.AppendLine($"  Target accessed: {Header.TargetLastAccessedDate}");


        if (TargetIDs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- Target ID information ---");
            foreach (var shellBag in TargetIDs)
            {
                sb.Append($">>{shellBag}");
            }
        }

        if ((Header.DataFlags & Header.DataFlag.HasLinkInfo) == Header.DataFlag.HasLinkInfo)
        {
            sb.AppendLine();
            sb.AppendLine("--- Link information ---");
            sb.AppendLine($"Location flags: {LocationFlags}");

            if (VolumeInfo != null)
            {
                sb.AppendLine();
                sb.AppendLine("Volume information");
                sb.AppendLine($"Drive type: {VolumeInfo.DriveType}");
                sb.AppendLine($"Serial number: {VolumeInfo.VolumeSerialNumber}");

                var label = VolumeInfo.VolumeLabel?.Length > 0 ? VolumeInfo.VolumeLabel : "(No label)";

                sb.AppendLine($"Label: {label}");
            }

            if (LocalPath?.Length > 0)
            {
                sb.AppendLine($"Local path: {LocalPath}");
            }

            if (NetworkShareInfo != null)
            {
                sb.AppendLine();
                sb.AppendLine("Network share information");

                if (NetworkShareInfo.DeviceName?.Length > 0)
                {
                    sb.AppendLine($"Device name: {NetworkShareInfo.DeviceName}");
                }

                sb.AppendLine($"Share name: {NetworkShareInfo.NetworkShareName}");

                sb.AppendLine(NetworkShareInfo.HasValidProviderType
                    ? $"Provider type: {NetworkShareInfo.NetworkProviderType}"
                    : "Provider type: (not set)");
                sb.AppendLine($"Share flags: {NetworkShareInfo.ShareFlags}");
            }

            if (CommonPath?.Length > 0)
            {
                sb.AppendLine($"Common path: {CommonPath}");
            }
        }

        if ((Header.DataFlags & Header.DataFlag.HasName) == Header.DataFlag.HasName)
        {
            sb.AppendLine($"Name: {Name}");
        }

        if ((Header.DataFlags & Header.DataFlag.HasRelativePath) == Header.DataFlag.HasRelativePath)
        {
            sb.AppendLine($"Relative Path: {RelativePath}");
        }

        if ((Header.DataFlags & Header.DataFlag.HasWorkingDir) == Header.DataFlag.HasWorkingDir)
        {
            sb.AppendLine($"Working Directory: {WorkingDirectory}");
        }

        if ((Header.DataFlags & Header.DataFlag.HasArguments) == Header.DataFlag.HasArguments)
        {
            sb.AppendLine($"Arguments: {Arguments}");
        }

        if ((Header.DataFlags & Header.DataFlag.HasIconLocation) == Header.DataFlag.HasIconLocation)
        {
            sb.AppendLine($"Icon Location: {IconLocation}");
        }

        if (ExtraBlocks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- Extra blocks information ---");
            foreach (var extraDataBase in ExtraBlocks)
            {
                sb.AppendLine($">>{extraDataBase}");
                sb.AppendLine();
            }
        }


        if (ParseErrors.Count > 0)
        {
            sb.AppendLine("--- Parse errors ---");
            foreach (var error in ParseErrors)
            {
                sb.AppendLine($"  {error}");
            }
        }

        return sb.ToString();
    }
}
