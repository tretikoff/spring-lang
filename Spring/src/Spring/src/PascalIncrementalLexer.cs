using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.Spring
{
    public class SpringIncrementalLexer : SpringLexer, IIncrementalLexer
    {
        public SpringIncrementalLexer(IBuffer buffer) : base(buffer)
        {
        }
        public uint LexerStateEx => (uint)TokenStart;
        public void Start(int startOffset, int endOffset, uint state)
        {
            _currentPosition = (int)state;
            Advance();
        }

        public int EOFPos => Buffer.Length;
        public int LexemIndent => 1;

    }
}
