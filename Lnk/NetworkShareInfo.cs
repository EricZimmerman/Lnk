using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Lnk
{
    public class NetworkShareInfo
    {
        //TODO add descriptions to these

        public enum ProviderType
        {
            WNNC_NET_10NET = 0x50000,
            WNNC_NET_3IN1 = 0x270000,
            WNNC_NET_9TILES = 0x90000,
            WNNC_NET_APPLETALK = 0x130000,
            WNNC_NET_AS400 = 0xB0000,
            WNNC_NET_AVID = 0x1A0000,
            WNNC_NET_AVID1 = 0x3A0000,
            WNNC_NET_BMC = 0x180000,
            WNNC_NET_BWNFS = 0x100000,
            WNNC_NET_CLEARCASE = 0x160000,
            WNNC_NET_COGENT = 0x110000,
            WNNC_NET_CSC = 0x260000,
            WNNC_NET_DAV = 0x2E0000,
            WNNC_NET_DCE = 0x190000,
            WNNC_NET_DECORB = 0x200000,
            WNNC_NET_DFS = 0x3B0000,
            WNNC_NET_DISTINCT = 0x230000,
            WNNC_NET_DOCUSPACE = 0x1B0000,
            WNNC_NET_DRIVEONWEB = 0x3E0000,
            WNNC_NET_EXIFS = 0x2D0000,
            WNNC_NET_EXTENDNET = 0x290000,
            WNNC_NET_FARALLON = 0x120000,
            WNNC_NET_FJ_REDIR = 0x220000,
            WNNC_NET_FOXBAT = 0x2B0000,
            WNNC_NET_FRONTIER = 0x170000,
            WNNC_NET_FTP_NFS = 0xC0000,
            WNNC_NET_GOOGLE = 0x430000,
            WNNC_NET_HOB_NFS = 0x320000,
            WNNC_NET_IBMAL = 0x340000,
            WNNC_NET_INTERGRAPH = 0x140000,
            WNNC_NET_KNOWARE = 0x2F0000,
            WNNC_NET_KWNP = 0x3C0000,
            WNNC_NET_LANMAN = 0x20000,
            WNNC_NET_LANSTEP = 0x80000,
            WNNC_NET_LANTASTIC = 0xA0000,
            WNNC_NET_LIFENET = 0xE0000,
            WNNC_NET_LOCK = 0x350000,
            WNNC_NET_LOCUS = 0x60000,
            WNNC_NET_MANGOSOFT = 0x1C0000,
            WNNC_NET_MASFAX = 0x310000,
            WNNC_NET_MFILES = 0x410000,
            WNNC_NET_MS_NFS = 0x420000,
            WNNC_NET_NETWARE = 0x30000,
            WNNC_NET_OBJECT_DIRE = 0x300000,
            WNNC_NET_OPENAFS = 0x390000,
            WNNC_NET_PATHWORKS = 0xD0000,
            WNNC_NET_POWERLAN = 0xF0000,
            WNNC_NET_PROTSTOR = 0x210000,
            WNNC_NET_QUINCY = 0x380000,
            WNNC_NET_RDR2SAMPLE = 0x250000,
            WNNC_NET_RIVERFRONT1 = 0x1E0000,
            WNNC_NET_RIVERFRONT2 = 0x1F0000,
            WNNC_NET_RSFX = 0x400000,
            WNNC_NET_SERNET = 0x1D0000,
            WNNC_NET_SHIVA = 0x330000,
            WNNC_NET_SRT = 0x370000,
            WNNC_NET_STAC = 0x2A0000,
            WNNC_NET_SUN_PC_NFS = 0x70000,
            WNNC_NET_SYMFONET = 0x150000,
            WNNC_NET_TERMSRV = 0x360000,
            WNNC_NET_TWINS = 0x240000,
            WNNC_NET_VINES = 0x40000,
            WNNC_NET_VMWARE = 0x3F0000,
            WNNC_NET_YAHOO = 0x2C0000,
            WNNC_NET_ZENWORKS = 0x3D0000
        }

        public enum ShareFlag
        {
            [Description("If set the device name contains data")] ValidDevice = 0x0001,
            [Description("If set the network provider type contains data")] ValidNetType = 0x0002
        }

        public NetworkShareInfo(byte[] rawBytes)
        {
            Size = BitConverter.ToInt32(rawBytes, 0);
            ShareFlags = (ShareFlag) BitConverter.ToInt32(rawBytes, 4);

            NetworkShareNameOffset = BitConverter.ToInt32(rawBytes, 8);
            DeviceNameOffset = BitConverter.ToInt32(rawBytes, 12);

            NetworkProviderType = (ProviderType) BitConverter.ToInt32(rawBytes, 16);

            if (NetworkShareNameOffset > 20)
            {
                NetworkShareName = Encoding.Unicode
                    .GetString(rawBytes, NetworkShareNameOffset, (rawBytes.Length - NetworkShareNameOffset)*2);

                if (DeviceNameOffset > 0)
                {
                    DeviceName = Encoding.Unicode
                        .GetString(rawBytes, DeviceNameOffset, (rawBytes.Length - DeviceNameOffset)*2);
                }
            }
            else
            {
                NetworkShareName = Encoding.GetEncoding(1252)
                    .GetString(rawBytes, NetworkShareNameOffset, rawBytes.Length - NetworkShareNameOffset)
                    .Split('\0')
                    .First();

                DeviceName = string.Empty;
                if (DeviceNameOffset > 0)
                {
                    DeviceName = Encoding.GetEncoding(1252)
                        .GetString(rawBytes, DeviceNameOffset, rawBytes.Length - DeviceNameOffset).Split('\0').First();
                }
            }
        }

        public int Size { get; }
        public ShareFlag ShareFlags { get; }

        public int NetworkShareNameOffset { get; }
        public int DeviceNameOffset { get; }
        public ProviderType NetworkProviderType { get; }

        public string NetworkShareName { get; }
        public string DeviceName { get; }

        public override string ToString()
        {
            return
                $"Share flags: {ShareFlags}, NetworkOffset: {NetworkShareNameOffset}, DeviceOffset: {DeviceNameOffset}, Network Provider: {NetworkProviderType}, Share name: {NetworkShareName}, Device: {DeviceName}";
        }
    }
}