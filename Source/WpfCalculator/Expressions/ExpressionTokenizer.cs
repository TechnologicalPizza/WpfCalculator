﻿using System;
using System.Collections.Generic;
using System.Globalization;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
{
    public partial class ExpressionTokenizer
    {
        private static TokenDefinition[] _tokenDefinitions;

        public const char SpaceChar = '_';
        public const char ListOpeningChar = '(';
        public const char ListSeparatorChar = ',';
        public const char ListClosingChar = ')';

        #region Static Constructor

        static ExpressionTokenizer()
        {
            static TokenDefinition NewDef(
                TokenType type, Func<char, bool> predicate, bool isSingular = false)
            {
                return new TokenDefinition(type, predicate, isSingular);
            }

            var decimalNumberDef = NewDef(
                TokenType.DecimalNumber,
                c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.OtherNumber);

            _tokenDefinitions = new[]
            {
                NewDef(TokenType.Operator, IsOperator, true),
                NewDef(TokenType.Name, IsNameToken),
                NewDef(TokenType.DecimalSeparator, c => c == '.', true),
                NewDef(TokenType.DecimalDigit, IsDigitToken, true),
                decimalNumberDef,
                NewDef(TokenType.WhiteSpace, char.IsWhiteSpace),
                NewDef(TokenType.Space, IsSpaceToken),
                NewDef(TokenType.ListOpening, c => c == ListOpeningChar, true),
                NewDef(TokenType.ListClosing, c => c == ListClosingChar, true),
                NewDef(TokenType.ListSeparator, c => c == ListSeparatorChar, true)
            };
        }

        public static bool IsDigitToken(char value)
        {
            return char.IsDigit(value);
        }

        public static bool IsNameToken(char value)
        {
            return char.IsLetter(value);
        }

        public static bool IsSpaceToken(char value)
        {
            return value == SpaceChar;
        }

        #endregion

        public static TokenDefinition GetDefinition(char c)
        {
            for (int j = 0; j < _tokenDefinitions.Length; j++)
            {
                var def = _tokenDefinitions[j];
                if (def.Predicate(c))
                    return def;
            }
            return null;
        }

        public static void Tokenize(
            ReadOnlyMemory<char> text, ICollection<Token> output)
        {
            var currentType = TokenType.Unknown;
            int lastOffset = 0;
            int offset = 0;

            void FinishToken()
            {
                int length = offset - lastOffset;
                if (length > 0)
                {
                    var slice = text.Slice(lastOffset, length);
                    output.Add(new ValueToken(currentType, slice));
                }
            }

            var span = text.Span;
            while (offset < text.Length)
            {
                char c = span[offset];
                TokenDefinition definition = GetDefinition(c);

                var nextType = definition == null ? TokenType.Unknown : definition.Type;
                bool isSingular = definition == null ? false : definition.IsSingular;
                if (nextType != currentType || isSingular)
                {
                    FinishToken();

                    lastOffset = offset;
                    currentType = nextType;
                }
                offset++;
            }

            FinishToken();
        }

        private static bool IsOperator(char c)
        {
            // TODO: check in ExpressionOptions.Default instead
            switch (c)
            {
                case '+':

                case '-':
                case '–':

                case '/':
                case ':':
                case '÷':

                case '%':

                case '*':
                case '×':

                case '^':

                case '!':
                    return true;

                default:
                    return false;
            }
        }
    }
}
