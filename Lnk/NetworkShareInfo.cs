using System;
using System.ComponentModel;

namespace Lnk;

public class NetworkShareInfo
{
    //TODO add descriptions to these

    public enum ProviderType
    {
        WnncNet10Net = 0x50000,
        WnncNet_3In1 = 0x270000,
        WnncNet_9Tiles = 0x90000,
        WnncNetAppletalk = 0x130000,
        WnncNetAs400 = 0xB0000,
        WnncNetAvid = 0x1A0000,
        WnncNetAvid1 = 0x3A0000,
        WnncNetBmc = 0x180000,
        WnncNetBwnfs = 0x100000,
        WnncNetClearcase = 0x160000,
        WnncNetCogent = 0x110000,
        WnncNetCsc = 0x260000,
        WnncNetDav = 0x2E0000,
        WnncNetDce = 0x190000,
        WnncNetDecorb = 0x200000,
        WnncNetDfs = 0x3B0000,
        WnncNetDistinct = 0x230000,
        WnncNetDocuspace = 0x1B0000,
        WnncNetDriveonweb = 0x3E0000,
        WnncNetExifs = 0x2D0000,
        WnncNetExtendnet = 0x290000,
        WnncNetFarallon = 0x120000,
        WnncNetFjRedir = 0x220000,
        WnncNetFoxbat = 0x2B0000,
        WnncNetFrontier = 0x170000,
        WnncNetFtpNfs = 0xC0000,
        WnncNetGoogle = 0x430000,
        WnncNetHobNfs = 0x320000,
        WnncNetIbmal = 0x340000,
        WnncNetIntergraph = 0x140000,
        WnncNetKnoware = 0x2F0000,
        WnncNetKwnp = 0x3C0000,
        WnncNetLanman = 0x20000,
        WnncNetLanstep = 0x80000,
        WnncNetLantastic = 0xA0000,
        WnncNetLifenet = 0xE0000,
        WnncNetLock = 0x350000,
        WnncNetLocus = 0x60000,
        WnncNetMangosoft = 0x1C0000,
        WnncNetMasfax = 0x310000,
        WnncNetMfiles = 0x410000,
        WnncNetMsNfs = 0x420000,
        WnncNetNetware = 0x30000,
        WnncNetObjectDire = 0x300000,
        WnncNetOpenafs = 0x390000,
        WnncNetPathworks = 0xD0000,
        WnncNetPowerlan = 0xF0000,
        WnncNetProtstor = 0x210000,
        WnncNetQuincy = 0x380000,
        WnncNetRdr2Sample = 0x250000,
        WnncNetRiverfront1 = 0x1E0000,
        WnncNetRiverfront2 = 0x1F0000,
        WnncNetRsfx = 0x400000,
        WnncNetSernet = 0x1D0000,
        WnncNetShiva = 0x330000,
        WnncNetSrt = 0x370000,
        WnncNetStac = 0x2A0000,
        WnncNetSunPcNfs = 0x70000,
        WnncNetSymfonet = 0x150000,
        WnncNetTermsrv = 0x360000,
        WnncNetTwins = 0x240000,
        WnncNetVines = 0x40000,
        WnncNetVmware = 0x3F0000,
        WnncNetYahoo = 0x2C0000,
        WnncNetZenworks = 0x3D0000
    }

    [Flags]
    public enum ShareFlag
    {
        [Description("If set, the device name contains data")] ValidDevice = 0x0001,
        [Description("If set, the network provider type contains data")] ValidNetType = 0x0002
    }

    /// <summary>
    ///     Parses a CommonNetworkRelativeLink structure (MS-SHLLINK 2.3.2).
    ///     <paramref name="rawBytes" /> holds exactly CommonNetworkRelativeLinkSize bytes.
    /// </summary>
    public NetworkShareInfo(byte[] rawBytes, int codepage = 1252)
    {
        const string field = "CommonNetworkRelativeLink";

        // CommonNetworkRelativeLinkSize MUST be greater than or equal to 0x14
        Helpers.EnsureRange(rawBytes, 0, 0x14, field);

        var encoding = Helpers.GetEncoding(codepage);

        Size = BitConverter.ToInt32(rawBytes, 0);
        ShareFlags = (ShareFlag) BitConverter.ToInt32(rawBytes, 4);

        NetworkShareNameOffset = BitConverter.ToInt32(rawBytes, 8);
        DeviceNameOffset = BitConverter.ToInt32(rawBytes, 12);

        // NetworkProviderType MUST be ignored unless ValidNetType is set; the raw value is kept for reference.
        NetworkProviderType = (ProviderType) BitConverter.ToInt32(rawBytes, 16);

        NetworkShareName = Helpers.ReadAnsiZ(rawBytes, NetworkShareNameOffset, encoding, field);

        var validDevice = (ShareFlags & ShareFlag.ValidDevice) == ShareFlag.ValidDevice;

        DeviceName = string.Empty;
        if (validDevice && DeviceNameOffset > 0)
        {
            DeviceName = Helpers.ReadAnsiZ(rawBytes, DeviceNameOffset, encoding, field);
        }

        // NetNameOffsetUnicode and DeviceNameOffsetUnicode are present only when NetNameOffset > 0x14
        if (NetworkShareNameOffset > 0x14)
        {
            NetworkShareNameOffsetUnicode = Helpers.ReadInt32(rawBytes, 0x14, field);
            DeviceNameOffsetUnicode = Helpers.ReadInt32(rawBytes, 0x18, field);

            if (NetworkShareNameOffsetUnicode > 0)
            {
                NetworkShareName = Helpers.ReadUnicodeZ(rawBytes, NetworkShareNameOffsetUnicode, field);
            }

            if (validDevice && DeviceNameOffsetUnicode > 0)
            {
                DeviceName = Helpers.ReadUnicodeZ(rawBytes, DeviceNameOffsetUnicode, field);
            }
        }
    }

    public int Size { get; }
    public ShareFlag ShareFlags { get; }

    public int NetworkShareNameOffset { get; }
    public int DeviceNameOffset { get; }
    public int NetworkShareNameOffsetUnicode { get; }
    public int DeviceNameOffsetUnicode { get; }

    /// <summary>
    ///     True when <see cref="NetworkProviderType" /> is valid (ValidNetType flag set).
    /// </summary>
    public bool HasValidProviderType => (ShareFlags & ShareFlag.ValidNetType) == ShareFlag.ValidNetType;
    public ProviderType NetworkProviderType { get; }

    public string NetworkShareName { get; }
    public string DeviceName { get; }

    public override string ToString()
    {
        return
            $"Share flags: {ShareFlags}, NetworkOffset: {NetworkShareNameOffset}, DeviceOffset: {DeviceNameOffset}, Network Provider: {NetworkProviderType}, Share name: {NetworkShareName}, Device: {DeviceName}";
    }
}