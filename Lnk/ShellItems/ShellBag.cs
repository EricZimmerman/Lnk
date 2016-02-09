using System;
using System.Collections.Generic;
using System.Text;
using ExtensionBlocks;

namespace Lnk.ShellItems
{
    public abstract class ShellBag : IShellBag
    {
        public string InternalId { get; set; }

        public string FriendlyName { get; set; }

        public byte[] HexValue { get; set; }

        public string BagPath { get; set; }

        public string AbsolutePath { get; set; }

        public int Slot { get; set; }

        public bool IsDeleted { get; set; }
        
        public int MruPosition { get; set; }
        public int NodeSlot { get; set; }

        public List<IShellBag> ChildShellBags { get; set; }

        public string Value { get; set; }

        public DateTimeOffset? LastWriteTime { get; set; }

        public DateTimeOffset? FirstExplored { get; set; }

        public DateTimeOffset? LastExplored { get; set; }




        public List<IExtensionBlock> ExtensionBlocks { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Value: {Value}");
            sb.AppendLine($"Shell Type: {FriendlyName}");

            sb.AppendLine();

            sb.AppendLine($"Bag Path: {BagPath}, Slot #: {Slot}, MRU Position: {MruPosition}, Node Slot: {NodeSlot}");
            sb.AppendLine($"Absolute Path: {AbsolutePath}");

            sb.AppendLine();

            if (IsDeleted)
            {
                sb.AppendLine("Deleted: True");
                sb.AppendLine();
            }

                sb.AppendLine($"# Child Bags: {ChildShellBags.Count}");

          if (FirstExplored.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine(
                    $"First explored: {FirstExplored.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            }

            if (LastExplored.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine(
                    $"Last explored: {LastExplored.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            }

            if (ExtensionBlocks.Count > 0)
            {
                var extensionNumber = 0;

                sb.AppendLine();
                sb.AppendLine($"Extension blocks found: {ExtensionBlocks.Count}");

                foreach (var extensionBlock in ExtensionBlocks)
                {
                    if (extensionBlock is BeefPlaceHolder)
                    {
                        continue;
                    }

                        sb.AppendLine($"---------------------- Block {extensionNumber:N0} ----------------------");

                    sb.AppendLine(extensionBlock.ToString());

                    extensionNumber += 1;
                }

                sb.AppendLine("--------------------------------------------------");
            }

            if (LastWriteTime.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine(
                    $"Last Write Time: {LastWriteTime.Value.ToString(Utils.GetDateTimeFormatWithMilliseconds())}");
            }

            sb.AppendLine();

            sb.AppendLine($"Hex Value: {BitConverter.ToString(HexValue)}");

            return sb.ToString();
        }
    }
}