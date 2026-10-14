using System;
using System.ComponentModel;

namespace Lnk;

public class VolumeInfo
{
    public enum DriveTypes
    {
        [Description("Unknown")] DriveUnknown = 0,

        [Description("No root directory")] DriveNoRootDir = 1,

        [Description("Removable storage media (Floppy, USB)")] DriveRemovable = 2,

        [Description("Fixed storage media (Hard drive)")] DriveFixed = 3,

        [Description("Remote storage")] DriveRemote = 4,

        [Description("Optical disc (CD-ROM, DVD, BD)")] DriveCdrom = 5,

        [Description("RAM drive")] DriveRamdisk = 6
    }

    /// <summary>
    ///     Parses a VolumeID structure (MS-SHLLINK 2.3.1). <paramref name="rawBytes" /> holds exactly VolumeIDSize bytes.
    /// </summary>
    public VolumeInfo(byte[] rawBytes, int codepage = 1252)
    {
        const string field = "VolumeID";

        // VolumeIDSize MUST be greater than 0x10
        Helpers.EnsureRange(rawBytes, 0, 0x10, field);

        Size = BitConverter.ToInt32(rawBytes, 0);
        DriveType = (DriveTypes) BitConverter.ToInt32(rawBytes, 4);
        VolumeSerialNumber = BitConverter.ToUInt32(rawBytes, 8).ToString("X8");
        VolumeLabelOffset = BitConverter.ToInt32(rawBytes, 12);

        if (VolumeLabelOffset == 0x14)
        {
            // The ANSI label offset MUST be ignored; VolumeLabelOffsetUnicode follows at 0x10.
            VolumeLabelOffsetUnicode = Helpers.ReadInt32(rawBytes, 0x10, field);
            VolumeLabel = Helpers.ReadUnicodeZ(rawBytes, VolumeLabelOffsetUnicode, field);
        }
        else
        {
            VolumeLabel = Helpers.ReadAnsiZ(rawBytes, VolumeLabelOffset, Helpers.GetEncoding(codepage), field);
        }
    }

    public DriveTypes DriveType { get; }
    public int Size { get; }

    public string VolumeSerialNumber { get; }
    public int VolumeLabelOffset { get; }

    /// <summary>
    ///     Offset of the Unicode volume label, or 0 when the label is stored in the system code page.
    /// </summary>
    public int VolumeLabelOffsetUnicode { get; }

    public string VolumeLabel { get; }

    public override string ToString()
    {
        return $"Drive type: {DriveType} Size: {Size}, Serial: {VolumeSerialNumber}, Vol label: {VolumeLabel}";
    }
}