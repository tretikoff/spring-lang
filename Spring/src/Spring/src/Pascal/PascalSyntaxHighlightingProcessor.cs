using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Host.Features.SyntaxHighlighting;
using JetBrains.ReSharper.Plugins.Pascal;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Spring.Pascal
{
    [Language(typeof(PascalLanguage))]
    internal class PascalSyntaxHighlightingManager : RiderSyntaxHighlightingManager
    {
        public override SyntaxHighlightingProcessor CreateProcessor()
        {
            return new PascalSyntaxHighlightingProcessor();
        }
    }

    
    class PascalSyntaxHighlightingProcessor : SyntaxHighlightingProcessor
    {
        protected override bool IsBlockComment(TokenNodeType tokenType)
        {
            return tokenType.IsComment;
        }

        protected override bool IsString(TokenNodeType tokenType)
        {
            return tokenType.IsStringLiteral;
        }

        protected override bool IsNumber(TokenNodeType tokenType)
        {
            return tokenType.IsConstantLiteral;
        }

        protected override bool IsKeyword(TokenNodeType tokenType)
        {
            return base.IsKeyword(tokenType) || tokenType.IsKeyword;
        }
    }
}
