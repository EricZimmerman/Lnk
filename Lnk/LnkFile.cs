using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Lnk.ExtraData;
using Lnk.ShellItems;

namespace Lnk
{
    public class LnkFile
    {
        public List<ShellBag> TargetIDs { get; }
        public List<ExtraDataBase> ExtraBlocks { get; }

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
            sb.AppendLine($"  Show window: {Header.ShowWindow} ({Helpers.GetDescriptionFromEnumValue(Header.ShowWindow)})");
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
                    sb.AppendLine($"Serial number: {VolumeInfo.DriveSerialNumber}");

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

        [Flags]
        public enum LocationFlag
        {
            [Description("The linked file is on a volume")] VolumeIDAndLocalBasePath = 0x0001,

            [Description("The linked file is on a network share")] CommonNetworkRelativeLinkAndPathSuffix = 0x0002
        }

        //TODO Include this at all?
        //public string FullName;

            public DateTimeOffset SourceCreated { get; }
            public DateTimeOffset SourceModified { get; }
            public DateTimeOffset SourceAccessed { get; }

        public LnkFile(byte[] rawBytes, string sourceFile)
        {
            SourceFile = Path.GetFullPath(sourceFile);
            var headerBytes = new byte[76];
            Buffer.BlockCopy(rawBytes, 0, headerBytes, 0, 76);

            Header = new Header(headerBytes);


            var fi = new FileInfo(sourceFile);
            SourceCreated = new DateTimeOffset(fi.CreationTimeUtc);
            SourceModified = new DateTimeOffset(fi.LastWriteTimeUtc);
            SourceAccessed = new DateTimeOffset(fi.LastAccessTimeUtc);

            var index = 76;

            TargetIDs = new List<ShellBag>();

            if ((Header.DataFlags & Header.DataFlag.HasTargetIDList) == Header.DataFlag.HasTargetIDList)
            {
                //process shell items
                var shellItemSize = BitConverter.ToInt16(rawBytes, index);
                index += 2;

                var shellItemBytes = new byte[shellItemSize];
                Buffer.BlockCopy(rawBytes, index, shellItemBytes, 0, shellItemSize);

                var shellItemsRaw = new List<byte[]>();
                var shellItemIndex = 0;

                while (shellItemIndex < shellItemBytes.Length)
                {
                    var shellSize = BitConverter.ToUInt16(shellItemBytes, shellItemIndex);

                    if (shellSize == 0)
                    {
                        break;
                    }
                    var itemBytes = new byte[shellSize];
                    Buffer.BlockCopy(shellItemBytes, shellItemIndex, itemBytes, 0, shellSize);

                    shellItemsRaw.Add(itemBytes);
                    shellItemIndex += shellSize;
                }

                foreach (var shellItem in shellItemsRaw)
                {
                    switch (shellItem[2])
                    {
                        case 0x1f:
                            var f = new ShellBag0x1f(-1, -1, shellItem, "");
                            TargetIDs.Add(f);
                            break;

                        case 0x2f:
                            var ff = new ShellBag0X2F(-1, -1, shellItem, "");
                            TargetIDs.Add(ff);
                            break;
                        case 0x2e:
                            var ee = new ShellBag0x2e(-1, -1, shellItem, "");
                            TargetIDs.Add(ee);
                            break;
                        case 0xb1:
                        case 0x31:
                        case 0x35:
                            var d = new ShellBag0X31(-1, -1, shellItem, "");
                            TargetIDs.Add(d);
                            break;
                        case 0x32:
                            var d2 = new ShellBag0X32(-1, -1, shellItem, "");
                            TargetIDs.Add(d2);
                            break;
                        case 0x00:
                            var v0 = new ShellBag0x00(-1, -1, shellItem, "");
                            TargetIDs.Add(v0);
                            break;
                        case 0x01:
                            var one = new ShellBag0X01(-1, -1, shellItem, "");
                            TargetIDs.Add(one);
                            break;
                        case 0x71:
                            var sevenone = new ShellBag0x71(-1, -1, shellItem, "");
                            TargetIDs.Add(sevenone);
                            break;
                        case 0x61:
                            var sixone = new ShellBag0X61(-1, -1, shellItem, "");
                            TargetIDs.Add(sixone);
                            break;

                        case 0xC3:
                            var c3 = new ShellBag0Xc3(-1, -1, shellItem, "");
                            TargetIDs.Add(c3);
                            break;

                        case 0x74:
                        case 0x77:
                            var sev = new ShellBag0x74(-1, -1, shellItem, "");
                            TargetIDs.Add(sev);
                            break;

                        case 0x41:
                        case 0x42:
                        case 0x43:
                        case 0x46:
                        case 0x47:
                            var forty = new ShellBag0x40(-1, -1, shellItem, "");
                            TargetIDs.Add(forty);
                            break;
                        default:
                            throw new Exception($"Unknown item ID: 0x{shellItem[2]:X}");
                    }
                }

                //TODO tie back extra block for SpecialFolderDataBlock and KnownFolderDataBlock

                index += shellItemSize;
            }

            if ((Header.DataFlags & Header.DataFlag.HasLinkInfo) == Header.DataFlag.HasLinkInfo)
            {
                var locationItemSize = BitConverter.ToInt32(rawBytes, index);
                var locationBytes = new byte[locationItemSize];
                Buffer.BlockCopy(rawBytes, index, locationBytes, 0, locationItemSize);

                var locationInfoHeaderSize = BitConverter.ToInt32(locationBytes, 4);

                LocationFlags = (LocationFlag) BitConverter.ToInt32(locationBytes, 8);

                var volOffset = BitConverter.ToInt32(locationBytes, 12);
                var vbyteSize = BitConverter.ToInt32(locationBytes, volOffset);
                var volBytes = new byte[vbyteSize];
                Buffer.BlockCopy(locationBytes, volOffset, volBytes, 0, vbyteSize);

                if (volOffset > 0)
                {
                    VolumeInfo = new VolumeInfo(volBytes);
                }

                var localPathOffset = BitConverter.ToInt32(locationBytes, 16);
                var networkShareOffset = BitConverter.ToInt32(locationBytes, 20);

                if ((LocationFlags & LocationFlag.VolumeIDAndLocalBasePath) == LocationFlag.VolumeIDAndLocalBasePath)
                {
                    LocalPath = Encoding.GetEncoding(1252)
                        .GetString(locationBytes, localPathOffset, locationBytes.Length - localPathOffset)
                        .Split('\0')
                        .First();
                }
                if ((LocationFlags & LocationFlag.CommonNetworkRelativeLinkAndPathSuffix) ==
                    LocationFlag.CommonNetworkRelativeLinkAndPathSuffix)
                {
                    var networkShareSize = BitConverter.ToInt32(locationBytes, networkShareOffset);
                    var networkBytes = new byte[networkShareSize];
                    Buffer.BlockCopy(locationBytes, networkShareOffset, networkBytes, 0, networkShareSize);

                    NetworkShareInfo = new NetworkShareInfo(networkBytes);
                }

                var commonPathOffset = BitConverter.ToInt32(locationBytes, 24);

                CommonPath = Encoding.GetEncoding(1252)
                    .GetString(locationBytes, commonPathOffset, locationBytes.Length - commonPathOffset)
                    .Split('\0')
                    .First();

                if (locationInfoHeaderSize > 28)
                {
                    var uniLocalOffset = BitConverter.ToInt32(locationBytes, 28);
                    throw new Exception(
                        $"Unsupported data found. Email lnk file '{sourceFile}' to saericzimmerman@gmail.com");
                    //TODO var unicodeLocalPath = Encoding.Unicode.GetString(locationBytes, uniLocalOffset,5);
                }

                if (locationInfoHeaderSize > 32)
                {
                    var uniCommonOffset = BitConverter.ToInt32(locationBytes, 32);
                    throw new Exception(
                        $"Unsupported data found. Email lnk file '{sourceFile}' to saericzimmerman@gmail.com");
                    //TODO var unicodeCommonPath = Encoding.Unicode.GetString(locationBytes, uniCommonOffset, 5);
                }

                //                if (LocationFlags == LocationFlag.VolumeIDAndLocalBasePath)
//                {
//                    FullName = Path.Combine(CommonPath, LocalPath);
//                }
//                else
//                {
//                    FullName = Path.Combine(CommonPath, NetworkShareInfo.NetworkShareName);
//                }

                index += locationItemSize;
            }

            if ((Header.DataFlags & Header.DataFlag.HasName) == Header.DataFlag.HasName)
            {
                var nameLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    Name = Encoding.Unicode.GetString(rawBytes, index, nameLen*2);
                    index += nameLen;
                }
                else
                {
                    Name = Encoding.GetEncoding(1252).GetString(rawBytes, index, nameLen);
                }
                index += nameLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasRelativePath) == Header.DataFlag.HasRelativePath)
            {
                var relLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    RelativePath = Encoding.Unicode.GetString(rawBytes, index, relLen*2);
                    index += relLen;
                }
                else
                {
                    RelativePath = Encoding.GetEncoding(1252).GetString(rawBytes, index, relLen);
                }
                index += relLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasWorkingDir) == Header.DataFlag.HasWorkingDir)
            {
                var workLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    WorkingDirectory = Encoding.Unicode.GetString(rawBytes, index, workLen*2);
                    index += workLen;
                }
                else
                {
                    WorkingDirectory = Encoding.GetEncoding(1252).GetString(rawBytes, index, workLen);
                }
                index += workLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasArguments) == Header.DataFlag.HasArguments)
            {
                var argLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    Arguments = Encoding.Unicode.GetString(rawBytes, index, argLen*2);
                    index += argLen;
                }
                else
                {
                    Arguments = Encoding.GetEncoding(1252).GetString(rawBytes, index, argLen);
                }
                index += argLen;
            }

            if ((Header.DataFlags & Header.DataFlag.HasIconLocation) == Header.DataFlag.HasIconLocation)
            {
                var icoLen = BitConverter.ToInt16(rawBytes, index);
                index += 2;
                if ((Header.DataFlags & Header.DataFlag.IsUnicode) == Header.DataFlag.IsUnicode)
                {
                    IconLocation = Encoding.Unicode.GetString(rawBytes, index, icoLen*2);
                    index += icoLen;
                }
                else
                {
                    IconLocation = Encoding.GetEncoding(1252).GetString(rawBytes, index, icoLen);
                }
                index += icoLen;
            }


            var extraByteBlocks = new List<byte[]>();
            //extra blocks
            while (index < rawBytes.Length)
            {
                var extraSize = BitConverter.ToInt32(rawBytes, index);
                if (extraSize == 0)
                {
                    break;
                }

                var extraBytes = new byte[extraSize];
                Buffer.BlockCopy(rawBytes, index, extraBytes, 0, extraSize);

                extraByteBlocks.Add(extraBytes);

                index += extraSize;
            }

            ExtraBlocks = new List<ExtraDataBase>();

            foreach (var extraBlock in extraByteBlocks)
            {
                var sig = (ExtraDataTypes) BitConverter.ToInt32(extraBlock, 4);

                switch (sig)
                {
                    case ExtraDataTypes.TrackerDataBlock:
                        var tb = new TrackerDataBaseBlock(extraBlock);
                        ExtraBlocks.Add(tb);
                        break;
                    case ExtraDataTypes.ConsoleDataBlock:
                        var cdb = new ConsoleDataBlock(extraBlock);
                        ExtraBlocks.Add(cdb);
                        break;
                    case ExtraDataTypes.ConsoleFEDataBlock:
                        var cfeb = new ConsoleFEDataBlock(extraBlock);
                        ExtraBlocks.Add(cfeb);
                        break;
                    case ExtraDataTypes.DarwinDataBlock:
                        var db = new DarwinDataBlock(extraBlock);
                        ExtraBlocks.Add(db);
                        break;
                    case ExtraDataTypes.EnvironmentVariableDataBlock:
                        var eb = new EnvironmentVariableDataBlock(extraBlock);
                        ExtraBlocks.Add(eb);
                        break;
                    case ExtraDataTypes.IconEnvironmentDataBlock:
                        var ib = new EnvironmentVariableDataBlock(extraBlock);
                        ExtraBlocks.Add(ib);
                        break;
                    case ExtraDataTypes.KnownFolderDataBlock:
                        var kf = new KnownFolderDataBlock(extraBlock);
                        ExtraBlocks.Add(kf);
                        break;
                    case ExtraDataTypes.PropertyStoreDataBlock:
                        var ps = new PropertyStoreDataBlock(extraBlock);

//                        Debug.WriteLine(
//                            $"File: {Path.GetFileName(sourceFile)} Extra blocks start at: 0x{index:X} Flags: {Header.DataFlags}");
//                        Debug.WriteLine(ps);

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
                    case ExtraDataTypes.VistaAndAboveIDListDataBlock:
                        var vid = new VistaAndAboveIDListDataBlock(extraBlock);
                        ExtraBlocks.Add(vid);
                        break;
                    default:
                        throw new Exception(
                            $"Unknown extra data block signature: 0x{sig:X}. Please send lnk file to saericzimmerman@gmail.com so support can be added");
                }
            }
        }

        public string CommonPath { get; }
        public string LocalPath { get; }
        public VolumeInfo VolumeInfo { get; }
        public NetworkShareInfo NetworkShareInfo { get; }
        public string SourceFile { get; }
        public Header Header { get; }

        public string Name { get; }
        public string RelativePath { get; }
        public string WorkingDirectory { get; }
        public string Arguments { get; }
        public string IconLocation { get; }

        public LocationFlag LocationFlags { get; }
    }
}