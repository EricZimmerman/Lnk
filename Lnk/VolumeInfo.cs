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
            [Description("Unknown")] DriveUnknown = 0,

            [Description("No root directory")] DriveNoRootDir = 1,

            [Description("Removable storage media (Floppy, USB)")] DriveRemovable = 2,

            [Description("Fixed storage media (Hard drive)")] DriveFixed = 3,

            [Description("Remote storage")] DriveRemote = 4,

            [Description("Optical disc (CD-ROM, DVD, BD)")] DriveCdrom = 5,

            [Description("RAM drive")] DriveRamdisk = 6
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