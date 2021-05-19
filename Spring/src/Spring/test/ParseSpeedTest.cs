using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            PrintParseSpeed(s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s))), "IncrementalParser", x => x);
        }

        [Test]
        public void TestParseSpeedChangeOneChar()
        {
            PrintParseSpeed(s => new SpringParser(new SpringLexer(new StringBuffer(s))), "Parser", oldLex =>
            {
                var str = oldLex.Buffer.GetText().ToCharArray();
                str[_random.Next(str.Length)] = getRandomChar();
                return new SpringLexer(new StringBuffer(str.ToString()));
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
                Console.WriteLine($"{parserType}: file {filename}, - {benchmark[filename]}");
                if (changeText != null)
                {
                    reparseBenchmark[filename] = fetchTime(() => parser.ReParse(changeText(parser.GetLexer())));
                    Console.WriteLine($"{parserType} reparse: file {filename}, - {reparseBenchmark[filename]}");
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

        private char getRandomChar()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789;()_:";

            return chars[_random.Next(chars.Length)];
        }

        private String changeRandomLexeme()
        {
            return null;
        }
    }
}
