using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

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

    public VolumeInfo(byte[] rawBytes, int codepage=1252)
    {
        Size = BitConverter.ToInt32(rawBytes, 0);
        DriveType = (DriveTypes) BitConverter.ToInt32(rawBytes, 4);
        VolumeSerialNumber = BitConverter.ToInt32(rawBytes, 8).ToString("X8");
        VolumeLabelOffset = BitConverter.ToInt32(rawBytes, 12);

        if (VolumeLabelOffset > 16)
        {
            try
            {
                VolumeLabel = Encoding.Unicode
                    .GetString(rawBytes, VolumeLabelOffset, (rawBytes.Length - VolumeLabelOffset) * 2);
            }
            catch (Exception)
            {
                VolumeLabel = CodePagesEncodingProvider.Instance.GetEncoding(codepage)
                    ?.GetString(rawBytes, VolumeLabelOffset, rawBytes.Length - VolumeLabelOffset)
                    .Split('\0')
                    .First();
            }
        }
        else
        {
            VolumeLabel = CodePagesEncodingProvider.Instance.GetEncoding(codepage)
                ?.GetString(rawBytes, VolumeLabelOffset, rawBytes.Length - VolumeLabelOffset)
                .Split('\0')
                .First();
        }
    }

    public DriveTypes DriveType { get; }
    public int Size { get; }

    public string VolumeSerialNumber { get; }
    public int VolumeLabelOffset { get; }

    public string VolumeLabel { get; }

    public override string ToString()
    {
        return $"Drive type: {DriveType} Size: {Size}, Serial: {VolumeSerialNumber}, Vol label: {VolumeLabel}";
    }
}