using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ExtensionBlocks;
using FluentAssertions;
using Lnk.ExtraData;
using Lnk.ShellItems;
using NUnit.Framework;

namespace Lnk.Test;

[TestFixture]
public class TestMain
{
    public static string BasePath = @"..\..\TestFiles";
    public static string Win10Path = Path.Combine(BasePath, "Win10");
    public static string WinXpPath = Path.Combine(BasePath, "WinXP");
    public static string Win2K3Path = Path.Combine(BasePath, "Win2k3");
    public static string Win7Path = Path.Combine(BasePath, "Win7");
    public static string Win80Path = Path.Combine(BasePath, "Win8.0");
    public static string Win81Path = Path.Combine(BasePath, "Win8.1");
    public static string Win2012Path = Path.Combine(BasePath, "Win2012");
    public static string Win2012R2Path = Path.Combine(BasePath, "Win2012R2");
    public static string WinVistaPath = Path.Combine(BasePath, "WinVista");

    public static string MiscPath = Path.Combine(BasePath, "Misc");
    public static string BadPath = Path.Combine(BasePath, "Bad");

    private readonly List<string> _allPaths = new List<string>
    {
        MiscPath,
        WinXpPath,
        Win10Path,
        Win2K3Path,
        Win7Path,
        Win80Path,
        Win81Path,
        Win2012Path,
        Win2012R2Path,
        WinVistaPath
    };

    [Test]
    public void BaseTests()
    {
        foreach (var allPath in _allPaths)
        {
            foreach (var file in Directory.GetFiles(allPath, "*.test"))
            {
                var lk = Lnk.LoadFile(file);

                lk.Should().NotBe(null);

                lk.Header.Should().NotBeNull();
            }
        }
    }

    [Test]
    public void DarwinBlockLnk()
    {
        var darwin = Path.Combine(MiscPath, "local.file.darwin.test");

        var lnk = Lnk.LoadFile(darwin);

        var d = lnk.ExtraBlocks.Single(t => t is DarwinDataBlock) as DarwinDataBlock;

        d?.ProductCode.Should().Be("{AC76BA86-7AD7-1036-7B44-A93000000001}");
        d?.ComponentId.Should().Be("{F95C9759-13AF-4E41-A46B-DBD281608D53}");
        d?.FeatureName.Should().Be("ReaderProgramFiles");
    }

    [Test]
    public void foobar()
    {
         // var ls = Directory.GetFiles(@"C:\Temp\");
//           var ls = Directory.GetFiles(@"C:\Temp\llunknown");
//
            /*foreach (var s in ls)
            {
                if (s.EndsWith("lnk") == false)
                {
                    continue;
                }
                Console.WriteLine(s);
                var l = Lnk.LoadFile(s);
                Console.WriteLine(l);
            }*/

            var aaaaa = new ShellBag0X2E(File.ReadAllBytes(@"C:\temp\2e.bin"));
            Console.WriteLine(aaaaa);
            
            return;
            
        var ll = Lnk.LoadFile(@"C:\temp\bl.bin");
        Console.WriteLine(ll);

        //    ll = Lnk.LoadFile(@"C:\Temp\1\12dc1ea8e34b5a6.automaticDestinations-ms\AppId_12dc1ea8e34b5a6_DirName_2.lnk");
        //     Debug.WriteLine(ll);
    }

    [Test]
    public void InvalidFileShouldThrowException()
    {
        var badFile = Path.Combine(BadPath, "$I2GXWHL.lnk");
        Action action = () => Lnk.LoadFile(badFile);

            

        action.Should().Throw<Exception>().WithMessage("Invalid signature!");
    }

    [Test]
    public void UnicodeNetworkPath()
    {
        var winP =Path.Combine(MiscPath, "unicodeNetworkPath.lnk.test");

        var lnk = Lnk.LoadFile(winP);
        Debug.WriteLine(lnk);
           
    }
    
    [Test]
    public void Sps()
    {
        var winP =@"C:\temp\sps";

        var po = new PropertyStore(File.ReadAllBytes(winP));
        Debug.WriteLine(po);
           
    }

