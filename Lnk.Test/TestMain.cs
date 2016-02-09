using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Lnk.Test
{
    [TestFixture]
    public class TestMain
    {
        public static string BasePath = @"..\..\TestFiles";
        public static string Win10Path = Path.Combine(BasePath,"Win10");
        public static string WinXpPath = Path.Combine(BasePath,"WinXP");
        public static string MiscPath = Path.Combine(BasePath,"Misc");

        private readonly List<string> _allPaths = new List<string>
        {
            MiscPath,
            //WinXpPath,
            Win10Path
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

                    // Debug.WriteLine($"{file} {lk.Header}");


                }
            }
        }

    }
}
