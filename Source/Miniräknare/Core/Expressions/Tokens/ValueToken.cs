﻿using System;

namespace Miniräknare.Expressions.Tokens
{
    public class ValueToken : Token
    {
        public ReadOnlyMemory<char> Value { get; }

        internal override string DebuggerDisplay => base.DebuggerDisplay + ": \"" + ToString() + "\"";

        public ValueToken(TokenType type, ReadOnlyMemory<char> value) : base(type)
        {
            Value = value;
        }

        public ValueToken(TokenType type, ReadOnlyString value) : this(type, value.Chars)
        {
        }

        public bool ConsistsOfDigits()
        {
            var span = Value.Span;
            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];
                if (!ExpressionTokenizer.IsDigitToken(c))
                    return false;
            }
            return true;
        }

        public override Token Clone()
        {
            return new ValueToken(TokenType.Name, Value);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
