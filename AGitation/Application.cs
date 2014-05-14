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
		static void AnalyseRepository(string repositoryPath, string outputPath, Regex pattern)
		{
			long totalLineCount = 0;
			var results = new Dictionary<string, long>();
			using (var repository = new Repository(repositoryPath))
			{
				var files = Directory.GetFiles(repositoryPath, "*", SearchOption.AllDirectories);
				var targets = files.Where(file => pattern.Match(file).Success);
				int filesProcessed = 0;
				int fileCount = targets.Count();
				foreach (var file in targets)
				{
					if (filesProcessed % 10 == 0)
						Console.WriteLine("Files: {0}/{1}, authors: {2}, lines: {3}", filesProcessed, fileCount, results.Keys.Count, totalLineCount);
					if (!pattern.Match(file).Success)
						continue;
					string path = file.Substring(repositoryPath.Length);
					path = path.Replace("\\", "/");
					if (path.Length > 0 && path[0] == '/')
						path = path.Substring(1);
					try
					{
						var blameHunks = repository.Blame(path);
						foreach (var blameHunk in blameHunks)
						{
							string author = blameHunk.FinalSignature.Name;
							int lineCount = blameHunk.LineCount;
							totalLineCount += lineCount;
							if (!results.ContainsKey(author))
								results[author] = 0;
							results[author] += lineCount;
						}
					}
					catch
					{
						// This path is likely not part of the history
					}
					filesProcessed++;
				}
			}
			using (var outputStream = new StreamWriter(outputPath))
			{
				var pairs = results.Select(pair => pair).OrderByDescending(pair => pair.Value);
				foreach (var pair in pairs)
				{
					string author = pair.Key;
					long lineCount = pair.Value;
					double percentage = (double)lineCount / totalLineCount * 100.0;
					outputStream.WriteLine("{0}: {1:0.0}% ({2})", author, percentage, lineCount);
				}
			}
		}

		static void Main(string[] arguments)
		{
			if (arguments.Length != 3)
			{
				string name = Assembly.GetExecutingAssembly().ManifestModule.Name;
				Console.WriteLine("Usage:");
				Console.WriteLine("{0} <path to the root of a git repository> <output path> <regular expression specifying which paths to analyse>", name);
				return;
			}
			string repositoryPath = arguments[0];
			string outputPath = arguments[1];
			var pattern = new Regex(arguments[2]);
			AnalyseRepository(repositoryPath, outputPath, pattern);
		}
	}
}
