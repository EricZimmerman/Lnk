using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Lnk
{
    public class VolumeInfo
    {
        public enum DriveTypes
        {
            [Description("Unknown")] DRIVE_UNKNOWN = 0,

            [Description("No root directory")] DRIVE_NO_ROOT_DIR = 1,

            [Description("Removable storage media (floppy, usb)")] DRIVE_REMOVABLE = 2,

            [Description("Fixed storage media (harddisk)")] DRIVE_FIXED = 3,

            [Description("Remote storage")] DRIVE_REMOTE = 4,

            [Description("Optical disc (CD-ROM, DVD, BD)")] DRIVE_CDROM = 5,

            [Description("RAM drive")] DRIVE_RAMDISK = 6
        }

        public VolumeInfo(byte[] rawBytes)
        {
            Size = BitConverter.ToInt32(rawBytes, 0);
            DriveType = (DriveTypes) BitConverter.ToInt32(rawBytes, 4);
            DriveSerialNumber = BitConverter.ToInt32(rawBytes, 8).ToString("X");
            DriveLabelOffset = BitConverter.ToInt32(rawBytes, 12);

            if (DriveLabelOffset > 16)
            {
                VolumeLabel = Encoding.Unicode
                    .GetString(rawBytes, DriveLabelOffset, (rawBytes.Length - DriveLabelOffset)*2);
            }
            else
            {
                VolumeLabel = Encoding.GetEncoding(1252)
                    .GetString(rawBytes, DriveLabelOffset, rawBytes.Length - DriveLabelOffset).Split('\0').First();
            }
        }

        public DriveTypes DriveType { get; }
        public int Size { get; }

        public string DriveSerialNumber { get; }
        public int DriveLabelOffset { get; }

        public string VolumeLabel { get; }

        public override string ToString()
        {
            return $"Drive type: {DriveType} Size: {Size}, Serial: {DriveSerialNumber}, Vol label: {VolumeLabel}";
        }
    }
}