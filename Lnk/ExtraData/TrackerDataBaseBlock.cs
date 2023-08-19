using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lnk.ExtraData;

public class TrackerDataBaseBlock : ExtraDataBase
{
    public TrackerDataBaseBlock(byte[] rawBytes, int codepage=1252)
    {
        Size = BitConverter.ToUInt32(rawBytes, 0);

        Signature = ExtraDataTypes.TrackerDataBlock;
        Version = BitConverter.ToInt32(rawBytes, 8);
        MachineId = CodePagesEncodingProvider.Instance.GetEncoding(codepage).GetString(rawBytes, 16, 16).Split('\0').First();

        var guidRaw0 = new byte[16];
        var guidRaw1 = new byte[16];
        var guidRaw2 = new byte[16];
        var guidRaw3 = new byte[16];
        Buffer.BlockCopy(rawBytes, 0x20, guidRaw0, 0, 16);
        Buffer.BlockCopy(rawBytes, 0x30, guidRaw1, 0, 16);
        Buffer.BlockCopy(rawBytes, 0x40, guidRaw2, 0, 16);
        Buffer.BlockCopy(rawBytes, 0x50, guidRaw3, 0, 16);

        VolumeDroid = new Guid(guidRaw0);
        FileDroid = new Guid(guidRaw1);
        VolumeDroidBirth = new Guid(guidRaw2);
        FileDroidBirth = new Guid(guidRaw3);

        var tempMac = FileDroid.ToString().Split('-').Last();

        MacAddress = Regex.Replace(tempMac, ".{2}", "$0:");
        MacAddress = MacAddress.Substring(0, MacAddress.Length - 1);

        CreationTime = GetDateTimeOffsetFromGuid(FileDroid);
    }

    public DateTimeOffset CreationTime { get; }

    public int Version { get; }
    public string MachineId { get; }
    public Guid VolumeDroid { get; }
    public Guid FileDroid { get; }
    public Guid VolumeDroidBirth { get; }
    public Guid FileDroidBirth { get; }

    public string MacAddress { get; }

    private DateTimeOffset GetDateTimeOffsetFromGuid(Guid guid)
    {
        // offset to move from 1/1/0001, which is 0-time for .NET, to gregorian 0-time of 10/15/1582
        var gregorianCalendarStart = new DateTimeOffset(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);
        const int versionByte = 7;
        const int versionByteMask = 0x0f;
        const int versionByteShift = 4;
        const byte timestampByte = 0;

        var bytes = guid.ToByteArray();

        // reverse the version
        bytes[versionByte] &= versionByteMask;
        bytes[versionByte] |= 0x01 >> versionByteShift;

        var timestampBytes = new byte[8];
        Array.Copy(bytes, timestampByte, timestampBytes, 0, 8);

        var timestamp = BitConverter.ToInt64(timestampBytes, 0);
        var ticks = timestamp + gregorianCalendarStart.Ticks;

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    public override string ToString()
    {
        //return $"{GetType().Name}\r\n{PropertyStore}";
        return
            $"Tracker database block" +
            $"\r\nMachine ID: {MachineId}" +
            $"\r\nMac Address: {MacAddress}" +
            $"\r\nCreation: {CreationTime}" +
            $"\r\nVolume Droid: {VolumeDroid}" +
            $"\r\nVolume Droid Birth: {VolumeDroidBirth}" +
            $"\r\nFile Droid: {FileDroid}" +
            $"\r\nFile Droid birth: {FileDroidBirth}";
    }
}