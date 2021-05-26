using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util.Collections;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Spring.test
{
    public enum ParserType
    {
        Incremental,
        Regular,
    }

    public class IncrementalLexerFactory : ILexerFactory
    {
        public ILexer CreateLexer(IBuffer buffer)
        {
            return new SpringIncrementalLexer(buffer);
        }
    }
    
    public class LexerFactory : ILexerFactory
    {
        public ILexer CreateLexer(IBuffer buffer)
        {
            return new SpringLexer(buffer);
        }
    }

    [TestFixture]
    public class ParseSpeedTestBase
    {
        protected readonly Random Random = new();

        protected void PrintParseSpeed(ParserType parserType, Action<SpringParser> changeText = null)
        {
            var benchmark = new HashMap<string, TimeSpan>();
            var reparseBenchmark = changeText == null ? null : new HashMap<string, TimeSpan>();
            foreach (var filename in Filenames)
            {
                var content =
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
                var parser = new SpringParser(parserType == ParserType.Incremental
                    ? new SpringIncrementalLexer(new StringBuffer(content))
                    : new SpringLexer(new StringBuffer(content)));
                parser.ParseFile();
                parser.ParseFile();
                parser.ParseFile();
                parser.ParseFile();
                parser.ParseFile();
                benchmark[filename] = fetchTime(() => parser.ParseFile());
                Console.WriteLine($"{parserType}: file {filename}, - {benchmark[filename].ToFormatString()}");
                if (changeText == null) continue;
                changeText(parser);
                reparseBenchmark[filename] = fetchTime(() => parser.ParseFile());
                Console.WriteLine(
                    $"{parserType} reparse: file {filename}, - {reparseBenchmark[filename].ToFormatString()}");
            }

            var fullTime = benchmark.Aggregate(new TimeSpan(), (acc, x) => acc.Add(x.Value));
            Console.WriteLine($"{parserType}: Project parse time is {fullTime.ToFormatString()}");
            if (changeText != null)
            {
                var reparseTime = reparseBenchmark.Aggregate(new TimeSpan(), (acc, x) => acc.Add(x.Value));
                Console.WriteLine($"{parserType} reparse: Project reparse time is {reparseTime.ToFormatString()}");
            }

            Console.WriteLine();
        }
        
        protected void PrintLexerSpeed(ParserType lexrType, Action<SpringParser> changeText)
        {
            var benchmark = new HashMap<string, TimeSpan>();
            var relexBenchmark = changeText == null ? null : new HashMap<string, TimeSpan>();
            foreach (var filename in Filenames)
            {
                var content =
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
                SpringParser lexr = null;
                benchmark[filename] = fetchTime(() => lexr = new SpringParser(lexrType == ParserType.Incremental
                    ? new SpringIncrementalLexer(new StringBuffer(content))
                    : new SpringLexer(new StringBuffer(content))));
                Console.WriteLine($"{lexrType}: file {filename}, - {benchmark[filename].ToFormatString()}");
                changeText(lexr);
                relexBenchmark[filename] = fetchTime(() => changeText(lexr));
                Console.WriteLine(
                    $"{lexrType} relex: file {filename}, - {relexBenchmark[filename].ToFormatString()}");
            }

            var fullTime = benchmark.Aggregate(new TimeSpan(), (acc, x) => acc.Add(x.Value));
            Console.WriteLine($"{lexrType}: Project lex time is {fullTime.ToFormatString()}");
            if (changeText != null)
            {
                var relexTime = relexBenchmark.Aggregate(new TimeSpan(), (acc, x) => acc.Add(x.Value));
                Console.WriteLine($"{lexrType} relex: Project relex time is {relexTime.ToFormatString()}");
            }

            Console.WriteLine();
        }

        protected static SpringLexer CreateIncrementalLexer(string str)
        {
            return new SpringIncrementalLexer(new StringBuffer(str));
        }


        protected readonly string[] Filenames =
        {
            "BanSystem.pas",
            "FileServer.pas",
            "LobbyClient.pas",
            "Main.pas",
            "Rcon.pas",
            // "Server.pas",
            "ServerCommands.pas",
            "ServerHelper.pas",
            // "ServerLoop.pas",
        };

        protected TimeSpan fetchTime(Action x)
        {
            var sw = new Stopwatch();
            sw.Start();
            x();
            sw.Stop();
            return sw.Elapsed;
        }

        protected char GetRandomChar()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789;()_:";

            return chars[Random.Next(chars.Length)];
        }
    }

    internal static class TimeSpanExtensions
    {
        public static string ToFormatString(this TimeSpan timeSpan)
        {
            var sb = new StringBuilder();
            if (timeSpan.Seconds != 0)
            {
                sb.Append($"{timeSpan.Seconds} с ");
            }

            if (timeSpan.Milliseconds != 0)
            {
                sb.Append($"{timeSpan.TotalMilliseconds} мс");
            }

            return sb.ToString();
        }
    }
}
