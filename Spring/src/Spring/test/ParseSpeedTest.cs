using System;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Collections;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Spring.test
{
    public class ParseSpeedTest : ParseSpeedTestBase
    {
        [Test]
        public void FilesBench()
        {
            var chars = 0;
            var lexemes = 0;
            var strings = 0;
            foreach (var filename in Filenames)
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
            PrintParseSpeed(ParserType.Regular);
            // PrintParseSpeed((s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s)))), "Parser with incremental lexer");
        }

        [Test]
        public void TestParseSpeed()
        {
            PrintParseSpeed(ParserType.Regular);
            // PrintParseSpeed((s => new SpringParser(new SpringIncrementalLexer(new StringBuffer(s)))), ParserType.Incremental);
        }

        [Test]
        public void TestReparseNoChange()
        {
            // PrintParseSpeed(s => new SpringParser(new SpringLexer(new StringBuffer(s))), "Parser", x => x);
            // PrintParseSpeed(ParserType.Incremental,
            //     x => x);
        }

        // [Test]
        // public void TestParseSpeedChangeRandomChar()
        // {
        //     PrintParseSpeed(ParserType.Incremental,
        //         // PrintParseSpeed( ParserType.Regular,
        //         oldLex =>
        //         {
        //             var chars = oldLex.Buffer.GetText().ToCharArray();
        //             chars[Random.Next(chars.Length)] = GetRandomChar();
        //             return CreateIncrementalLexer(new string(chars));
        //         });
        // }

        private SpringLexer CreateLexer(string str)
        {
            return new SpringLexer(new StringBuffer(str));
        }

        // [Test]
        // public void TestParseSpeedChangeRandomLexeme()
        // {
        //     PrintParseSpeed(ParserType.Incremental, parser =>
        //     {
        //         var chars = oldLex.Buffer.GetText().ToCharArray();
        //         var oldStr = new string(chars);
        //         var buffer = new TokenBuffer(oldLex);
        //         var tokenLength = buffer.CachedTokens.Count;
        //         var tokenToReplace = buffer.CachedTokens[Random.Next(tokenLength)];
        //         var tokenWhichReplaces = buffer.CachedTokens[Random.Next(tokenLength)];
        //         var sb = new StringBuilder(oldStr.Substring(0, tokenToReplace.Start));
        //         sb.Append(oldStr.Substring(tokenWhichReplaces.Start,
        //             tokenWhichReplaces.End - tokenWhichReplaces.Start));
        //         sb.Append(oldStr.Substring(tokenToReplace.End));
        //         return CreateLexer(sb.ToString());
        //         // return CreateIncrementalLexer(sb.ToString());
        //     });
        // }

        [Test]
        [TestCase(ParserType.Incremental)]
        [TestCase(ParserType.Regular)]
        public void TestRelexSpeedChangeFirstBegin(ParserType type)
        {
            PrintLexerSpeed(type, parser =>
            {
                var chars = parser.Lexer.Buffer.GetText().ToCharArray();
                var buffer = new TokenBuffer(parser.Lexer);
                var start = 0;
                var end = 0;
                for (var i = 0; i < buffer.CachedTokens.Count; i++)
                {
                    if (buffer[i].Type != SpringTokenType.Begin) continue;
                    var j = start = buffer[i].Start;
                    end = buffer[i].End;
                    for (; j < end; j++)
                    {
                        chars[j] = 'X';
                    }

                    break;
                }

                var range = new TextRange(start, end);
                parser.builder.ReScan(range,
                    type == ParserType.Incremental ? new IncrementalLexerFactory() : new LexerFactory(),
                    new BufferRange(parser.Lexer.Buffer, new TextRange(0, parser.Lexer.Buffer.Length)));
            });
        }

        [Test]
        [TestCase(ParserType.Incremental)]
        [TestCase(ParserType.Regular)]
        public void TestChangeFirstBegin(ParserType type)
        {
            PrintParseSpeed(type, parser =>
            {
                var chars = parser.Lexer.Buffer.GetText().ToCharArray();
                var buffer = new TokenBuffer(parser.Lexer);
                var start = 0;
                var end = 0;
                for (var i = 0; i < buffer.CachedTokens.Count; i++)
                {
                    if (buffer[i].Type != SpringTokenType.Begin) continue;
                    var j = start = buffer[i].Start;
                    end = buffer[i].End;
                    for (; j < end; j++)
                    {
                        chars[j] = 'X';
                    }

                    break;
                }

                var range = new TextRange(start, end);
                parser.builder.ReScan(range,
                    type == ParserType.Incremental ? new IncrementalLexerFactory() : new LexerFactory(),
                    new BufferRange(parser.Lexer.Buffer, new TextRange(0, parser.Lexer.Buffer.Length)));
            });
        }

        [Test]
        [TestCase(ParserType.Regular)]
        [TestCase(ParserType.Incremental)]
        public void TestChangeLastEnd(ParserType type)
        {
            PrintParseSpeed(type, parser =>
            {
                var chars = parser.Lexer.Buffer.GetText().ToCharArray();
                var buffer = new TokenBuffer(parser.Lexer);
                var start = 0;
                var end = 0;
                for (var i = buffer.CachedTokens.Count - 1; i >= 0; i--)
                {
                    if (buffer[i].Type != SpringTokenType.End) continue;
                    var j = start = buffer[i].Start;
                    end = buffer[i].End;
                    for (; j < end; j++)
                    {
                        chars[j] = 'X';
                    }

                    break;
                }

                var range = new TextRange(start, end);
                parser.builder.ReScan(range,
                    type == ParserType.Incremental ? new IncrementalLexerFactory() : new LexerFactory(),
                    new BufferRange(parser.Lexer.Buffer, new TextRange(0, parser.Lexer.Buffer.Length)));
            });
        }
        
        [Test]
                [TestCase(ParserType.Regular)]
                [TestCase(ParserType.Incremental)]
                public void TestRelexChangeLastEnd(ParserType type)
                {
                    PrintLexerSpeed(type, parser =>
                    {
                        var chars = parser.Lexer.Buffer.GetText().ToCharArray();
                        var buffer = new TokenBuffer(parser.Lexer);
                        var start = 0;
                        var end = 0;
                        for (var i = buffer.CachedTokens.Count - 1; i >= 0; i--)
                        {
                            if (buffer[i].Type != SpringTokenType.End) continue;
                            var j = start = buffer[i].Start;
                            end = buffer[i].End;
                            for (; j < end; j++)
                            {
                                chars[j] = 'X';
                            }
        
                            break;
                        }
        
                        var range = new TextRange(start, end);
                        parser.builder.ReScan(range,
                            type == ParserType.Incremental ? new IncrementalLexerFactory() : new LexerFactory(),
                            new BufferRange(parser.Lexer.Buffer, new TextRange(0, parser.Lexer.Buffer.Length)));
                    });
                }
    }
}
