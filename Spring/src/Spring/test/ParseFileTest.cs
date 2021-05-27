using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Spring.test
{
    [TestFixture]
    public class ParseFileTest
    {
        [TestCase("BanSystem.pas")]
        [TestCase("FileServer.pas")]
        [TestCase("LobbyClient.pas")]
        [TestCase("Main.pas")]
        [TestCase("Rcon.pas")]
        [TestCase("Server.pas")]
        [TestCase("ServerCommands.pas")]
        [TestCase("ServerHelper.pas")]
        [TestCase("ServerLoop.pas")]
        [Test]
        public void TestParse(string filename)
        {
            var content = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
            var parser = new SpringParser(new SpringLexer(new StringBuffer(content)));
            var tree = parser.ParseFile();
        }

        [TestCase("BanSystem.pas")]
        [TestCase("FileServer.pas")]
        [TestCase("LobbyClient.pas")]
        [TestCase("Main.pas")]
        [TestCase("Rcon.pas")]
        [TestCase("Server.pas")]
        [TestCase("ServerCommands.pas")]
        [TestCase("ServerHelper.pas")]
        [TestCase("ServerLoop.pas")]
        [Test]
        public void TestTokenRanges(string filename)
        {
            var content = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "../../../test/files", filename));
            var lexer = new SpringLexer(new StringBuffer(content));
            var lexems = new List<SpringToken>();
            lexer.Start();
            var lastTokenEnd = lexer.TokenEnd;
            lexer.TokenStart.Should().Be(0);
            while (lexer.TokenType != null)
            {
                lexems.Add(lexer.CurToken);
                lexer.Advance();
                if (lastTokenEnd != lexer.TokenStart)
                {
                    continue;
                }

                lexer.TokenStart.Should().Be(lastTokenEnd, $"mismatched on {lexer.CurrentPosition} position");
                lastTokenEnd = lexer.TokenEnd;
            }

            lastTokenEnd.Should().Be(content.Length);
        }
    }
}
