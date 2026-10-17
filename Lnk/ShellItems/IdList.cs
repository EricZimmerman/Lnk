using System;
using System.Collections.Generic;
using ExtensionBlocks;

namespace Lnk.ShellItems;

internal static class IdList
{
    /// <summary>
    ///     Splits an IDList (a sequence of ItemIDs ending in a 2 byte TerminalID, MS-SHLLINK 2.2.1) into shell items, read
    ///     with ExtensionBlocks' shell item classes (shared with ShellBags Explorer, SBECmd and the RECmd plugins).
    ///     A malformed or unknown item becomes a <see cref="ShellBagDamaged" /> instead of aborting the whole file.
    /// </summary>
    /// <param name="buffer">Buffer holding the IDList</param>
    /// <param name="offset">Offset of the first ItemID</param>
    /// <param name="end">Offset one past the last byte the IDList may use</param>
    /// <param name="codepage">Code page for ANSI strings</param>
    /// <param name="errors">Receives a message for every item that could not be parsed</param>
    public static List<IShellBag> Parse(byte[] buffer, int offset, int end, int codepage, List<string> errors)
    {
        var items = new List<IShellBag>();
        var pos = offset;

        while (pos + 2 <= end)
        {
            var itemSize = BitConverter.ToUInt16(buffer, pos);

            if (itemSize == 0)
            {
                return items; // TerminalID
            }

            if (itemSize < 3 || pos + itemSize > end)
            {
                var remaining = new byte[end - pos];
                Buffer.BlockCopy(buffer, pos, remaining, 0, remaining.Length);
                var msg = $"Invalid ItemID size {itemSize} at offset {pos} (IDList ends at {end})";
                items.Add(new ShellBagDamaged(remaining, msg));
                errors?.Add(msg);
                return items;
            }

            var itemBytes = new byte[itemSize];
            Buffer.BlockCopy(buffer, pos, itemBytes, 0, itemSize);

            try
            {
                var item = ShellItemFactory.Create(itemBytes, -1, -1, string.Empty, codepage);

                if (item is ShellBagUnknown)
                {
                    throw new Exception(
                        $"Unknown shell item ID: 0x{itemBytes[2]:X}. Please send to saericzimmerman@gmail.com so support can be added.");
                }

                items.Add(item);
            }
            catch (Exception ex)
            {
                // Parsers that read with BitConverter directly throw range errors; turn them into something readable
                var reason = ex is ArgumentException or IndexOutOfRangeException
                    ? "item data ends before the parser expected (truncated or corrupt item)"
                    : ex.Message;
                var msg = $"Shell item at offset {pos} (type 0x{itemBytes[2]:X2}): {reason}";
                items.Add(new ShellBagDamaged(itemBytes, reason));
                errors?.Add(msg);
            }

            pos += itemSize;
        }

        if (pos < end)
        {
            errors?.Add($"IDList at offset {offset} is missing its TerminalID");
        }

        return items;
    }
}
