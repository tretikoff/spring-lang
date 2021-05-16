using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.TreeBuilder;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Spring
{
    public sealed class TreeBuilder
    {
        private int _myLexemeCount;
        private readonly ILexer _myLexer;
        public readonly List<Marker> MyProduction;
        private readonly CompositeNodeType _myRootType;
        private readonly IPsiBuilderTokenFactory _myTokenFactory;
        private readonly TokenBuffer MyTokenBuffer;
        private readonly IArrayOfTokens MyArrayOfTokens;
        private int _myCurrentLexeme;
        private TokenNodeType _myCurrentTokenType;
        private int _myNonCommentLexeme;
        private readonly bool _myIsLazyCachingLexer;

        public TreeBuilder(
            ILexer lexer,
            CompositeNodeType rootType,
            IPsiBuilderTokenFactory tokenFactory,
            Lifetime lifetime)
            : this(lexer, rootType, 0, -1, tokenFactory, lifetime)
        {
        }

        private TreeBuilder(
            ILexer lexer,
            CompositeNodeType rootType,
            int start,
            int stop,
            IPsiBuilderTokenFactory tokenFactory,
            Lifetime lifetime)
        {
            _myLexer = lexer;
            _myRootType = rootType;
            _myTokenFactory = tokenFactory;
            _myCurrentLexeme = _myNonCommentLexeme = start;
            switch (lexer)
            {
                case CachingLexer cachingLexer:
                    MyTokenBuffer = cachingLexer.TokenBuffer;
                    break;
                case ILazyCachingLexer lazyCachingLexer:
                    MyTokenBuffer = lazyCachingLexer.TokenBuffer;
                    if (stop == -1)
                    {
                        _myIsLazyCachingLexer = true;
                    }

                    break;
                default:
                    MyTokenBuffer = new TokenBuffer(lexer);
                    break;
            }

            MyArrayOfTokens = MyTokenBuffer.CachedTokens;
            if (MyArrayOfTokens.Count != 0)
                Assertion.Assert(MyArrayOfTokens[0].Start == 0, "offset of first token is not a zero");
            if (stop == -1)
            {
                _myLexemeCount = MyArrayOfTokens.Count;
            }
            else
            {
                Assertion.Assert(stop <= MyArrayOfTokens.Count, "stop is greater than myArrayOfTokens.Count");
                _myLexemeCount = stop;
            }

            MyProduction = new List<Marker>();
            _myCurrentTokenType = _myCurrentLexeme < _myLexemeCount ? MyArrayOfTokens[_myCurrentLexeme].Type : null;
            CheckInvariant();
        }

        private void Bind(int pRootMarker, CompositeElement rootNode)
        {
            var interruptChecker = new SeldomInterruptCheckerWithCheckTime(100);
            var curNode = rootNode;
            var marker1 = MyProduction[pRootMarker];
            var lexemeIndex = marker1.LexemeIndex;
            var index1 = marker1.FirstChild != -1 ? marker1.FirstChild : marker1.OppositeMarker;
            while (true)
            {
                var marker2 = MyProduction[index1];
                InsertLeaves(ref lexemeIndex, marker2.LexemeIndex, curNode, interruptChecker);
                if (index1 != marker1.OppositeMarker)
                {
                    var index2 = marker2.OppositeMarker + index1;
                    var marker3 = MyProduction[index2];
                    if (marker2.Type == MarkerType.StartMarkerType)
                    {
                        if (marker3.Type == MarkerType.DoneMarkerType)
                        {
                            var compositeElement = CreateCompositeElement(marker2);
                            curNode.AddChild(compositeElement);
                            curNode = compositeElement;
                        }
                        else
                        {
                            if (marker3.UserData is Token userData)
                                AlterLeaves(ref lexemeIndex, marker3.LexemeIndex, curNode, userData);
                            if (marker3.UserData is Token[] userDatas)
                                AlterLeaves(ref lexemeIndex, marker3.LexemeIndex, curNode, userDatas);
                        }

                        index1 = marker2.FirstChild != -1 ? marker2.FirstChild : index2;
                    }
                    else
                    {
                        if (marker2.Type == MarkerType.DoneMarkerType)
                        {
                            curNode = (CompositeElement) curNode.Parent;
                            Assertion.Assert(curNode != null, "curNode != null");
                        }

                        var parentMarker = marker3.ParentMarker;
                        index1 = marker3.NextMarker != -1
                            ? marker3.NextMarker
                            : MyProduction[parentMarker].OppositeMarker + parentMarker;
                    }
                }
                else
                    break;
            }
        }

        private static CompositeElement CreateCompositeElement(Marker item)
        {
            if (item.ElementType is CompositeNodeWithArgumentType elementType)
                return elementType.Create(item.UserData);
            if (!(item.ElementType is CompositeNodeType elemT))
                Assertion.Fail("Composite element type is expected but found: '{0}'", item.ElementType);
            else
                return elemT.Create();
            return null;
        }

        private void InsertLeaves(
            ref int curToken,
            int lastIdx,
            CompositeElement curNode,
            SeldomInterruptCheckerWithCheckTime interruptChecker)
        {
            lastIdx = Math.Min(lastIdx, _myLexemeCount);
            while (curToken < lastIdx)
            {
                interruptChecker.CheckForInterrupt();
                var arrayOfToken = MyArrayOfTokens[curToken];
                curNode.AddChild(CreateToken(arrayOfToken.Type, _myLexer.Buffer, arrayOfToken.Start, arrayOfToken.End));
                ++curToken;
            }
        }

        private void AlterLeaves(ref int curToken, int lastIdx, CompositeElement curNode, Token token)
        {
            curNode.AddChild(CreateToken(token.Type, _myLexer.Buffer, token.Start, token.End));
            curToken = Math.Min(lastIdx, _myLexemeCount);
        }

        private void AlterLeaves(
            ref int curToken,
            int lastIdx,
            CompositeElement curNode,
            Token[] tokens)
        {
            foreach (var token in tokens)
                curNode.AddChild(CreateToken(token.Type, _myLexer.Buffer, token.Start, token.End));
            curToken = Math.Min(lastIdx, _myLexemeCount);
        }

        public void ReScan(
            TextRange oldRange,
            ILexerFactory lexerFactory,
            BufferRange newBufferRange)
        {
            MyTokenBuffer.ReScan(oldRange, lexerFactory, newBufferRange);
        }

        private LeafElementBase CreateToken(
            TokenNodeType tokenNodeType,
            IBuffer buffer,
            int startOffset,
            int endOffset)
        {
            return _myTokenFactory == null
                ? tokenNodeType.Create(buffer, new TreeOffset(startOffset), new TreeOffset(endOffset))
                : _myTokenFactory.CreateToken(tokenNodeType, buffer, startOffset, endOffset);
        }

        private void UpdateMyCurrentTokenType() => _myCurrentTokenType =
            _myCurrentLexeme != _myLexemeCount ? MyArrayOfTokens.GetTokenType(_myCurrentLexeme) : null;

        public void PrepareLightTree()
        {
            if (MyProduction.Count == 0)
                return;
            var checkerWithCheckTime = new SeldomInterruptCheckerWithCheckTime(200);
            var node = 0;
            var intStack = new Stack<int>();
            intStack.Push(node);
            var num1 = 0;
            var num2 = 0;
            for (var index = 1; index < MyProduction.Count; ++index)
            {
                checkerWithCheckTime.CheckForInterrupt();
                var marker = MyProduction[index];
                if (node == -1)
                    Logger.LogError("Unexpected end of the production");
                marker.ParentMarker = node;
                switch (marker.Type)
                {
                    case MarkerType.StartMarkerType:
                        marker.FirstChild = marker.LastChild = marker.NextMarker = -1;
                        Marker.AddChild(this, node, index);
                        intStack.Push(node);
                        node = index;
                        ++num2;
                        if (num2 > num1)
                        {
                            num1 = num2;
                        }

                        break;
                    case MarkerType.DoneMarkerType:
                    case MarkerType.DoneAlterTokenMarkerType:
                        if (marker.OppositeMarker + index != node)
                            Logger.LogError("Unbalanced tree");
                        node = intStack.Pop();
                        --num2;
                        break;
                }

                MyProduction[index] = marker;
            }

            if (node == 0)
                return;
            Assertion.Fail("Unbalanced tree");
        }

        public CompositeElement BuildTree()
        {
            PrepareLightTree();
            CompositeNodeType compositeNodeType = null;
            if (MyProduction.Count != 0)
                compositeNodeType = MyProduction[0].ElementType as CompositeNodeType;
            var rootNode = compositeNodeType != null
                ? CreateCompositeElement(MyProduction[0])
                : _myRootType.Create();
            if (MyProduction.Count != 0)
                Bind(0, rootNode);
            return rootNode;
        }

        public int Mark()
        {
            CheckInvariant();
            var marker = new Marker(MarkerType.StartMarkerType, _myCurrentLexeme);
            _myNonCommentLexeme = _myCurrentLexeme;
            MyProduction.Add(marker);
            CheckInvariant();
            return MyProduction.Count - 1;
        }

        public void Error(int marker, string message) => Done(marker, PsiBuilderErrorElement.NODE_TYPE, message);
        public bool Eof() => this._myCurrentLexeme >= this._myLexemeCount;

        public void Error(string message) => Error(Mark(), message);

        public void Done(int marker, [NotNull] NodeType type, object userData)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            DoneImpl(marker, type, MarkerType.DoneMarkerType, userData);
        }

        private void DoneImpl(int marker, [CanBeNull] NodeType type, MarkerType doneMarkerType, object userData)
        {
            if (marker >= MyProduction.Count)
                Assertion.Fail("marker is past the end of sequence " + marker + " of " + MyProduction.Count);
            CheckInvariant();
            DoValidityChecks(marker + 1, MyProduction.Count);
            var marker1 = new Marker(doneMarkerType, _myCurrentLexeme) {ElementType = type, UserData = userData};
            var marker2 = MyProduction[marker];
            if (marker2.OppositeMarker != -1)
                Logger.LogError("Marker already done");
            marker2.OppositeMarker = MyProduction.Count - marker;
            if (marker2.LexemeIndex > _myCurrentLexeme)
                marker1.LexemeIndex = marker2.LexemeIndex;
            marker2.ElementType = type;
            marker2.UserData = userData;
            MyProduction[marker] = marker2;
            marker1.OppositeMarker = -marker2.OppositeMarker;
            MyProduction.Add(marker1);
            _myNonCommentLexeme = _myCurrentLexeme;
            CheckInvariant();
        }

        public TokenNodeType AdvanceLexer()
        {
            Assertion.Assert(_myCurrentLexeme != _myLexemeCount, "cannot step past the end");
            CheckInvariant();
            var currentTokenType = _myCurrentTokenType;
            ++_myCurrentLexeme;
            if (_myIsLazyCachingLexer && _myCurrentLexeme == _myLexemeCount)
            {
                while (_myLexer.TokenType != null && _myLexemeCount == MyArrayOfTokens.Count)
                    _myLexer.Advance();
                _myLexemeCount = MyArrayOfTokens.Count;
            }

            UpdateMyCurrentTokenType();
            if (!currentTokenType.IsComment && !currentTokenType.IsWhitespace)
                _myNonCommentLexeme = _myCurrentLexeme;
            CheckInvariant();
            return currentTokenType;
        }

        public TokenNodeType GetTokenType() => _myCurrentTokenType;


        public Token GetToken() => MyArrayOfTokens[_myCurrentLexeme];

        [Conditional("JET_MODE_ASSERT")]
        private void CheckInvariant(bool @do = false)
        {
            if (!@do)
                return;
            Assertion.Assert(_myNonCommentLexeme <= _myCurrentLexeme,
                "non comment lexeme must be before current lexeme");
            var num = 0;
            if (MyProduction.Count != 0)
                num = MyProduction[MyProduction.Count - 1].LexemeIndex;
            Assertion.Assert(
                _myNonCommentLexeme == num || !MyArrayOfTokens[_myNonCommentLexeme - 1].Type.IsComment &&
                !MyArrayOfTokens[_myNonCommentLexeme - 1].Type.IsWhitespace, "wrong non comment lexeme");
        }

        [Conditional("JET_MODE_ASSERT")]
        private void DoValidityChecks(int first, int last)
        {
        }
    }
}
