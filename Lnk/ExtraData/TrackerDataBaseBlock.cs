using System;
using System.Text;
using System.Linq;

namespace Lnk.ExtraData;

public class TrackerDataBaseBlock : ExtraDataBase
{
    public TrackerDataBaseBlock(byte[] rawBytes, int codepage=1252)
    {
        RequireSize(rawBytes, 0x60, "TrackerDataBlock");

        Size = BitConverter.ToUInt32(rawBytes, 0);

        Signature = ExtraDataTypes.TrackerDataBlock;

        // Length (0x58) at offset 8, Version (0) at offset 12, MachineID (16 bytes, NUL terminated) at offset 16
        Length = BitConverter.ToUInt32(rawBytes, 8);
        Version = BitConverter.ToInt32(rawBytes, 12);
        MachineId = Helpers.ReadAnsiZ(rawBytes, 16, Helpers.GetEncoding(codepage), "MachineID", 16 + 16);

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

        // For version 1 UUIDs the node field (last 6 bytes) is the MAC address of the machine that created it
        var node = FileDroid.ToByteArray();
        MacAddress = string.Join(":", node.Skip(10).Select(b => b.ToString("x2")));

        CreationTime = GetDateTimeOffsetFromGuid(FileDroid);
    }

    public DateTimeOffset CreationTime { get; }

    /// <summary>
    ///     Size of the tracker data that follows the block header (MUST be 0x58)
    /// </summary>
    public uint Length { get; }

    /// <summary>
    ///     Tracker data version (MUST be 0)
    /// </summary>
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