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

    public LnkFile(byte[] rawBytes, string sourceFile, int codepage = 1252)
    {
        try
        {
            RawBytes = rawBytes;
            SourceFile = Path.GetFullPath(sourceFile);

            var headerBytes = new byte[76];
            Buffer.BlockCopy(rawBytes, 0, headerBytes, 0, 76);

            Header = new Header(headerBytes);

            var fi = new FileInfo(sourceFile);
            SourceCreated = new DateTimeOffset(fi.CreationTimeUtc);
            SourceModified = new DateTimeOffset(fi.LastWriteTimeUtc);
            SourceAccessed = new DateTimeOffset(fi.LastAccessTimeUtc);

            if (SourceCreated.Value.Year == 1601)
            {
                SourceCreated = null;
            }

            if (SourceModified.Value.Year == 1601)
            {
                SourceModified = null;
            }

            if (SourceAccessed.Value.Year == 1601)
            {
                SourceAccessed = null;
            }

            var index = 76;

            TargetIDs = new List<ShellBag>();

            if ((Header.DataFlags & Header.DataFlag.HasTargetIdList) == Header.DataFlag.HasTargetIdList)
            {
                //process shell items
                var shellItemSize = BitConverter.ToInt16(rawBytes, index);
                index += 2;

                var shellItemBytes = new byte[shellItemSize];

                // validate the BlockCopy - validate the source offset + count is less <= source length and validate the dest offset + count is <= dest length
                if (index + shellItemSize > rawBytes.Length || shellItemSize > shellItemBytes.Length)
                {
                    throw new Exception($"Invalid shell item data - is this file corrupt?");
                }

                Buffer.BlockCopy(rawBytes, index, shellItemBytes, 0, shellItemSize);

                var shellItemsRaw = new List<byte[]>();
                var shellItemIndex = 0;

                while (shellItemIndex < shellItemBytes.Length)
                {
                    if (shellItemBytes.Length < shellItemIndex + 2)
                    {
                        throw new Exception($"Invalid shell item data - is this file corrupt?");
                    }

                    var shellSize = BitConverter.ToUInt16(shellItemBytes, shellItemIndex);

                    if (shellSize <= 0)
                    {
                        break;
                    }

                    var itemBytes = new byte[shellSize];
                    // validate the BlockCopy - validate the source offset + count is less <= source length and validate the dest offset + count is <= dest length
                    if (shellItemIndex + shellSize > shellItemBytes.Length || shellSize > itemBytes.Length)
                    {
                        throw new Exception($"Invalid shell item data - is this file corrupt?");
                    }

                    Buffer.BlockCopy(shellItemBytes, shellItemIndex, itemBytes, 0, shellSize);

                    shellItemsRaw.Add(itemBytes);
                    shellItemIndex += shellSize;
                }

                //TODO try catch and add placeholder for shellitem when exeption happens? or ?
                foreach (var shellItem in shellItemsRaw)
                {
                    if (shellItem.Length >= 0x28)
                    {
                        var sig1 = BitConverter.ToInt64(shellItem, 0x8);
                        var sig2 = BitConverter.ToInt64(shellItem, 0x18);

                        if (sig1 == 0 && sig2 == 0
                           ) // if ((sig1 == zip1_0 && sig2 == zip2_0) || sig2 == zip2_1 || (sig1 == zip1_1 && sig2 == zip2_0))
                        {
                            //double check
                            if (shellItem[0x28] == 0x2f || shellItem[0x26] == 0x2f || shellItem[0x1a] == 0x2f ||
                                shellItem[0x1c] == 0x2f)
                            // forward slash in date or N / A
                            {
                                //zip?
                                var zz = new ShellBagZipContents(shellItem);

                                TargetIDs.Add(zz);
                                continue;
                            }
                        }
                    }

                    switch (shellItem[2])
                    {
                        case 0x1f:
                            var f = new ShellBag0X1F(shellItem, codepage);
                            TargetIDs.Add(f);
                            break;
                        case 0x22:
                        case 0x23:
                            var two3 = new ShellBag0X23(shellItem, codepage);
                            TargetIDs.Add(two3);
                            break;
                        case 0x2a:
                        case 0x2f:
                            var ff = new ShellBag0X2F(shellItem, codepage);
                            TargetIDs.Add(ff);
                            break;
                        case 0x2e:
                                var ee = new ShellBag0X2E(shellItem);
                                TargetIDs.Add(ee);
                                break;
                        case 0xb1:
                        case 0x31:
                        case 0x3A:
                        case 0x35:
                        case 0x39:
                            var d = new ShellBag0X31(shellItem, codepage);
                            TargetIDs.Add(d);
                            break;
                        case 0x32:
                        case 0x36:
                            var d2 = new ShellBag0X32(shellItem, codepage);
                            TargetIDs.Add(d2);
                            break;
                        case 0x00:
                            var v0 = new ShellBag0X00(shellItem, codepage);
                            TargetIDs.Add(v0);
                            break;
                        case 0x01:
                            var one = new ShellBag0X01(shellItem);
                            TargetIDs.Add(one);
                            break;
                        case 0x71:
                            var sevenone = new ShellBag0X71(shellItem);
                            TargetIDs.Add(sevenone);
                            break;
                        case 0x61:
                            var sixone = new ShellBag0X61(shellItem, codepage);
                            TargetIDs.Add(sixone);
                            break;

                        case 0xC3:
                            var c3 = new ShellBag0Xc3(shellItem, codepage);
                            TargetIDs.Add(c3);
                            break;

                        case 0x74:
                        case 0x77:
                            var sev = new ShellBag0X74(shellItem, codepage);
                            TargetIDs.Add(sev);
                            break;

                        case 0xae:
                        case 0xaa:
                        case 0x79:
                            var ae = new ShellBagZipContents(shellItem);
                            TargetIDs.Add(ae);
                            break;

                        case 0x41:
                        case 0x42:
                        case 0x43:
                        case 0x46:
                        case 0x47:
                            var forty = new ShellBag0X40(shellItem, codepage);
                            TargetIDs.Add(forty);
                            break;
                        case 0x4C:
                            var fc = new ShellBag0X4C(shellItem);
                            TargetIDs.Add(fc);
                            break;
                        default:
                            throw new Exception(
                                $"Unknown shell item ID: 0x{shellItem[2]:X}. Please send to saericzimmerman@gmail.com so support can be added.");
                    }
                }

                //TODO tie back extra block for SpecialFolderDataBlock and KnownFolderDataBlock??

                index += shellItemSize;
            }

            if ((Header.DataFlags & Header.DataFlag.HasLinkInfo) == Header.DataFlag.HasLinkInfo)
            {
                var locationItemSize = BitConverter.ToInt32(rawBytes, index);

                if (locationItemSize <= 0)
                {
                    throw new Exception($"Invalid location item data size ({locationItemSize}) - is this file corrupt?");

                }

                var locationBytes = new byte[locationItemSize];

                // validate the BlockCopy - validate the source offset + count is less <= source length and validate the dest offset + count is <= dest length
                if (index + locationItemSize > rawBytes.Length || locationItemSize > locationBytes.Length)
                {
                    throw new Exception($"Invalid location item data - is this file corrupt?");
                }

                Buffer.BlockCopy(rawBytes, index, locationBytes, 0, locationItemSize);

                if (locationBytes.Length > 20)
                {
                    var locationInfoHeaderSize = BitConverter.ToInt32(locationBytes, 4);

                    LocationFlags = (LocationFlag)BitConverter.ToInt32(locationBytes, 8);

                    var volOffset = BitConverter.ToInt32(locationBytes, 12);
                    if (volOffset < 0)
                    {
                        throw new Exception($"Invalid volume offset ({volOffset}) - is this file corrupt?");
                    }

                    var vbyteSize = BitConverter.ToInt32(locationBytes, volOffset);
                    if (vbyteSize <= 0)
                    {
                        throw new Exception($"Invalid volume data size ({vbyteSize}) - is this file corrupt?");
                    }

                    var volBytes = new byte[vbyteSize];

                    // validate the BlockCopy - validate the source offset + count is less <= source length and validate the dest offset + count is <= dest length
                    if (volOffset + vbyteSize > locationBytes.Length || vbyteSize > volBytes.Length)
                    {
                        throw new Exception($"Invalid volume data - is this file corrupt?");
                    }

                    Buffer.BlockCopy(locationBytes, volOffset, volBytes, 0, vbyteSize);

                    if (volOffset > 0)
                    {
                        VolumeInfo = new VolumeInfo(volBytes, codepage);
                    }

                    var localPathOffset = BitConverter.ToInt32(locationBytes, 16);
                    var networkShareOffset = BitConverter.ToInt32(locationBytes, 20);
                    if (networkShareOffset < 0)
                    {
                        throw new Exception($"Invalid network share offset ({networkShareOffset}) - is this file corrupt?");
                    }

                    if ((LocationFlags & LocationFlag.VolumeIdAndLocalBasePath) ==
                        LocationFlag.VolumeIdAndLocalBasePath)
                    {
                        LocalPath = CodePagesEncodingProvider.Instance.GetEncoding(codepage)
                            .GetString(locationBytes, localPathOffset, locationBytes.Length - localPathOffset)
                            .Split('\0')
                            .First();
                    }
                    if ((LocationFlags & LocationFlag.CommonNetworkRelativeLinkAndPathSuffix) ==
                        LocationFlag.CommonNetworkRelativeLinkAndPathSuffix)
                    {
                        var networkShareSize = BitConverter.ToInt32(locationBytes, networkShareOffset);
                        if (networkShareSize <= 0)
                        {
                            throw new Exception($"Invalid network share data size ({networkShareSize}) - is this file corrupt?");
                        }

                        var networkBytes = new byte[networkShareSize];
                        // validate the BlockCopy - validate the source offset + count is less <= source length and validate the dest offset + count is <= dest length
                        if (networkShareOffset + networkShareSize > locationBytes.Length || networkShareSize > networkBytes.Length)
                        {
                            throw new Exception($"Invalid network share data - is this file corrupt?");
                        }

                        Buffer.BlockCopy(locationBytes, networkShareOffset, networkBytes, 0, networkShareSize);

                        NetworkShareInfo = new NetworkShareInfo(networkBytes, codepage);
                    }

                    if (locationBytes.Length < 28)
                    {
                        throw new Exception($"Unable to calculate common path offset - the locationBytes array is too small - is this file corrupt?");
                    }

                    var commonPathOffset = BitConverter.ToInt32(locationBytes, 24);
                    if (commonPathOffset < 0)
                    {
                        throw new Exception($"Invalid common path offset ({commonPathOffset}) - is this file corrupt?");
                    }

                    CommonPath = CodePagesEncodingProvider.Instance.GetEncoding(codepage)
                        .GetString(locationBytes, commonPathOffset, locationBytes.Length - commonPathOffset)
                        .Split('\0')
                        .First();

                    if (locationInfoHeaderSize > 28)
                    {
                        var uniLocalOffset = BitConverter.ToInt32(locationBytes, 28);

                        var unicodeLocalPath = Encoding.Unicode
                            .GetString(locationBytes, uniLocalOffset, locationBytes.Length - uniLocalOffset)
                            .Split('\0')
                            .First();
                        LocalPath = unicodeLocalPath;
                    }

                    if (locationInfoHeaderSize > 32)
                    {
                        var uniCommonOffset = BitConverter.ToInt32(locationBytes, 32);

                        var unicodeCommonPath = Encoding.Unicode
                            .GetString(locationBytes, uniCommonOffset, locationBytes.Length - uniCommonOffset)
                            .Split('\0')
                            .First();
                        CommonPath = unicodeCommonPath;
                    }
                }


                index += locationItemSize;
            }

            if ((Header.DataFlags & Header.DataFlag.HasName) == Header.DataFlag.HasName)
            {
                var nameLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    Name = Encoding.Unicode.GetString(rawBytes, index, nameLen * 2);
                    index += nameLen;
                }
                else
                {
                    Name = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, nameLen);
                }
                index += nameLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasRelativePath) == Header.DataFlag.HasRelativePath)
            {
                var relLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    RelativePath = Encoding.Unicode.GetString(rawBytes, index, relLen * 2);
                    index += relLen;
                }
                else
                {
                    RelativePath = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, relLen);
                }
                index += relLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasWorkingDir) == Header.DataFlag.HasWorkingDir)
            {
                var workLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    WorkingDirectory = Encoding.Unicode.GetString(rawBytes, index, workLen * 2);
                    index += workLen;
                }
                else
                {
                    WorkingDirectory = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, workLen);
                }
                index += workLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasArguments) == Header.DataFlag.HasArguments)
            {
                var argLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    Arguments = Encoding.Unicode.GetString(rawBytes, index, argLen * 2);
                    index += argLen;
                }
                else
                {
                    Arguments = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, argLen);
                }
                index += argLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasIconLocation) == Header.DataFlag.HasIconLocation)
            {
                var icoLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    IconLocation = Encoding.Unicode.GetString(rawBytes, index, icoLen * 2);
                    index += icoLen;
                }
                else
                {
                    IconLocation = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, index, icoLen);
                }
                index += icoLen;
            }


            var extraByteBlocks = new List<byte[]>();
            //extra blocks
            while (index < rawBytes.Length)
            {
                var extraSize = BitConverter.ToInt32(rawBytes, index);
                if (extraSize <= 0 || (uint)extraSize >= rawBytes.Length)
                {
                    break;
                }

                if (extraSize > rawBytes.Length - index)
                {
                    extraSize = rawBytes.Length - index;
                }

                var extraBytes = new byte[extraSize];

                // validate the BlockCopy - validate the source offset + count is less <= source length and validate the dest offset + count is <= dest length
                if (index + extraSize > rawBytes.Length || extraSize > extraBytes.Length)
                {
                    throw new Exception($"Invalid extra block data - is this file corrupt?");
                }

                Buffer.BlockCopy(rawBytes, index, extraBytes, 0, extraSize);

                extraByteBlocks.Add(extraBytes);

                index += extraSize;
            }

            ExtraBlocks = new List<ExtraDataBase>();

            foreach (var extraBlock in extraByteBlocks)
            {
                try
                {
                    var sig = (ExtraDataTypes)BitConverter.ToInt32(extraBlock, 4);

                    switch (sig)
                    {
                        case ExtraDataTypes.TrackerDataBlock:
                            var tb = new TrackerDataBaseBlock(extraBlock, codepage);
                            ExtraBlocks.Add(tb);
                            break;
                        case ExtraDataTypes.ConsoleDataBlock:
                            var cdb = new ConsoleDataBlock(extraBlock);
                            ExtraBlocks.Add(cdb);
                            break;
                        case ExtraDataTypes.ConsoleFeDataBlock:
                            var cfeb = new ConsoleFeDataBlock(extraBlock);
                            ExtraBlocks.Add(cfeb);
                            break;
                        case ExtraDataTypes.DarwinDataBlock:
                            var db = new DarwinDataBlock(extraBlock, codepage);
                            ExtraBlocks.Add(db);
                            break;
                        case ExtraDataTypes.EnvironmentVariableDataBlock:
                            var eb = new EnvironmentVariableDataBlock(extraBlock, codepage);
                            ExtraBlocks.Add(eb);
                            break;
                        case ExtraDataTypes.IconEnvironmentDataBlock:
                            var ib = new IconEnvironmentDataBlock(extraBlock, codepage);
                            ExtraBlocks.Add(ib);
                            break;
                        case ExtraDataTypes.KnownFolderDataBlock:
                            var kf = new KnownFolderDataBlock(extraBlock);
                            ExtraBlocks.Add(kf);
                            break;
                        case ExtraDataTypes.PropertyStoreDataBlock:
                            var ps = new PropertyStoreDataBlock(extraBlock);

                            ExtraBlocks.Add(ps);
                            break;
                        case ExtraDataTypes.ShimDataBlock:
                            var sd = new KnownFolderDataBlock(extraBlock);
                            ExtraBlocks.Add(sd);
                            break;
                        case ExtraDataTypes.SpecialFolderDataBlock:
                            var sf = new SpecialFolderDataBlock(extraBlock);
                            ExtraBlocks.Add(sf);
                            break;
                        case ExtraDataTypes.VistaAndAboveIdListDataBlock:
                            var vid = new VistaAndAboveIdListDataBlock(extraBlock, codepage);
                            ExtraBlocks.Add(vid);
                            break;
                        default:
                            throw new Exception(
                                $"Unknown extra data block signature: 0x{sig:X}. Please send lnk file to saericzimmerman@gmail.com so support can be added");
                    }
                }
                catch (Exception e)
                {
                    var dmg = new DamagedDataBlock(extraBlock, e.Message);
                    ExtraBlocks.Add(dmg);

                }

            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error processing file ({sourceFile}) - the error was ({ex.Message})", ex);
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
        sb.AppendLine($"  Target modified: {Header.TargetLastAccessedDate}");
        sb.AppendLine($"  Target accessed: {Header.TargetModificationDate}");


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

                var label = VolumeInfo.VolumeLabel.Length > 0 ? VolumeInfo.VolumeLabel : "(No label)";

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

                if (NetworkShareInfo.DeviceName.Length > 0)
                {
                    sb.AppendLine($"Device name: {NetworkShareInfo.DeviceName}");
                }

                sb.AppendLine($"Share name: {NetworkShareInfo.NetworkShareName}");

                sb.AppendLine($"Provider type: {NetworkShareInfo.NetworkProviderType}");
                sb.AppendLine($"Share flags: {NetworkShareInfo.ShareFlags}");
            }

            if (CommonPath.Length > 0)
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


        return sb.ToString();
    }
}