    [Test]
    public void RemoteFileLnk()
    {
        var winP = Path.Combine(MiscPath, "remote.file.xp.test");

        var lnk = Lnk.LoadFile(winP);

        lnk.RelativePath.Should().BeNull();

        lnk.VolumeInfo.Should().BeNull();

        lnk.NetworkShareInfo.NetworkShareName.Should().Be("\\\\ALS-FICHIERS3\\QUALITÉ");
        lnk.NetworkShareInfo.NetworkProviderType.Should().Be(NetworkShareInfo.ProviderType.WnncNetLanman);

        var dom = lnk.TargetIDs.Single(t => t.FriendlyName == "Domain/Workgroup name");
        dom.Value.Should().Be("Aldec_lyon");

        lnk.CommonPath.Should().Be(@"Archives\Méthodologie WAS\Norme de développement JAVA.doc");

        var tdb = lnk.ExtraBlocks.Single(t => t.GetType().Name == "TrackerDataBaseBlock") as TrackerDataBaseBlock;

        tdb?.VolumeDroid.Should().Be("00000000-0000-0000-0000-000000000000");
        tdb?.FileDroid.Should().Be("ea461b34-9877-11da-80bd-000f1ff7c0dc");

        tdb?.MacAddress.Should().Be("00:0f:1f:f7:c0:dc");
        tdb?.MachineId.Should().Be("als-fichiers3");

        tdb?.CreationTime.Year.Should().Be(2006);
        tdb?.CreationTime.Month.Should().Be(2);
        tdb?.CreationTime.Day.Should().Be(8);
        tdb?.CreationTime.Hour.Should().Be(7);
        tdb?.CreationTime.Minute.Should().Be(52);
        tdb?.CreationTime.Second.Should().Be(55);

        lnk.ExtraBlocks.Count.Should().Be(1);

        var tID = lnk.TargetIDs.Last();

        tID.ExtensionBlocks.Count.Should().Be(1);
        tID.Value.Should().Be("Norme de développement JAVA.doc");

        var b4 = tID.ExtensionBlocks.Last() as Beef0004;
        b4?.MFTInformation.MFTEntryNumber.Should().Be(null);
        b4?.MFTInformation.MFTSequenceNumber.Should().Be(null);

        var tsf = lnk.ExtraBlocks.Any(t => t.GetType().Name == "SpecialFolderDataBlock");

        tsf.Should().BeFalse();
    }

