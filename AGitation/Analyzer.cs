using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace AGitation
{
    class Analyzer
    {
        string _RepositoryPath;
        string _OutputPath;
        Regex _InclusivePattern;
        Regex _ExclusivePattern;

        IEnumerable<string> _RemainingTargets;
        Dictionary<string, long> _Results;
        long _TotalLineCount;
        int _FilesProcessed;
        int _FileCount;

        public Analyzer(string repositoryPath, string outputPath, Regex inclusivePattern, Regex exclusivePattern)
        {
            _RepositoryPath = repositoryPath;
            _OutputPath = outputPath;
            _InclusivePattern = inclusivePattern;
            _ExclusivePattern = exclusivePattern;
        }

        public void Run()
        {
            _Results = new Dictionary<string, long>();
            _TotalLineCount = 0;
            var files = Directory.GetFiles(_RepositoryPath, "*", SearchOption.AllDirectories);
            _RemainingTargets = files.Select(path =>
            {
                path = path.Substring(_RepositoryPath.Length);
                path = path.Replace("\\", "/");
                if (path.Length > 0 && path[0] == '/')
                    path = path.Substring(1);
                return path;
            });
            _RemainingTargets = _RemainingTargets.Where(path => _InclusivePattern.Match(path).Success && !_ExclusivePattern.Match(path).Success);
            _FilesProcessed = 0;
            _FileCount = _RemainingTargets.Count();
            var threads = new List<Thread>();
            int threadCount = Environment.ProcessorCount;
            Console.WriteLine("Using {0} thread(s)", threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(BlameThread);
                thread.Start();
                threads.Add(thread);
            }
            foreach (var thread in threads)
                thread.Join();
            using (var outputStream = new StreamWriter(_OutputPath))
            {
                var pairs = _Results.Select(pair => pair).OrderByDescending(pair => pair.Value);
                foreach (var pair in pairs)
                {
                    string author = pair.Key;
                    long lineCount = pair.Value;
                    double percentage = (double)lineCount / _TotalLineCount * 100.0;
                    outputStream.WriteLine("{0}: {1:0.0}% ({2})", author, percentage, lineCount);
                }
            }
        }

        void BlameThread()
        {
            using (var repository = new Repository(_RepositoryPath))
            {
                bool firstTime = true;
                while (true)
                {
                    string path;
                    lock (this)
                    {
                        if (firstTime)
                            firstTime = false;
                        else
                            _FilesProcessed++;
                        if (_RemainingTargets.Count() == 0)
                            return;
                        path = _RemainingTargets.First();
                        _RemainingTargets = _RemainingTargets.Skip(1);
                        if (_FilesProcessed % 10 == 0)
                            Console.WriteLine("Files: {0}/{1}, authors: {2}, lines: {3}", _FilesProcessed, _FileCount, _Results.Keys.Count, _TotalLineCount);
                        Console.WriteLine("Processing {0}", path);
                    }
                    try
                    {
                        var blameHunks = repository.Blame(path);
                        foreach (var blameHunk in blameHunks)
                        {
                            string author = blameHunk.FinalSignature.Name;
                            int lineCount = blameHunk.LineCount;
                            lock (this)
                            {
                                _TotalLineCount += lineCount;
                                if (!_Results.ContainsKey(author))
                                    _Results[author] = 0;
                                _Results[author] += lineCount;
                            }
                        }
                    }
                    catch
                    {
                        // This path is likely not part of the history
                    }
                }
            }
        }
    }
}
