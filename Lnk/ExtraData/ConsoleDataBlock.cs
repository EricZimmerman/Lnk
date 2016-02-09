using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lnk.ExtraData
{
    [Flags]
    public enum FillAttribute
    {
        ForegroundBlue = 0x0001,
        ForegroundGreen = 0x0002,
        ForegroundRed = 0x0003,
        ForegroundIntensity = 0x0004,
        BackgroundBlue = 0x00010,
        BackgroundGreen = 0x00020,
        BackgroundRed = 0x00040,
        BackgroundIntensity = 0x00080
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

    public   class ConsoleDataBlock:ExtraDataBase
    {
        public override string ToString()
        {
            return $"Size: {Size}, Fill Attr: {FillAttributes}, Popup Attr: {PopupFillAttributes}, WidthBufferSize: {ScreenWidthBufferSize}, HeightBufferSize: {ScreenHeightBufferSize}, Window Width: {WindowWidth}, " +
                   $"Window height: {WindowHeight}, OriginX: {WindowOriginX}, OriginY: {WindowOriginY}, FontSize: {FontSize}, IsBold: {IsBold}, FaceName: {FaceName}, CursorSize: {CursorSize}, IsFullScreen: {IsFullScreen}," +
                   $"IsQuickEdit: {IsQuickEdit}, IsInsertMode: {IsInsertMode}, IsAutoPositioned: {IsAutoPositioned}, HistoryBufferSize: {HistoryBufferSize}, HistoryBufferCount: {HistoryBufferCount}, HistoryDupesAllowed: {HistoryDuplicatesAllowed}";
        }

        public ExtraDataTypes Signature { get; }

        public uint Size { get; }

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
        public uint FontSize { get; }
        public FontFamily FontFamily { get; }

        /// <summary>
        /// If false, font is regular weight
        /// </summary>
        public bool IsBold { get; }

        public string FaceName { get; }

        public CursorWeight CursorSize { get; }

        public bool IsFullScreen { get; }
        public bool IsQuickEdit { get; }
        public bool IsInsertMode { get; }
        public bool IsAutoPositioned { get; }

        public uint HistoryBufferSize { get; }
        public uint HistoryBufferCount { get; }

        public bool HistoryDuplicatesAllowed { get; }

        public List<uint> ColorTable { get; } 

        public ConsoleDataBlock(byte[] rawBytes)
        {
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

            FontSize = BitConverter.ToUInt16(rawBytes, 34);

            FontFamily = (FontFamily) BitConverter.ToUInt32(rawBytes, 36);

            var fontWeight = BitConverter.ToUInt16(rawBytes, 40);
            IsBold = fontWeight >= 700;

            FaceName = Encoding.Unicode.GetString(rawBytes, 44,64).Split('\0').First();

            var curSize = BitConverter.ToUInt32(rawBytes, 108);

            if (curSize <= 25)
            {
                CursorSize = CursorWeight.Small;
            }
            else if (curSize> 26 && curSize<=50)
            {
                CursorSize = CursorWeight.Normal;
            }
            else if (curSize > 50 && curSize <= 100)
            {
                CursorSize = CursorWeight.Large;
            }

            IsFullScreen = BitConverter.ToUInt32(rawBytes, 112) > 0;
            IsQuickEdit = BitConverter.ToUInt32(rawBytes, 116) > 0;
            IsInsertMode = BitConverter.ToUInt32(rawBytes, 120) > 0;
            IsAutoPositioned = BitConverter.ToUInt32(rawBytes, 124) > 0;
            HistoryBufferSize = BitConverter.ToUInt32(rawBytes, 128);
            HistoryBufferCount = BitConverter.ToUInt32(rawBytes, 132);
            HistoryDuplicatesAllowed = BitConverter.ToUInt32(rawBytes, 136) > 0;

            ColorTable = new List<uint>();

            for (var i = 0; i < 8; i++)
            {
                ColorTable.Add(BitConverter.ToUInt32(rawBytes,140 + (i*8)));
            }



        }
    }
}
