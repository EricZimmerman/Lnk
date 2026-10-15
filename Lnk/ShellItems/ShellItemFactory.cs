using System;
using System.Collections.Generic;

namespace Lnk.ShellItems;

internal static class ShellItemFactory
{
    /// <summary>
    ///     Splits an IDList (a sequence of ItemIDs ending in a 2 byte TerminalID, MS-SHLLINK 2.2.1) into shell items.
    ///     A malformed or unknown item becomes a <see cref="ShellBagDamaged" /> instead of aborting the whole file.
    /// </summary>
    /// <param name="buffer">Buffer holding the IDList</param>
    /// <param name="offset">Offset of the first ItemID</param>
    /// <param name="end">Offset one past the last byte the IDList may use</param>
    /// <param name="codepage">Code page for ANSI strings</param>
    /// <param name="errors">Receives a message for every item that could not be parsed</param>
    public static List<ShellBag> ParseIdList(byte[] buffer, int offset, int end, int codepage, List<string> errors)
    {
        var items = new List<ShellBag>();
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
                items.Add(Create(itemBytes, codepage));
            }
            catch (Exception ex)
            {
                // Older parsers read with BitConverter directly; turn their range errors into something readable
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

    public static ShellBag Create(byte[] shellItem, int codepage)
    {
        if (shellItem.Length >= 0x29)
        {
            var sig1 = BitConverter.ToInt64(shellItem, 0x8);
            var sig2 = BitConverter.ToInt64(shellItem, 0x18);

            // zip folder contents: no signature, but a '/' in a date string or "N/A" at one of these positions
            if (sig1 == 0 && sig2 == 0 &&
                (shellItem[0x28] == 0x2f || shellItem[0x26] == 0x2f || shellItem[0x1a] == 0x2f || shellItem[0x1c] == 0x2f))
            {
                return new ShellBagZipContents(shellItem);
            }
        }

        switch (shellItem[2])
        {
            case 0x1f:
                return new ShellBag0X1F(shellItem, codepage);
            case 0x22:
            case 0x23:
                return new ShellBag0X23(shellItem, codepage);
            case 0x2a:
            case 0x2f:
                return new ShellBag0X2F(shellItem, codepage);
            case 0x2e:
                return new ShellBag0X2E(shellItem);
            case 0xb1:
            case 0x31:
            case 0x3A:
            case 0x35:
            case 0x39:
                return new ShellBag0X31(shellItem, codepage);
            case 0x32:
            case 0x36:
                return new ShellBag0X32(shellItem, codepage);
            case 0x00:
                return new ShellBag0X00(shellItem, codepage);
            case 0x01:
                return new ShellBag0X01(shellItem);
            case 0x71:
                return new ShellBag0X71(shellItem);
            case 0x61:
                return new ShellBag0X61(shellItem, codepage);
            case 0xC3:
                return new ShellBag0Xc3(shellItem, codepage);
            case 0x74:
            case 0x77:
                return new ShellBag0X74(shellItem, codepage);
            case 0xae:
            case 0xaa:
            case 0x79:
                return new ShellBagZipContents(shellItem);
            case 0x41:
            case 0x42:
            case 0x43:
            case 0x46:
            case 0x47:
                return new ShellBag0X40(shellItem, codepage);
            case 0x4C:
                return new ShellBag0X4C(shellItem);
            default:
                throw new LnkParseException(
                    $"Unknown shell item ID: 0x{shellItem[2]:X}. Please send to saericzimmerman@gmail.com so support can be added.",
                    2, "ItemID");
        }
    }
}
