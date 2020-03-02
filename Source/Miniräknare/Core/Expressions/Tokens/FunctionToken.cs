﻿using System;
using System.Text;

namespace Miniräknare.Expressions.Tokens
{
    public class FunctionToken : CollectionToken
    {
        public ValueToken Name { get; }
        public ListToken ArgumentList { get; }

        public int ArgumentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < ArgumentList.Count; i++)
                {
                    if (ArgumentList[i].Type == TokenType.ListSeparator)
                        continue;
                    count++;
                }
                return count;
            }
        }

        public FunctionToken(ListToken parent, ValueToken name, ListToken arguments) 
            : base(parent, TokenType.Function, arguments.Children)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (name.Type != TokenType.Name)
                throw new ArgumentException("Invalid token type.", nameof(name));

            ArgumentList = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        protected override StringBuilder ToStringCore()
        {
            var paramsString = base.ToStringCore();
            paramsString.Insert(0, Name);
            return paramsString;
        }
    }
}
