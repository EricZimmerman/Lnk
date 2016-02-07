using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk
{
   public class NetworkShareInfo
    {
       public enum ShareFlag
        {
            [Description("If set the device name contains data")]
            ValidDevice=0x0001,
            [Description("If set the network provider type contains data")]
            ValidNetType=0x0002
        }

        //TODO add descriptions to these
       public enum ProviderType
       {
            WNNC_NET_AVID = 0x001a0000,
            WNNC_NET_DOCUSPACE = 0x001b0000,
            WNNC_NET_MANGOSOFT = 0x001c0000,
            WNNC_NET_SERNET = 0x001d0000,
            WNNC_NET_RIVERFRONT1 = 0x001e0000,
            WNNC_NET_RIVERFRONT2 = 0x001f0000,
            WNNC_NET_DECORB = 0x00200000,
            WNNC_NET_PROTSTOR = 0x00210000,
            WNNC_NET_FJ_REDIR = 0x00220000,
            WNNC_NET_DISTINCT = 0x00230000,
            WNNC_NET_TWINS = 0x00240000,
            WNNC_NET_RDR2SAMPLE = 0x00250000,
            WNNC_NET_CSC = 0x00260000,
            WNNC_NET_3IN1 = 0x00270000,
            WNNC_NET_EXTENDNET = 0x00290000,
            WNNC_NET_STAC = 0x002a0000,
            WNNC_NET_FOXBAT = 0x002b0000,
            WNNC_NET_YAHOO = 0x002c0000,
            WNNC_NET_EXIFS = 0x002d0000,
            WNNC_NET_DAV = 0x002e0000,
            WNNC_NET_KNOWARE = 0x002f0000,
            WNNC_NET_OBJECT_DIRE = 0x00300000,
            WNNC_NET_MASFAX = 0x00310000,
            WNNC_NET_HOB_NFS = 0x00320000,
            WNNC_NET_SHIVA = 0x00330000,
            WNNC_NET_IBMAL = 0x00340000,
            WNNC_NET_LOCK = 0x00350000,
            WNNC_NET_TERMSRV = 0x00360000,
            WNNC_NET_SRT = 0x00370000,
            WNNC_NET_QUINCY = 0x00380000,
            WNNC_NET_OPENAFS = 0x00390000,
            WNNC_NET_AVID1 = 0x003a0000,
            WNNC_NET_DFS = 0x003b0000,
            WNNC_NET_KWNP = 0x003c0000,
            WNNC_NET_ZENWORKS = 0x003d0000,
            WNNC_NET_DRIVEONWEB = 0x003e0000,
            WNNC_NET_VMWARE = 0x003f0000,
            WNNC_NET_RSFX = 0x00400000,
            WNNC_NET_MFILES = 0x00410000,
            WNNC_NET_MS_NFS = 0x00420000,
            WNNC_NET_GOOGLE = 0x00430000

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
              .GetString(rawBytes, NetworkShareNameOffset, (rawBytes.Length - NetworkShareNameOffset) * 2);

                if (DeviceNameOffset > 0)
                {
                    DeviceName = Encoding.Unicode
             .GetString(rawBytes, DeviceNameOffset, (rawBytes.Length - DeviceNameOffset) * 2);
                }

            }
            else
            {
                NetworkShareName = Encoding.GetEncoding(1252)
              .GetString(rawBytes, NetworkShareNameOffset, rawBytes.Length - NetworkShareNameOffset).Split('\0').First();

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
    }
}
