using System;
using System.Collections.Generic;

namespace Lnk.ExtraData;

[Flags]
public enum FillAttribute
{
    ForegroundBlue = 0x0001,
    ForegroundGreen = 0x0002,
    ForegroundRed = 0x0004,
    ForegroundIntensity = 0x0008,
    BackgroundBlue = 0x0010,
    BackgroundGreen = 0x0020,
    BackgroundRed = 0x0040,
    BackgroundIntensity = 0x0080
}

public enum FontFamily
{
    Dontcare = 0x00,
    Roman = 0x10,
    Swiss = 0x20,
    Modern = 0x30,
    Script = 0x40,
    Decorative = 0x50
}

public enum CursorWeight
{
    Small = 1,
    Normal = 2,
    Large = 3
}

public class ConsoleDataBlock : ExtraDataBase
{
    public ConsoleDataBlock(byte[] rawBytes)
    {
        RequireSize(rawBytes, 0xCC, "ConsoleDataBlock");

        Signature = ExtraDataTypes.ConsoleDataBlock;

        Size = BitConverter.ToUInt32(rawBytes, 0);

        FillAttributes = (FillAttribute) BitConverter.ToUInt16(rawBytes, 8);
        PopupFillAttributes = (FillAttribute) BitConverter.ToUInt16(rawBytes, 10);
        ScreenWidthBufferSize = BitConverter.ToInt16(rawBytes, 12);
        ScreenHeightBufferSize = BitConverter.ToInt16(rawBytes, 14);
        WindowWidth = BitConverter.ToInt16(rawBytes, 16);
        WindowHeight = BitConverter.ToInt16(rawBytes, 18);
        WindowOriginX = BitConverter.ToInt16(rawBytes, 20);
        WindowOriginY = BitConverter.ToInt16(rawBytes, 22);

        Reserved0 = BitConverter.ToInt32(rawBytes, 24);
        Reserved1 = BitConverter.ToInt32(rawBytes, 28);

        // FontSize: the high word is the font height and the low word the font width
        FontWidth = BitConverter.ToUInt16(rawBytes, 32);
        FontSize = BitConverter.ToUInt16(rawBytes, 34);

        // FontFamily: high nibble is the family (FF_*), low nibble the pitch (TMPF_*)
        var fontFamily = BitConverter.ToUInt32(rawBytes, 36);
        FontFamily = (FontFamily) (fontFamily & 0xF0);
        FontPitch = fontFamily & 0x0F;

        FontWeight = BitConverter.ToUInt32(rawBytes, 40);
        IsBold = FontWeight >= 700;

        FaceName = Helpers.ReadUnicodeZ(rawBytes, 44, "FaceName", 44 + 64);

        // CursorSize: <= 25 small, 26-50 medium, 51-100 large
        CursorSizeRaw = BitConverter.ToUInt32(rawBytes, 108);
        CursorSize = CursorSizeRaw switch
        {
            <= 25 => CursorWeight.Small,
            <= 50 => CursorWeight.Normal,
            _ => CursorWeight.Large
        };

        IsFullScreen = BitConverter.ToUInt32(rawBytes, 112) > 0;
        IsQuickEdit = BitConverter.ToUInt32(rawBytes, 116) > 0;
        IsInsertMode = BitConverter.ToUInt32(rawBytes, 120) > 0;
        IsAutoPositioned = BitConverter.ToUInt32(rawBytes, 124) > 0;
        HistoryBufferSize = BitConverter.ToUInt32(rawBytes, 128);
        HistoryBufferCount = BitConverter.ToUInt32(rawBytes, 132);
        // HistoryNoDup: non-zero means duplicates are NOT allowed in the history buffer
        HistoryDuplicatesAllowed = BitConverter.ToUInt32(rawBytes, 136) == 0;

        // ColorTable: 16 COLORREF (4 byte) entries
        ColorTable = new List<uint>(16);

        for (var i = 0; i < 16; i++)
        {
            ColorTable.Add(BitConverter.ToUInt32(rawBytes, 140 + i * 4));
        }
    }

    public FillAttribute FillAttributes { get; }
    public FillAttribute PopupFillAttributes { get; }

    public short ScreenWidthBufferSize { get; }
    public short ScreenHeightBufferSize { get; }
    public short WindowWidth { get; }
    public short WindowHeight { get; }
    public short WindowOriginX { get; }
    public short WindowOriginY { get; }

    public int Reserved0 { get; }
    public int Reserved1 { get; }
    /// <summary>
    ///     Font height in pixels
    /// </summary>
    public uint FontSize { get; }

    /// <summary>
    ///     Font width in pixels
    /// </summary>
    public uint FontWidth { get; }

    public FontFamily FontFamily { get; }

    /// <summary>
    ///     Low nibble of the FontFamily field (TMPF_* pitch flags)
    /// </summary>
    public uint FontPitch { get; }

    public uint FontWeight { get; }

    /// <summary>
    ///     If false, font is regular weight
    /// </summary>
    public bool IsBold { get; }

    public string FaceName { get; }

    public CursorWeight CursorSize { get; }

    /// <summary>
    ///     Cursor size as stored (percentage of the character cell)
    /// </summary>
    public uint CursorSizeRaw { get; }

    public bool IsFullScreen { get; }
    public bool IsQuickEdit { get; }
    public bool IsInsertMode { get; }
    public bool IsAutoPositioned { get; }

    public uint HistoryBufferSize { get; }
    public uint HistoryBufferCount { get; }

    public bool HistoryDuplicatesAllowed { get; }

    public List<uint> ColorTable { get; }

    public override string ToString()
    {
        return
            $"Console data block" +
            $"\r\nFill Attributes: {FillAttributes}" +
            $"\r\nPopup Attributes: {PopupFillAttributes}" +
            $"\r\nBuffer Size (Width x Height): {ScreenWidthBufferSize} x {ScreenHeightBufferSize}" +
            $"\r\nWindow Size (Width x Height): {WindowWidth} x {WindowHeight}" +
            $"\r\nOrigin (X/Y): {WindowOriginX}/{WindowOriginY}" +
            $"\r\nFont Size: {FontSize}" +
            $"\r\nIs Bold: {IsBold}" +
            $"\r\nFace Name: {FaceName}" +
            $"\r\nCursor Size: {CursorSize}" +
            $"\r\nIs Full Screen: {IsFullScreen}" +
            $"\r\nIs Quick Edit: {IsQuickEdit}" +
            $"\r\nIs Insert Mode: {IsInsertMode}" +
            $"\r\nIs Auto Positioned: {IsAutoPositioned}" +
            $"\r\nHistory Buffer Size: {HistoryBufferSize}" +
            $"\r\nHistory Buffer Count: {HistoryBufferCount}" +
            $"\r\nHistory Duplicates Allowed: {HistoryDuplicatesAllowed}";
    }
}