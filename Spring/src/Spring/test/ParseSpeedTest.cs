using System;
using System.Diagnostics;
using System.IO;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
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
        private ILexerFactory GetLexerFactory(bool incremental = false)
        {
            if (incremental)
            {
                return new SpringIncrementalLexerFactory();
            }

            return new SpringLanguageService.SpringLexerFactory();
        }

        private string[] filenames = new[]
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
        public void TestParseSpeed()
        {
            foreach (var filename in filenames)
            {
                var content =
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
                var parser = new SpringParser(new SpringLexer(new StringBuffer(content)));
                var incrementalParser = new SpringParser(new SpringIncrementalLexer(new StringBuffer(content)));
                Console.WriteLine($"file {filename}, usual parser - {fetchParseTime(parser)}");
                Console.WriteLine($"file {filename}, uncremental parser - {fetchParseTime(incrementalParser)}");
                Console.WriteLine();
            }
        }

        [Test]
        public void TestParseSpeedChangeOneChar()
        {
            foreach (var filename in filenames)
            {
                var content =
                    File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
                var parser = new SpringParser(new SpringLexer(new StringBuffer(content)));
                var incrementalParser = new SpringParser(new SpringIncrementalLexer(new StringBuffer(content)));
                Console.WriteLine($"file {filename}, usual parser - {fetchParseTime(parser)}");
                Console.WriteLine($"file {filename}, uncremental parser - {fetchParseTime(incrementalParser)}");
                Console.WriteLine();
            }
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


        private TimeSpan fetchParseTime(IParser parser)
        {
            var sw = new Stopwatch();
            sw.Start();
            var tree = parser.ParseFile();
            sw.Stop();
            return sw.Elapsed;
        }

        private char insertRandomChar()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789;()_:";
            var random = new Random();

            return chars[random.Next(chars.Length)];
        }

        private String changeRandomLexeme()
        {
            return null;
        }
    }
}