    [Test]
    public void Win7DirectoryLnk()
    {
        var win7LocalDir = Path.Combine(MiscPath, "local.directory.seven.test");

        var lnk = Lnk.LoadFile(win7LocalDir);

        lnk.RelativePath.Should().Be(@"..\..\Administrator");

        lnk.VolumeInfo.Should().NotBeNull();

        lnk.VolumeInfo.DriveType.Should().Be(VolumeInfo.DriveTypes.DriveFixed);
        lnk.VolumeInfo.VolumeSerialNumber.Should().Be("502E1A8A");
        lnk.VolumeInfo.VolumeLabel.Should().Be("SSD-WIN7");

        lnk.LocalPath.Should().Be(@"C:\Users\");


        var tdb = lnk.ExtraBlocks.Single(t => t.GetType().Name == "TrackerDataBaseBlock") as TrackerDataBaseBlock;

        tdb?.VolumeDroid.Should().Be("4f7c66da-d320-4cc4-8d50-165dd98ebc01");
        tdb?.FileDroid.Should().Be("136502ff-8c66-11df-b6eb-001377d34a59");

        tdb?.MacAddress.Should().Be("00:13:77:d3:4a:59");
        tdb?.MachineId.Should().Be("netbook");

        tdb?.CreationTime.Year.Should().Be(2010);
        tdb?.CreationTime.Month.Should().Be(7);
        tdb?.CreationTime.Day.Should().Be(10);
        tdb?.CreationTime.Hour.Should().Be(20);
        tdb?.CreationTime.Minute.Should().Be(59);
        tdb?.CreationTime.Second.Should().Be(48);
    }

    [Test]
    public void Win81Lnk()
    {
        var winP = Path.Combine(Win81Path, "PhotosApp.lnk");

        var lnk = Lnk.LoadFile(winP);

        lnk.RelativePath.Should().BeNull();

        lnk.VolumeInfo.Should().BeNull();


        lnk.IconLocation.Should().Be(@"%windir%\FileManager\PhotosApp.exe");


        var evb =
            lnk.ExtraBlocks.Single(t => t.GetType().Name ==
                                        "EnvironmentVariableDataBlock") as EnvironmentVariableDataBlock;

        evb?.EnvironmentVariablesUnicode.Should().Be(@"%windir%\FileManager\PhotosApp.exe");

        var psdb =
            lnk.ExtraBlocks.Single(t => t.GetType().Name == "PropertyStoreDataBlock") as PropertyStoreDataBlock;

        psdb?.PropertyStore.Sheets.Count.Should().Be(6);

        var foo = psdb.PropertyStore.Sheets.Single(t => t.GUID == "86d40b4d-9069-443c-819a-2a54090dccec");

        foo.PropertyNames.Count.Should().Be(9);
        foo.PropertyNames.First().Key.Should().Be("2");
        foo.PropertyNames.First().Value.Should().Be("Assets\\PhotosSmallLogo.png");

        var desc = Utils.GetDescriptionFromGuidAndKey("86d40b4d-9069-443c-819a-2a54090dccec", 2);

        desc.Should().Be("Tile Small Image Location");

        lnk.ExtraBlocks.Count.Should().Be(2);
    }

    [Test]
    public void WinVistaLnk()
    {
        var winP = Path.Combine(WinVistaPath, "Windows Update.lnk");

        var lnk = Lnk.LoadFile(winP);

        lnk.RelativePath.Should().Be(@"..\..\..\..\Windows\System32\wuapp.exe");

        lnk.VolumeInfo.Should().NotBeNull();

        lnk.VolumeInfo.DriveType.Should().Be(VolumeInfo.DriveTypes.DriveFixed);
        lnk.VolumeInfo.VolumeSerialNumber.Should().Be("D85CC709");
        lnk.VolumeInfo.VolumeLabel.Should().Be("TestOS");

        lnk.LocalPath.Should().Be(@"D:\Windows\System32\wuapp.exe");

        var tdb = lnk.ExtraBlocks.Single(t => t.GetType().Name == "TrackerDataBaseBlock") as TrackerDataBaseBlock;

        tdb?.VolumeDroid.Should().Be("545b5616-9d9b-4f68-8fc9-98b391eeee2c");
        tdb?.FileDroid.Should().Be("a7bdf3f4-6a85-11db-b5ae-f1534be43d84");

        tdb?.MacAddress.Should().Be("f1:53:4b:e4:3d:84");
        tdb?.MachineId.Should().Be("lh-ixn3n2mx5l20");

        tdb?.CreationTime.Year.Should().Be(2006);
        tdb?.CreationTime.Month.Should().Be(11);
        tdb?.CreationTime.Day.Should().Be(2);
        tdb?.CreationTime.Hour.Should().Be(15);
        tdb?.CreationTime.Minute.Should().Be(20);
        tdb?.CreationTime.Second.Should().Be(21);

        lnk.ExtraBlocks.Count.Should().Be(4);

        var tID = lnk.TargetIDs.Last();

        tID.ExtensionBlocks.Count.Should().Be(1);
        tID.Value.Should().Be("wuapp.exe");

        var b4 = tID.ExtensionBlocks.Last() as Beef0004;

        b4?.MFTInformation.MFTEntryNumber.Should().Be((ulong) 39556);
        b4?.MFTInformation.MFTSequenceNumber.Should().Be(1);

        var tsf =
            lnk.ExtraBlocks.Single(t => t.GetType().Name == "SpecialFolderDataBlock") as SpecialFolderDataBlock;

        tsf?.SpecialFolderId.Should().Be(37);
    }

    [Test]
    public void WinXPProgramLnk()
    {
        var winxpProgram = Path.Combine(WinXpPath, "WordPad.lnk");

        var lnk = Lnk.LoadFile(winxpProgram);

        lnk.RelativePath.Should().Be(@"..\..\..\..\..\Program Files\Windows NT\Accessories\wordpad.exe");

        lnk.VolumeInfo.Should().NotBeNull();

        lnk.VolumeInfo.DriveType.Should().Be(VolumeInfo.DriveTypes.DriveFixed);
        lnk.VolumeInfo.VolumeSerialNumber.Should().Be("E0F7E847");
        lnk.VolumeInfo.VolumeLabel.Should().Be("");

        lnk.LocalPath.Should().Be(@"C:\Program Files\Windows NT\Accessories\wordpad.exe");


        var tdb = lnk.ExtraBlocks.Single(t => t.GetType().Name == "TrackerDataBaseBlock") as TrackerDataBaseBlock;

        tdb?.VolumeDroid.Should().Be("00000000-0000-0000-0000-000000000000");
        tdb?.FileDroid.Should().Be("0a897547-b9e8-11e5-9ab2-ffbfe31cf845");

        tdb?.MacAddress.Should().Be("ff:bf:e3:1c:f8:45");
        tdb?.MachineId.Should().Be("xppro");

        tdb?.CreationTime.Year.Should().Be(2016);
        tdb?.CreationTime.Month.Should().Be(1);
        tdb?.CreationTime.Day.Should().Be(13);
        tdb?.CreationTime.Hour.Should().Be(11);
        tdb?.CreationTime.Minute.Should().Be(23);
        tdb?.CreationTime.Second.Should().Be(16);

        lnk.ExtraBlocks.Count.Should().Be(2);

        var tID = lnk.TargetIDs.Last();

        tID.ExtensionBlocks.Count.Should().Be(1);
        tID.Value.Should().Be("wordpad.exe");

        var b4 = tID.ExtensionBlocks.Last() as Beef0004;

        b4?.MFTInformation.MFTEntryNumber.Should().Be(null);
    }
}