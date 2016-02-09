using System;
using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0Xc3 : ShellBag
    {
        public ShellBag0Xc3(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            FriendlyName = "Network location";

            ChildShellBags = new List<IShellBag>();

            //if (bagPath.Contains(@"BagMRU\5\3") && slot == 0)
            //{
            //    Debug.WriteLine(1);
            //}

            InternalId = Guid.NewGuid().ToString();

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            var index = 0;

            var size = BitConverter.ToUInt16(rawBytes, index);
            index += 2;

            var classtypeIndicator = rawBytes[index] & 0x70;
            index += 1;

            var unknown1 = rawBytes[index];
            index += 1;

            var flags = rawBytes[index];
            index += 1;

//            if (flags != 0xc1 && flags != 0xc5)
//            {
//                SiAuto.Main.LogWarning("Different flag found! {0}", flags);
//            }

            int len = 0;

            //SiAuto.Main.LogMessage("Walking out 0s");
            while (rawBytes[index + len] != 0x0)
            {
                len += 1;
            }

            var tempBytes = new byte[len];
            Array.Copy(rawBytes, index, tempBytes, 0, len);

            index += len;

            var location = Encoding.ASCII.GetString(tempBytes);

            //    SiAuto.Main.LogMessage("location: {0}", location);

            //SiAuto.Main.LogMessage("Walking out 0s");
            while (rawBytes[index] == 0x0)
            {
                index += 1;
                if (index >= rawBytes.Length)
                {
                    break;
                }
            }

            //TODO there is still more here. finish this
            //4D-69-63-72-6F-73-6F-66-74-20-4E-65-74-77-6F-72-6B-00-00-02-00-00-00
            //M-i-c-r-o-s-o-f-t- -N-e-t-w-o-r-k------

            Value = location;

            // SiAuto.Main.LogMessage("Got drive letter: {0}", Value);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}