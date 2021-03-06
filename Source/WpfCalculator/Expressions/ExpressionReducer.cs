﻿using System;
using System.Collections.Generic;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
{
    public static class ExpressionReducer
    {
        public enum ResultCode
        {
            Ok
        }

        public static ResultCode Reduce(ExpressionOptions options, List<Token> tokens)
        {
            // TODO: the reducer can currently break expressions pretty hard, so fix it
            throw new NotImplementedException();

            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ResultCode code;
            if ((code = ReduceLists(options, tokens)) != ResultCode.Ok)
                return code;
            return code;
        }

        public static ResultCode Reduce(ExpressionTree tree)
        {
            return Reduce(tree.Options, tree.Tokens);
        }

        private static ResultCode ReduceLists(ExpressionOptions options, List<Token> tokens)
        {
            var listStack = new Stack<List<Token>>();
            listStack.Push(tokens);

            while (listStack.Count > 0)
            {
                var currentTokens = listStack.Pop();
                for (int i = 0; i < currentTokens.Count; i++)
                {
                    var token = currentTokens[i];
                    if (token.Type == TokenType.List)
                    {
                        var listToken = (ListToken)token;
                        if (listToken.Count == 1)
                        {
                            var listChild = listToken[0];
                            currentTokens[i] = listChild;
                            i--;
                        }
                        listStack.Push(listToken.Children);
                    }
                }
            }
            return ResultCode.Ok;
        }
    }
}
