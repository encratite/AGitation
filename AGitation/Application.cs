using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AGitation
{
    class Application
    {
        static void Main(string[] arguments)
        {
            if (arguments.Length != 4)
            {
                string name = Assembly.GetExecutingAssembly().ManifestModule.Name;
                Console.WriteLine("Usage:");
                Console.WriteLine("{0} <path to the root of a git repository> <output path> <regular expression specifying which paths to include> <regular expression specyfing which paths to exclude>", name);
                return;
            }
            string repositoryPath = arguments[0];
            string outputPath = arguments[1];
            var inclusivePattern = new Regex(arguments[2]);
            var exclusivePattern = new Regex(arguments[3]);
            var analyzer = new Analyzer(repositoryPath, outputPath, inclusivePattern, exclusivePattern);
            analyzer.Run();
        }
    }
}
