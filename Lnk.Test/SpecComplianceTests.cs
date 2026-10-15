using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Lnk.ExtraData;
using Lnk.ShellItems;
using NUnit.Framework;

namespace Lnk.Test;

/// <summary>
///     Tests for MS-SHLLINK compliance and for malformed input. Most build their LNK data in memory so each rule
///     can be exercised exactly.
/// </summary>
[TestFixture]
public class SpecComplianceTests
{
    private const int HasTargetIdList = 0x01;
    private const int HasLinkInfo = 0x02;
    private const int HasName = 0x04;
    private const int HasArguments = 0x20;
    private const int HasIconLocation = 0x40;
    private const int IsUnicode = 0x80;

    /// <summary>
    ///     Minimal LNK writer: a valid 76 byte ShellLinkHeader followed by whatever the test appends.
    /// </summary>
    private sealed class LnkBytes
    {
        private readonly List<byte> _b = new();

        public LnkBytes(int flags)
        {
            U32(0x4C);
            _b.AddRange(new Guid("00021401-0000-0000-c000-000000000046").ToByteArray());
            U32((uint) flags);
            U32(0x20); // FILE_ATTRIBUTE_ARCHIVE
            I64(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc).ToFileTimeUtc()); // creation
            I64(new DateTime(2021, 6, 7, 8, 9, 10, DateTimeKind.Utc).ToFileTimeUtc()); // access
            I64(new DateTime(2022, 11, 12, 13, 14, 15, DateTimeKind.Utc).ToFileTimeUtc()); // write
            U32(1234); // FileSize
            U32(0); // IconIndex
            U32(1); // SW_SHOWNORMAL
            U16(0); // HotKey
            U16(0);
            U32(0);
            U32(0);
        }

        public int Length => _b.Count;

        public LnkBytes U16(ushort v) => Add(BitConverter.GetBytes(v));
        public LnkBytes U32(uint v) => Add(BitConverter.GetBytes(v));
        public LnkBytes I64(long v) => Add(BitConverter.GetBytes(v));

        public LnkBytes Add(byte[] bytes)
        {
            _b.AddRange(bytes);
            return this;
        }

        /// <summary>StringData: 16 bit character count followed by the characters (no terminator).</summary>
        public LnkBytes StringData(string s, bool unicode = true)
        {
            U16((ushort) s.Length);
            return Add(unicode ? Encoding.Unicode.GetBytes(s) : Encoding.ASCII.GetBytes(s));
        }

        public LnkBytes Terminal() => U32(0);

        public LnkBytes Poke(int offset, byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                _b[offset + i] = bytes[i];
            }

