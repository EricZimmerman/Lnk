using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FluentAssertions;
using Lnk.ExtraData;
using NUnit.Framework;

namespace Lnk.Test
{
    [TestFixture]
    public class TestMain
    {
        public static string BasePath = @"..\..\TestFiles";
        public static string Win10Path = Path.Combine(BasePath, "Win10");
        public static string WinXpPath = Path.Combine(BasePath, "WinXP");
        public static string Win2k3Path = Path.Combine(BasePath, "Win2k3");
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
            Win2k3Path,
            Win7Path,
            Win80Path,
            Win81Path,
            Win2012Path,
            Win2012R2Path,
            WinVistaPath
        };

        [Test]
        public void InvalidFileShouldThrowException()
        {
            var badFile = Path.Combine(BadPath, "$I2GXWHL.lnk");
            Action action = () => Lnk.LoadFile(badFile);

            action.ShouldThrow<Exception>().WithMessage("Invalid signature!");
        }

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
        public void Win7DirectoryLnk()
        {
            var win7LocalDir = Path.Combine(MiscPath, "local.directory.seven.test");

            var lnk = Lnk.LoadFile(win7LocalDir);

            lnk.RelativePath.Should().Be(@"..\..\Administrator");

            lnk.VolumeInfo.Should().NotBeNull();

            lnk.VolumeInfo.DriveType.Should().Be(VolumeInfo.DriveTypes.DRIVE_FIXED);
            lnk.VolumeInfo.DriveSerialNumber.Should().Be("502E1A8A");
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
    }
}