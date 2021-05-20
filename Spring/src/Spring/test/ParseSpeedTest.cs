using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util.Collections;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Spring.test
{
    public class SpringIncrementalLexerFactory : ILexerFactory
    {
        public ILexer CreateLexer(IBuffer buffer)
        {
            return new SpringIncrementalLexer(buffer);
        }
    }

    [TestFixture]
    public class ParseSpeedTest
    {
        private readonly Random _random = new();

        private ILexerFactory GetLexerFactory(bool incremental = false)
        {
            if (incremental)
            {
                return new SpringIncrementalLexerFactory();
            }

            return new SpringLanguageService.SpringLexerFactory();
        }

        private readonly string[] _filenames =
        {
            "BanSystem.pas",
            "FileServer.pas",
            "LobbyClient.pas",
            "Main.pas",
            "Rcon.pas",
            "Server.pas",
            "ServerCommands.pas",
            "ServerHelper.pas",
            "ServerLoop.pas",
        };

        [Test]
        public void FilesBench()
        {
            var chars = 0;
            var lexemes = 0;
            var strings = 0;
            foreach (var filename in _filenames)
            {
                var content =
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
                chars += content.Length;
                strings += File.ReadLines(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename))
                    .Count();
                lexemes += new TokenBuffer(new SpringLexer(new StringBuffer(content))).CachedTokens.Count;
            }
            Console.WriteLine($" {chars} {lexemes} {strings}");
        }

        [Test]
        public void TestReparseSpeed()
        {
            var parsers = new HashMap<string, IIncrementalParser>();
            var createParser =
                new Func<string, IIncrementalParser>(s => new SpringParser(new SpringLexer(new StringBuffer(s))));
            PrintParseSpeed(s =>
            {
                parsers[s] = createParser(s);
                return parsers[s];
            }, "Parser");
            // PrintParseSpeed((s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s)))), "Parser with incremental lexer");
        }

        [Test]
        public void TestParseSpeed()
        {
            PrintParseSpeed((s => new SpringParser(new SpringLexer(new StringBuffer(s)))), "Parser");
            // PrintParseSpeed((s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s)))), "Parser with incremental lexer");
        }

        [Test]
        public void TestReparseNoChange()
        {
            // PrintParseSpeed(s => new SpringParser(new SpringLexer(new StringBuffer(s))), "Parser", x => x);
            PrintParseSpeed(s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s))), "IncrementalParser",
                x => x);
        }

        [Test]
        public void TestParseSpeedChangeRandomChar()
        {
            PrintParseSpeed(s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s))), "Incremental",
                oldLex =>
                {
                    var chars = oldLex.Buffer.GetText().ToCharArray();
                    chars[_random.Next(chars.Length)] = GetRandomChar();
                    // return CreateIncrementalLexer(new string(chars));
                    return CreateLexer(new string(chars));
                });
        }

        private SpringLexer CreateLexer(string str)
        {
            return new SpringLexer(new StringBuffer(str));
        }

        private SpringLexer CreateIncrementalLexer(string str)
        {
            return new SpringIncrementalLexer(new StringBuffer(str));
        }

        [Test]
        public void TestParseSpeedChangeRandomLexeme()
        {
            PrintParseSpeed(s => new SpringParser(new SpringLexer(new StringBuffer(s))), "Incremental", oldLex =>
            {
                var chars = oldLex.Buffer.GetText().ToCharArray();
                var oldStr = new string(chars);
                var buffer = new TokenBuffer(oldLex);
                var tokenLength = buffer.CachedTokens.Count;
                var tokenToReplace = buffer.CachedTokens[_random.Next(tokenLength)];
                var tokenWhichReplaces = buffer.CachedTokens[_random.Next(tokenLength)];
                var sb = new StringBuilder(oldStr.Substring(0, tokenToReplace.Start));
                sb.Append(oldStr.Substring(tokenWhichReplaces.Start,
                    tokenWhichReplaces.End - tokenWhichReplaces.Start));
                sb.Append(oldStr.Substring(tokenToReplace.End));
                return CreateLexer(sb.ToString());
                // return CreateIncrementalLexer(sb.ToString());
            });
        }

        private void PrintParseSpeed(Func<string, IIncrementalParser> createParser, string parserType,
            Func<ILexer, ILexer> changeText = null)
        {
            var benchmark = new HashMap<string, TimeSpan>();
            var reparseBenchmark = changeText == null ? null : new HashMap<string, TimeSpan>();
            foreach (var filename in _filenames)
            {
                var content =
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
                var parser = createParser(content);
                benchmark[filename] = fetchTime(() => parser.ParseFile());
                Console.WriteLine($"{parserType}: file {filename}, - {benchmark[filename].ToFormatString()}");
                if (changeText != null)
                {
                    reparseBenchmark[filename] = fetchTime(() => parser.ReParse(changeText(parser.GetLexer())));
                    Console.WriteLine($"{parserType} reparse: file {filename}, - {reparseBenchmark[filename].ToFormatString()}");
                }
            }

            var fullTime = benchmark.Aggregate(new TimeSpan(), (acc, x) => acc.Add(x.Value));
            Console.WriteLine($"{parserType}: Project parse time is {fullTime}");
            if (changeText != null)
            {
                var reparseTime = reparseBenchmark.Aggregate(new TimeSpan(), (acc, x) => acc.Add(x.Value));
                Console.WriteLine($"{parserType} reparse: Project reparse time is {reparseTime}");
            }

            Console.WriteLine();
        }

        [Test]
        public void TestParseSpeedChangeSeveralChars()
        {
        }

        [Test]
        public void TestParseSpeedChangeSeveralChar()
        {
        }

        [Test]
        public void TestParseSpeedChangeLexem()
        {
        }

        [Test]
        public void TestParseRemoveLexem()
        {
        }

        [Test]
        public void TestParseAddLexem()
        {
        }


        private TimeSpan fetchTime(Action x)
        {
            var sw = new Stopwatch();
            sw.Start();
            x();
            sw.Stop();
            return sw.Elapsed;
        }

        private char GetRandomChar()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789;()_:";

            return chars[_random.Next(chars.Length)];
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
                
                sb.Append($"{timeSpan.Milliseconds} мс");
            }

            return sb.ToString();
        }
    }
}
