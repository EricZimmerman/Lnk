using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FluentAssertions;
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
        public void BaseTests()
        {
            foreach (var allPath in _allPaths)
            {
                foreach (var file in Directory.GetFiles(allPath, "*.test"))
                {
                    var lk = Lnk.LoadFile(file);

                    lk.Should().NotBe(null);

                    lk.Header.Should().NotBeNull();

                    if ((lk.Header.DataFlags & Header.DataFlag.HasLinkInfo) == Header.DataFlag.HasLinkInfo)
                    {
                    }



                    Debug.WriteLine($"{lk}----------------------------END OF FILE----------------------------\r\n");
                }
            }
        }
    }
}