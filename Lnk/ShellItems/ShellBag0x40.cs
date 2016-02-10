using System;
using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    internal class ShellBag0x40 : ShellBag
    {
        private string _desc;

        public ShellBag0x40(int slot, int mruPosition, byte[] rawBytes, string bagPath)
        {
            Slot = slot;
            MruPosition = mruPosition;

            ChildShellBags = new List<IShellBag>();

            InternalId = Guid.NewGuid().ToString();

            HexValue = rawBytes;

            ExtensionBlocks = new List<IExtensionBlock>();

            BagPath = bagPath;

            switch (rawBytes[2])
            {
                case 0x47:
                    FriendlyName = "Entire Network";
                    break;
                case 0x46:
                    FriendlyName = "Microsoft Windows Network";
                    break;
                case 0x41:
                    FriendlyName = "Domain/Workgroup name";
                    break;

                case 0x42:
                    FriendlyName = "Server UNC path";
                    break;

                case 0x43:
                    FriendlyName = "Share UNC path";
                    break;

                default:
                    FriendlyName = "Network location";
                    break;
            }


            var temp = Encoding.GetEncoding(1252).GetString(rawBytes, 5, rawBytes.Length - 5).Split('\0');

            _desc = temp[1];

            Value = temp[0];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(base.ToString());

            return sb.ToString();
        }
    }
}