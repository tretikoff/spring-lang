using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Util.Intern;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.Parsing
{
  public class TokenBuffer
  {
    private readonly IBuffer myBuffer;
    private readonly IArrayOfTokens myCachedTokens;

    public TextRange LastResyncAffectedRange { get; private set; }

    public TokenBuffer(ILexer lexer)
    {
      this.LastResyncAffectedRange = TextRange.InvalidRange;
      this.myCachedTokens = (IArrayOfTokens) new PackedArrayOfTokens();
      this.myBuffer = lexer.Buffer;
      lexer.Start();
      this.ReScanUpToEnd(lexer);
    }

    public TokenBuffer(IArrayOfTokens cachedTokens, IBuffer buffer)
    {
      this.LastResyncAffectedRange = TextRange.InvalidRange;
      this.myCachedTokens = cachedTokens;
      this.myBuffer = buffer;
    }

    public Token this[int index] => this.CachedTokens[index];

    private void ReScanUpToEnd(ILexer lexer)
    {
      this.CachedTokens.Clear();
      SeldomInterruptChecker interruptChecker = new SeldomInterruptChecker();
      IIncrementalLexer incrementalLexer = lexer as IIncrementalLexer;
      TokenNodeType tokenType;
      while ((tokenType = lexer.TokenType) != null)
      {
        interruptChecker.CheckForInterrupt();
        this.CachedTokens.Add(new Token(tokenType, lexer.TokenStart, lexer.TokenEnd, incrementalLexer == null ? 0U : incrementalLexer.LexerStateEx));
        lexer.Advance();
      }
    }

    /// <summary>
    /// Rescans the buffer incrementally and returns new instance of token buffer
    /// </summary>
    public TokenBuffer ReScan(
      TextRange oldRange,
      ILexerFactory lexerFactory,
      BufferRange newBufferRange)
    {
      int num = this.CachedTokens.Count == 0 ? 0 : this.CachedTokens.Last().End;
      Assertion.Assert(oldRange.StartOffset >= 0, "changedRange.StartOffset >= 0");
      Assertion.Assert(oldRange.EndOffset >= oldRange.StartOffset, "changedRange.EndOffset >= changedRange.StartOffset");
      Assertion.Assert(oldRange.EndOffset <= num, "changedRange.EndOffset <= oldBufferLength");
      IBuffer buffer = newBufferRange.Buffer;
      ILexer lexer1 = lexerFactory.CreateLexer(buffer);
      TokenBuffer tokenBuffer = lexer1 is IIncrementalLexer lexer2 ? this.ReScanInternalIncremental(oldRange, lexer2) : this.ReScanInternalFull(oldRange, lexer1);
      // Assertion.Assert(tokenBuffer.Buffer.Length == (tokenBuffer.CachedTokens.Count > 0 ? tokenBuffer.CachedTokens.Last().End : 0), "Token buffer doesn't match buffer text");
      return tokenBuffer;
    }

    private TokenBuffer ReScanInternalIncremental(
      TextRange changedRange,
      IIncrementalLexer lexer)
    {
      int startOffset = changedRange.StartOffset;
      int endOffset = changedRange.EndOffset;
      int num1 = this.CachedTokens.FindTokenAt(startOffset);
      if (num1 < 0)
        num1 = this.CachedTokens.Count - 1;
      if (num1 <= lexer.LexemIndent)
        return new TokenBuffer((ILexer) lexer)
        {
          LastResyncAffectedRange = TextRange.FromLength(lexer.Buffer.Length)
        };
      int num2 = num1 - lexer.LexemIndent;
      PackedArrayOfTokens packedArrayOfTokens = new PackedArrayOfTokens(num2);
      packedArrayOfTokens.AddRange(this.CachedTokens, 0, num2, 0);
      int num3 = num2 > 0 ? this.CachedTokens.GetTokenEnd(num2 - 1) : 0;
      uint tokenState = this.CachedTokens.GetTokenState(num2 - 1);
      if (tokenState == uint.MaxValue)
        return new TokenBuffer((ILexer) lexer)
        {
          LastResyncAffectedRange = TextRange.FromLength(lexer.Buffer.Length)
        };
      lexer.Start(num3, lexer.Buffer.Length, tokenState);
      int tokenAt = this.CachedTokens.FindTokenAt(endOffset);
      return this.ScanModifiedRangeAndTryMerge((ILexer) lexer, (IArrayOfTokens) packedArrayOfTokens, num3, endOffset, tokenAt);
    }

    public TokenBuffer ScanModifiedRangeAndTryMerge(
      ILexer newLexer,
      IArrayOfTokens newArrayOfTokens,
      int affectedRangeStart,
      int modifiedRangeEnd,
      int syncLexerPosition)
    {
      IBuffer buffer = newLexer.Buffer;
      TextRange textRange = TextRange.InvalidRange;
      SeldomInterruptChecker interruptChecker = new SeldomInterruptChecker();
      while (newLexer.TokenType != null)
      {
        interruptChecker.CheckForInterrupt();
        newArrayOfTokens.Add(new Token(newLexer.TokenType, newLexer.TokenStart, newLexer.TokenEnd, newLexer is ILexerEx ? ((ILexerEx) newLexer).LexerStateEx : 0U));
        if (syncLexerPosition >= 0 && syncLexerPosition < this.CachedTokens.Count)
        {
          Token cachedToken1 = this.CachedTokens[syncLexerPosition];
          int num;
          for (num = buffer.Length - newLexer.TokenStart; this.myBuffer.Length - cachedToken1.Start > num; cachedToken1 = this.CachedTokens[syncLexerPosition])
          {
            ++syncLexerPosition;
            if (syncLexerPosition >= this.CachedTokens.Count)
              goto label_16;
          }
          if (cachedToken1.Start > modifiedRangeEnd && this.myBuffer.Length - cachedToken1.Start == num && (newLexer.TokenType == cachedToken1.Type && newLexer.TokenEnd - newLexer.TokenStart == cachedToken1.End - cachedToken1.Start) && (!(newLexer is ILexerEx) || (int) ((ILexerEx) newLexer).LexerStateEx == (int) cachedToken1.LexerState))
          {
            int delta = buffer.Length - this.myBuffer.Length;
            int length = this.CachedTokens.Count - (syncLexerPosition + 1);
            int index = syncLexerPosition;
            if (length > 0)
            {
              if (!(newLexer is IIncrementalLexer))
              {
                for (; newLexer.TokenType != null && syncLexerPosition < this.CachedTokens.Count; ++syncLexerPosition)
                {
                  interruptChecker.CheckForInterrupt();
                  Token cachedToken2 = this.CachedTokens[syncLexerPosition];
                  if (newLexer.TokenType == cachedToken2.Type && newLexer.TokenEnd - newLexer.TokenStart == cachedToken2.End - cachedToken2.Start && (!(newLexer is ILexerEx) || (int) ((ILexerEx) newLexer).LexerStateEx == (int) cachedToken2.LexerState))
                    newLexer.Advance();
                  else
                    break;
                }
                if (newLexer.TokenType != null)
                {
                  newArrayOfTokens.AddRange(this.CachedTokens, index + 1, syncLexerPosition - index - 1, delta);
                  newArrayOfTokens.Add(new Token(newLexer.TokenType, newLexer.TokenStart, newLexer.TokenEnd, newLexer is ILexerEx ? ((ILexerEx) newLexer).LexerStateEx : 0U));
                  ++syncLexerPosition;
                  goto label_16;
                }
              }
              newArrayOfTokens.AddRange(this.CachedTokens, index + 1, length, delta);
            }
            textRange = new TextRange(affectedRangeStart, this.CachedTokens.GetTokenStart(index) + delta);
            break;
          }
        }
label_16:
        newLexer.Advance();
      }
      if (!textRange.IsValid)
        textRange = new TextRange(affectedRangeStart, buffer.Length);
      return new TokenBuffer(newArrayOfTokens, buffer)
      {
        LastResyncAffectedRange = textRange
      };
    }

    private TokenBuffer ReScanInternalFull(TextRange changedRange, ILexer lexer)
    {
      int startOffset = changedRange.StartOffset;
      int endOffset = changedRange.EndOffset;
      SeldomInterruptChecker interruptChecker = new SeldomInterruptChecker();
      int num = 0;
      int affectedRangeStart = 0;
      lexer.Start();
      for (; lexer.TokenType != null && num < this.CachedTokens.Count; ++num)
      {
        interruptChecker.CheckForInterrupt();
        Token cachedToken = this.CachedTokens[num];
        if (lexer.TokenType == cachedToken.Type && lexer.TokenStart == cachedToken.Start && (lexer.TokenEnd == cachedToken.End && lexer.TokenEnd < startOffset))
        {
          affectedRangeStart = cachedToken.End;
          lexer.Advance();
        }
        else
          break;
      }
      PackedArrayOfTokens packedArrayOfTokens = new PackedArrayOfTokens(num);
      if (num > 0)
        packedArrayOfTokens.AddRange(this.CachedTokens, 0, num, 0);
      return this.ScanModifiedRangeAndTryMerge(lexer, (IArrayOfTokens) packedArrayOfTokens, affectedRangeStart, endOffset, num);
    }

    public IBuffer Buffer => this.myBuffer;

    public IArrayOfTokens CachedTokens => this.myCachedTokens;


    public int FindTokenAt(int offset) => this.CachedTokens.FindTokenAt(offset);
  }
}
