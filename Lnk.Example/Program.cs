using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace Lnk.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed((options =>
                {
                    var lnkFile = options.InputFileInfo;
                    var lnk = Lnk.LoadFile(lnkFile.FullName);
                    var serialized = System.Text.Json.JsonSerializer.Serialize<LnkFile>(lnk);
                    Console.WriteLine(serialized);
                }));
        }


        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "The target to be parsed.")]
            public FileInfo InputFileInfo { get; set; }
        }
    }
}