            return this;
        }

        public byte[] ToArray() => _b.ToArray();
    }

    private static byte[] Block(uint signature, byte[] payload)
    {
        var b = new List<byte>();
        b.AddRange(BitConverter.GetBytes((uint) (8 + payload.Length)));
        b.AddRange(BitConverter.GetBytes(signature));
        b.AddRange(payload);
        return b.ToArray();
    }

    [Test]
    public void ArgumentsLongerThan260CharactersAreNotTruncatedAndLaterFieldsStayAligned()
    {
        // Whitespace padding past 260 characters is a known technique for hiding a payload in Arguments.
        var args = new string(' ', 300) + "-enc SQBFAFgA";
        var data = new LnkBytes(HasName | HasArguments | HasIconLocation | IsUnicode)
            .StringData("a description")
            .StringData(args)
            .StringData(@"%SystemRoot%\System32\shell32.dll")
            .Terminal()
            .ToArray();

        var lnk = new LnkFile(data);

        lnk.Arguments.Should().Be(args);
        lnk.Arguments.Length.Should().Be(313);
        lnk.IconLocation.Should().Be(@"%SystemRoot%\System32\shell32.dll");
        lnk.ParseErrors.Count.Should().Be(0);
        lnk.ExtraDataTerminated.Should().BeTrue();
    }

    [Test]
    public void NameLongerThan260CharactersIsNotTruncated()
    {
        var name = new string('x', 1000);
        var data = new LnkBytes(HasName).StringData(name, false).Terminal().ToArray();

        var lnk = new LnkFile(data);

        lnk.Name.Should().Be(name);
    }

    [Test]
    public void StringDataCountAbove32767IsReadAsUnsigned()
    {
        var args = new string('A', 40000);
        var data = new LnkBytes(HasArguments).StringData(args, false).Terminal().ToArray();

        var lnk = new LnkFile(data);

        lnk.Arguments.Length.Should().Be(40000);
    }

    [Test]
    public void TruncatedStringDataThrowsParseExceptionWithField()
    {
        var data = new LnkBytes(HasArguments).U16(500).Add(new byte[10]).ToArray();

        Action action = () => _ = new LnkFile(data);

        var ex = action.Should().Throw<LnkParseException>().Which as LnkParseException;
        ex.Field.Should().Be("COMMAND_LINE_ARGUMENTS");
    }

    [Test]
    public void ToStringLabelsTargetTimestampsCorrectly()
    {
        var lnk = new LnkFile(new LnkBytes(0).Terminal().ToArray());

        lnk.Header.TargetCreationDate.Year.Should().Be(2020);
        lnk.Header.TargetLastAccessedDate.Year.Should().Be(2021);
        lnk.Header.TargetModificationDate.Year.Should().Be(2022);

        var text = lnk.ToString();
        text.Should().Contain($"Target modified: {lnk.Header.TargetModificationDate}");
        text.Should().Contain($"Target accessed: {lnk.Header.TargetLastAccessedDate}");
    }

    [Test]
    public void OneCorruptTimestampDoesNotAffectTheOthers()
    {
        var data = new LnkBytes(0).Terminal()
            .Poke(36, BitConverter.GetBytes(-1L)) // access time out of range
            .ToArray();

        var lnk = new LnkFile(data);

        lnk.Header.TargetLastAccessedDate.Should().Be(DateTimeOffset.MinValue);
        lnk.Header.TargetLastAccessedDateRaw.Should().Be(-1L);
        lnk.Header.TargetCreationDate.Year.Should().Be(2020);
        lnk.Header.TargetModificationDate.Year.Should().Be(2022);
        lnk.ParseErrors.Single().Should().Contain("last access time is out of range");
    }

    [Test]
    public void UndefinedShowCommandIsKeptAndReported()
    {
        // LECmd#12: BITTER APT shortcuts use ShowCommand 0x0D
        var data = new LnkBytes(0).Terminal().Poke(60, BitConverter.GetBytes(0x0D)).ToArray();

        var lnk = new LnkFile(data);

        ((int) lnk.Header.ShowWindow).Should().Be(0x0D);
        lnk.ParseErrors.Single().Should().Contain("ShowCommand 0xD is not a defined SW_* value");
    }

    [Test]
    public void HotKeyWithCombinedModifiers()
    {
        var data = new LnkBytes(0).Terminal().Poke(64, new byte[] { 0x47, 0x03 }).ToArray(); // SHIFT|CONTROL + G

        new LnkFile(data).Header.HotKey.Should().Be("SHIFT+CONTROL+G");

        var lnk = Lnk.LoadFile(Path.Combine(TestMain.MiscPath, "local.file.exec.test"));
        lnk.Header.HotKey.Should().Be("SHIFT+CONTROL+G");
    }

    [Test]
    public void ShimDataBlockIsParsedAsShimDataBlock()
    {
        var layer = Encoding.Unicode.GetBytes("WIN7RTM");
        var payload = new byte[0x80];
        Buffer.BlockCopy(layer, 0, payload, 0, layer.Length);

        var data = new LnkBytes(0).Add(Block(0xA0000008, payload)).Terminal().ToArray();

        var lnk = new LnkFile(data);

        var shim = lnk.ExtraBlocks.Single() as ShimDataBlock;
        shim.Should().NotBeNull();
        shim.LayerName.Should().Be("WIN7RTM");
    }

    [Test]
    public void VistaAndAboveIdListDataBlockParsesEveryItem()
    {
        // Reuse a real LinkTargetIDList (size field excluded) as the IDList of a VistaAndAboveIDListDataBlock.
        var source = File.ReadAllBytes(Path.Combine(TestMain.WinXpPath, "WordPad.lnk"));
        var idListSize = BitConverter.ToUInt16(source, 76);
        var idList = new byte[idListSize];
        Buffer.BlockCopy(source, 78, idList, 0, idListSize);
        var expected = new LnkFile(source).TargetIDs;
        expected.Count.Should().BeGreaterThan(1);

        var data = new LnkBytes(0).Add(Block(0xA000000C, idList)).Terminal().ToArray();
        var lnk = new LnkFile(data);

        var vista = lnk.ExtraBlocks.Single() as VistaAndAboveIdListDataBlock;
        vista.Should().NotBeNull();
        vista.TargetIDs.Count.Should().Be(expected.Count);
        vista.TargetIDs.Last().Value.Should().Be(expected.Last().Value);
        lnk.ParseErrors.Count.Should().Be(0);
    }

    private static byte[] LinkInfoWithUnicodeVolumeLabel(string label, string localPath)
    {
        // VolumeID with VolumeLabelOffset = 0x14 => the label is at VolumeLabelOffsetUnicode
        var labelBytes = Encoding.Unicode.GetBytes(label + "\0");
        var vol = new List<byte>();
        vol.AddRange(BitConverter.GetBytes((uint) (0x14 + labelBytes.Length)));
        vol.AddRange(BitConverter.GetBytes(3u)); // DRIVE_FIXED
        vol.AddRange(BitConverter.GetBytes(0xA1B2C3D4u));
        vol.AddRange(BitConverter.GetBytes(0x14u));
        vol.AddRange(BitConverter.GetBytes(0x14u));
        vol.AddRange(labelBytes);

        const int headerSize = 0x24;
        var ansiPath = Encoding.ASCII.GetBytes("?\0");
        var uniPath = Encoding.Unicode.GetBytes(localPath + "\0");
        var volOffset = headerSize;
        var ansiOffset = volOffset + vol.Count;
        var suffixOffset = ansiOffset + ansiPath.Length;
        var uniOffset = suffixOffset + 1;
        var uniSuffixOffset = uniOffset + uniPath.Length;
        var size = uniSuffixOffset + 2;

        var li = new List<byte>();
        li.AddRange(BitConverter.GetBytes((uint) size));
        li.AddRange(BitConverter.GetBytes((uint) headerSize));
        li.AddRange(BitConverter.GetBytes(1u)); // VolumeIDAndLocalBasePath
        li.AddRange(BitConverter.GetBytes((uint) volOffset));
        li.AddRange(BitConverter.GetBytes((uint) ansiOffset));
        li.AddRange(BitConverter.GetBytes(0u));
        li.AddRange(BitConverter.GetBytes((uint) suffixOffset));
        li.AddRange(BitConverter.GetBytes((uint) uniOffset));
        li.AddRange(BitConverter.GetBytes((uint) uniSuffixOffset));
        li.AddRange(vol);
        li.AddRange(ansiPath);
        li.Add(0); // empty ANSI CommonPathSuffix
        li.AddRange(uniPath);
        li.AddRange(new byte[2]); // empty Unicode CommonPathSuffix
        return li.ToArray();
    }

    [Test]
    public void UnicodeVolumeLabelAndLocalPath()
    {
        var data = new LnkBytes(HasLinkInfo)
            .Add(LinkInfoWithUnicodeVolumeLabel("Données 数据", @"D:\Документы\файл.txt"))
            .Terminal()
            .ToArray();

        var lnk = new LnkFile(data);

        lnk.VolumeInfo.VolumeLabel.Should().Be("Données 数据");
        lnk.VolumeInfo.VolumeLabelOffsetUnicode.Should().Be(0x14);
        lnk.VolumeInfo.VolumeSerialNumber.Should().Be("A1B2C3D4");
        lnk.LocalPath.Should().Be(@"D:\Документы\файл.txt");
        lnk.CommonPath.Should().Be("");
        lnk.ParseErrors.Count.Should().Be(0);
    }

    [Test]
    public void NetworkOnlyLinkDoesNotInventALocalPath()
    {
        // LinkInfoHeaderSize 0x24 with LocalBasePathOffsetUnicode = 0: the old parser decoded garbage at offset 0
        var lnk = Lnk.LoadFile(Path.Combine(TestMain.MiscPath, "unicodeNetworkPath.lnk.test"));

        lnk.LocalPath.Should().BeNull();
        lnk.CommonPath.Should().Be("relay\\说明.txt");
        lnk.NetworkShareInfo.NetworkShareName.Should().Be("\\\\\u0793\\C");
        lnk.NetworkShareInfo.HasValidProviderType.Should().BeTrue();
    }

    [Test]
    public void ConsoleDataBlockMatchesSpec()
    {
        var lnk = Lnk.LoadFile(Path.Combine(TestMain.MiscPath, "local.file.env.test"));

        var console = lnk.ExtraBlocks.OfType<ConsoleDataBlock>().Single();

        console.ColorTable.Count.Should().Be(16);
        console.ColorTable[1].Should().Be(0x800000u); // dark blue (COLORREF 0x00BBGGRR)
        console.ColorTable[15].Should().Be(0xFFFFFFu); // white
    }

    [Test]
    public void TrackerDataBlockVersionAndLength()
    {
        var lnk = Lnk.LoadFile(Path.Combine(TestMain.MiscPath, "local.directory.seven.test"));

        var tracker = lnk.ExtraBlocks.OfType<TrackerDataBaseBlock>().Single();

        tracker.Length.Should().Be(0x58u);
        tracker.Version.Should().Be(0);
    }

    [Test]
    public void DataAfterTerminalBlockIsReported()
    {
        var data = new LnkBytes(0).Terminal().Add(Encoding.ASCII.GetBytes("MZ appended payload")).ToArray();

        var lnk = new LnkFile(data);

        lnk.ExtraDataTerminated.Should().BeTrue();
        lnk.TrailingDataSize.Should().Be(19);
        lnk.ParseErrors.Single().Should().Contain("follow the ExtraData terminal block");
    }

    [Test]
    public void MissingTerminalBlockIsReported()
    {
        var lnk = new LnkFile(new LnkBytes(0).ToArray());

        lnk.ExtraDataTerminated.Should().BeFalse();
        lnk.TrailingDataSize.Should().Be(0);
        lnk.ParseErrors.Single().Should().Contain("no terminal block");
    }

    [Test]
    public void OversizedLengthFieldsDoNotAllocate()
    {
        // LinkInfoSize of 0x7FFFFFFF used to allocate ~2 GB before the bounds check
        var data = new LnkBytes(HasLinkInfo).U32(0x7FFFFFFF).Add(new byte[64]).ToArray();

        var before = GC.GetAllocatedBytesForCurrentThread();
        Action action = () => _ = new LnkFile(data);
        action.Should().Throw<LnkParseException>().WithMessage("*corrupt*");
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(1024 * 1024);
    }

    [Test]
    public void ControlPanelItemUnderThisPcParses()
    {
        // This file is in Bad because its 20 byte 0x2E item (the Control Panel GUID) used to be reported as corrupt.
        // Damaged items are covered by the placeholder tests below
        var lnk = Lnk.LoadFile(Path.Combine(TestMain.BadPath, "ImDisk Virtual Disk Driver.test"));

        lnk.ParseErrors.Should().BeEmpty();
        lnk.TargetIDs.Select(t => t.Value).Should().Equal("This PC", "All Control Panel Items",
            @"C:\WINDOWS\System32\imdisk.cpl (ImDisk Virtual Disk Driver,ImDisk Virtual Disk Driver,)");
    }

    [Test]
    public void UnknownShellItemTypeBecomesPlaceholder()
    {
        // IDList with one item of unknown type 0xEE, then the TerminalID
        var item = new byte[] { 0x05, 0x00, 0xEE, 0x01, 0x02 };
        var data = new LnkBytes(HasTargetIdList).U16((ushort) (item.Length + 2)).Add(item).U16(0).Terminal().ToArray();

        var lnk = new LnkFile(data);

        var damaged = lnk.TargetIDs.Single() as ShellBagDamaged;
        damaged.Should().NotBeNull();
        damaged.ItemType.Should().Be((byte) 0xEE);
        damaged.ErrorMessage.Should().Contain("Unknown shell item ID");
    }

    [Test]
    public void TruncatedRootFolderItemGetsReadableDamageReason()
    {
        // LECmd#8 (0614ed9d...): a 0x1F item with the My Computer CLSID followed by junk
        var item = Convert.FromHexString(
            "61001f07e04fd020ea3a6910a2d808002b30309d329304314e666a755095f87b1a925c9fd66623ed1d18e596347f26736d1b" +
            "9b3dfd6340cbddb53d7b03126900c1aa8122c58ca64e2367c5e032d5280cd6dfafa37d39e28a98853d0d68f81fdd75");
        var data = new LnkBytes(HasTargetIdList).U16((ushort) (item.Length + 2)).Add(item).U16(0).Terminal().ToArray();

        var lnk = new LnkFile(data);

        var damaged = lnk.TargetIDs.Single() as ShellBagDamaged;
        damaged.Should().NotBeNull();
        damaged.ItemType.Should().Be((byte) 0x1F);
        damaged.ErrorMessage.Should().Contain("ends before the parser expected");
        lnk.ParseErrors.Single().Should().StartWith("Shell item at offset 78 (type 0x1F)");
    }

    [Test]
    public void EmptyOrTinyFilesThrowParseException()
    {
        var dir = Path.Combine(Path.GetTempPath(), "lnk-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            foreach (var size in new[] { 0, 1, 75 })
            {
                var f = Path.Combine(dir, $"tiny{size}.lnk");
                var bytes = new byte[size];
                if (size > 0)
                {
                    bytes[0] = 0x4C;
                }

                File.WriteAllBytes(f, bytes);
                Action action = () => Lnk.LoadFile(f);
                action.Should().Throw<LnkParseException>().WithMessage("*too small*");
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void ByteArrayConstructorHasNoSourceFile()
    {
        var lnk = new LnkFile(new LnkBytes(0).Terminal().ToArray());

        lnk.SourceFile.Should().BeNull();
        lnk.SourceCreated.Should().BeNull();
    }

    [Test]
    public void TwentyByteRootFolderGuidItemIsParsed()
    {
        // 0x2E item: size, type, a byte and the "Web Folders" GUID, with no extension blocks (from "target 2._lnk").
        // It used to be read as a control panel category and reported as truncated
        var item = new byte[] { 0x14, 0x00, 0x2E, 0x00 }
            .Concat(new Guid("bdeadf00-c265-11d0-bced-00a0c90ab50f").ToByteArray()).ToArray();

        var lnk = new LnkFile(new LnkBytes(0x1) // HasLinkTargetIDList
            .U16((ushort) (item.Length + 2)).Add(item).U16(0)
            .Terminal().ToArray());

        lnk.ParseErrors.Should().BeEmpty();
        var bag = lnk.TargetIDs.Should().ContainSingle().Which.Should().BeOfType<ShellBag0X2E>().Subject;
        bag.FriendlyName.Should().Be("Root folder: GUID");
        bag.Value.Should().Contain("Web Folders");
    }

    [Test]
    public void JumpListCorpusLnksParseWithoutErrors()
    {
        // The local "Jump lists and lnks" collection (not in the repository; some files are private)
        var corpus = Environment.GetEnvironmentVariable("JUMPLIST_CORPUS");
        if (string.IsNullOrEmpty(corpus) || !Directory.Exists(corpus))
        {
            corpus = TestMain.FindUp("Jump lists and lnks");
        }

        if (corpus == null)
        {
            Assert.Ignore("Jump list corpus not present");
        }

        var files = Directory.GetFiles(corpus, "*", SearchOption.AllDirectories)
            .Where(t => (t.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                         t.EndsWith("_lnk", StringComparison.OrdinalIgnoreCase)) &&
                        !t.Contains("jumplist tools"))
            .ToList();
        files.Count.Should().BeGreaterThan(1000);

        var withErrors = files.Select(t => (File: t, Lnk: Lnk.LoadFile(t)))
            .Where(t => t.Lnk.ParseErrors.Count > 0)
            .Select(t => $"{t.File}: {string.Join("; ", t.Lnk.ParseErrors)}")
            .ToList();

        withErrors.Should().BeEmpty(string.Join(Environment.NewLine, withErrors));
    }

    [Test]
    public void ItaCorpusParsesWithoutErrors()
    {
        if (TestMain.ItaPath == null)
        {
            Assert.Ignore("ITA_LNKs corpus not present");
        }

        var files = Directory.GetFiles(TestMain.ItaPath, "*", SearchOption.AllDirectories);
        files.Length.Should().BeGreaterThan(0);

        var withErrors = new List<string>();
        foreach (var file in files)
        {
            var lnk = Lnk.LoadFile(file);
            if (lnk.ParseErrors.Count > 0)
            {
                withErrors.Add($"{Path.GetFileName(file)}: {string.Join("; ", lnk.ParseErrors)}");
            }
        }

        // Everything must parse cleanly. "target 2._lnk" has a 20 byte 0x2E item (size, type, a byte and the
        // "Web Folders" GUID) that used to be read as a control panel category and reported as truncated.
        withErrors.Should().BeEmpty(string.Join(Environment.NewLine, withErrors));
    }
}
