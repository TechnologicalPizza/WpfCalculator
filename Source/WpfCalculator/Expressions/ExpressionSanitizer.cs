﻿using System;
using System.Collections.Generic;
using System.Text;
using WpfCalculator.Expressions.Tokens;

namespace WpfCalculator.Expressions
{
    public partial class ExpressionSanitizer
    {
        private static TokenType[] DecimalSeparatorTypes =
            new[] { TokenType.DecimalSeparator };

        private static TokenType[] DecimalNumberComponents =
            new[] { TokenType.DecimalDigit, TokenType.DecimalSeparator };

        public const char DecimalSeparator = '.';

        public enum ResultCode
        {
            Ok = 0,
            WhiteSpaceInName,
            RepeatingListSeparators,
            RepeatingDecimalSeparators,
            RepeatingDecimalNumbers,
            DecimalDigitInNumber,
            DecimalNumberInDigit,
            TrailingDecimalSeparator,
            InvalidDecimalSeparator,
            MultipleDecimalSeparators,
            UnexpectedListSeparator
        }

        public readonly struct SanitizeResult
        {
            public static readonly SanitizeResult Ok = new SanitizeResult(ResultCode.Ok, null);

            public ResultCode Code { get; }
            public Token ErrorToken { get; }

            public SanitizeResult(ResultCode code, Token errorToken)
            {
                Code = code;
                ErrorToken = errorToken;
            }
        }

        public static SanitizeResult Sanitize(List<Token> tokens)
        {
            var builder = new StringBuilder();

            SanitizeResult result;
            if ((result = RemoveWhiteSpaces(tokens)).Code != ResultCode.Ok ||
                (result = ValidateTypesInGroups(builder, tokens)).Code != ResultCode.Ok ||
                (result = MergeGroupsOfSingles(builder, tokens)).Code != ResultCode.Ok ||
                (result = MergeGroupsOfMultiples(builder, tokens)).Code != ResultCode.Ok)
                return result;
            return result;
        }

        public static SanitizeResult Sanitize(ExpressionTree tree)
        {
            return Sanitize(tree.Tokens);
        }

        #region RemoveWhiteSpaces

        private static SanitizeResult RemoveWhiteSpaces(IList<Token> tokens)
        {
            for (int i = tokens.Count; i-- > 0;)
            {
                var token = tokens[i];
                if (token.Type == TokenType.WhiteSpace)
                    tokens.RemoveAt(i);
            }
            return SanitizeResult.Ok;
        }

        #endregion

        #region ValidateTypesInGroups

        private static SanitizeResult ValidateTypesInGroups(StringBuilder builder, List<Token> tokens)
        {
            Token lastToken = null;

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                #region IsMiddleType

                bool IsMiddleType(
                    TokenType groupType,
                    TokenType middleType)
                {
                    if (lastToken != null &&
                        lastToken.Type != token.Type &&
                        lastToken.Type == groupType)
                    {
                        if (token.Type == middleType)
                        {
                            if (GetNextToken(tokens, i)?.Type == groupType)
                                return true;
                        }
                    }
                    return false;
                }

                #endregion

                if (IsMiddleType(TokenType.DecimalDigit, TokenType.DecimalNumber))
                    return new SanitizeResult(ResultCode.DecimalDigitInNumber, token);

                if (IsMiddleType(TokenType.DecimalNumber, TokenType.DecimalDigit))
                    return new SanitizeResult(ResultCode.DecimalNumberInDigit, token);

                if (IsMiddleType(TokenType.Name, TokenType.WhiteSpace))
                    return new SanitizeResult(ResultCode.WhiteSpaceInName, token);

                if (IsMiddleType(TokenType.DecimalDigit, TokenType.Space))
                {
                    tokens.RemoveAt(i--);
                    goto End;
                }

                if (IsMiddleType(TokenType.DecimalDigit, TokenType.DecimalSeparator))
                {
                    int offset = i - 1;
                    MergeGroup(
                        builder, tokens, offset, length: 3, removeTokens: true,
                        TokenType.DecimalNumber, out var resultToken);
                    tokens.Insert(offset, resultToken);
                    goto End;
                }

            End:
                lastToken = token;
            }
            return SanitizeResult.Ok;
        }

        #endregion

        #region MergeGroupsByType

        private static SanitizeResult MergeGroupsOfSingles(StringBuilder builder, List<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                #region MergeGroup helpers

                bool CanMergeGroup(TokenType expectedType, out int length)
                {
                    // TODO: cache predicate
                    int end = AsLongAsMatch(tokens, i, t => t.Type == expectedType);
                    length = end - i;
                    if (length > 1)
                        return true;
                    return false;
                }

                void MergeGroupAndInsert(TokenType expectedType, TokenType resultType)
                {
                    if (!CanMergeGroup(expectedType, out int length))
                        return;

                    if (!MergeGroup(builder, tokens, i, length, true, resultType, out var mergedToken))
                        // Should never be thrown as we check the length with CanMergeGroup()
                        throw new Exception("Unknown state.");

                    tokens.Insert(i, mergedToken);
                }

                #endregion

                if (CanMergeGroup(TokenType.DecimalNumber, out _))
                    return new SanitizeResult(ResultCode.RepeatingDecimalNumbers, tokens[i]);

                if (CanMergeGroup(TokenType.DecimalSeparator, out _))
                    return new SanitizeResult(ResultCode.RepeatingDecimalSeparators, tokens[i]);

                if (CanMergeGroup(TokenType.ListSeparator, out _))
                    return new SanitizeResult(ResultCode.RepeatingListSeparators, tokens[i]);

                MergeGroupAndInsert(TokenType.DecimalDigit, TokenType.DecimalNumber);
            }
            return SanitizeResult.Ok;
        }

        #endregion

        #region MergeGroupsOfMultipleTypes

        private static SanitizeResult MergeGroupsOfMultiples(
            StringBuilder builder, List<Token> tokens)
        {
            Token lastToken = null;

            // TODO: clean up this function

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                #region Merging Names with Spaces and Digits

                if (token.Type == TokenType.Name || 
                    token.Type == TokenType.Space)
                {
                    static bool IsNameSpaceOrDigits(Token t)
                    {
                        return t.Type == TokenType.Name
                            || t.Type == TokenType.Space
                            || (t is ValueToken valueToken && valueToken.ConsistsOfDigits());
                    }

                    int end = AsLongAsMatch(tokens, i, IsNameSpaceOrDigits);
                    int length = end - i;

                    if (MergeGroup(builder, tokens, i, length, true, TokenType.Name, out var resultToken))
                        tokens.Insert(i, resultToken);
                    goto End;
                }

                #endregion

                #region Merging Digits with Numbers

                static bool IsDigitOrDigitNumber(Token t)
                {
                    if (t.Type == TokenType.DecimalDigit)
                        return true;
                    if (t.Type == TokenType.DecimalNumber)
                        return MatchesTypes(((ValueToken)t).Value, DecimalNumberComponents);
                    return false;
                }
                if (IsDigitOrDigitNumber(token))
                {
                    int end = AsLongAsMatch(tokens, i, IsDigitOrDigitNumber);
                    int length = end - i;

                    // Check if multiple tokens contain a decimal separator.
                    if (length > 1)
                    {
                        bool separatorEncountered = false;
                        for (int j = i; j < end + 1 && j < tokens.Count; j++)
                        {
                            if (tokens[j] is ValueToken vt)
                            {
                                bool hasSeparator = HasType(vt.Value, DecimalSeparatorTypes);
                                if (hasSeparator && separatorEncountered)
                                    return new SanitizeResult(ResultCode.MultipleDecimalSeparators, token);
                                separatorEncountered |= hasSeparator;
                            }
                        }
                    }

                    if (MergeGroup(
                        builder, tokens, i, length, removeTokens: true,
                        TokenType.DecimalNumber, out var resultToken))
                        tokens.Insert(i, resultToken);
                    goto End;
                }

                #endregion

                #region Making decimal numbers with separator and trailing digits

                // Check for DecimalSeparators with trailing digits
                // and turn them into decimal numbers.
                if (token.Type == TokenType.DecimalSeparator)
                {
                    var nextToken = GetNextToken(tokens, i);
                    if (nextToken == null ||
                        nextToken.Type != TokenType.DecimalDigit)
                        return new SanitizeResult(ResultCode.TrailingDecimalSeparator, token);

                    if (lastToken != null &&
                        lastToken.Type != TokenType.Operator)
                        return new SanitizeResult(ResultCode.InvalidDecimalSeparator, token);

                    builder.Clear();
                    builder.Append("0");
                    builder.Append(((ValueToken)token).Value);
                    builder.Append(((ValueToken)nextToken).Value);
                    var decimalString = builder.ToString().AsMemory();

                    var decimalToken = new ValueToken(TokenType.DecimalNumber, decimalString);
                    tokens[i] = decimalToken; // replace the current token
                    tokens.RemoveAt(i + 1); // remove the next token

                    i--; // Go back one step so we can check if we can merge with following digits.
                    goto End;
                }

            #endregion

            End:
                lastToken = token;
            }
            return SanitizeResult.Ok;
        }

        #endregion

        #region Helpers

        public static bool MatchesTypes(ReadOnlySpan<char> chars, ReadOnlySpan<TokenType> types)
        {
            if (types.Length == 0)
                throw new ArgumentException(nameof(types));

            for (int i = 0; i < chars.Length; i++)
            {
                var def = ExpressionTokenizer.GetDefinition(chars[i]);

                bool match = false;
                for (int j = 0; j < types.Length; j++)
                {
                    if (def.Type == types[j])
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                    return false;
            }
            return true;
        }

        public static bool HasType(ReadOnlySpan<char> chars, ReadOnlySpan<TokenType> types)
        {
            if (types.Length == 0)
                throw new ArgumentException(nameof(types));

            for (int i = 0; i < chars.Length; i++)
            {
                var def = ExpressionTokenizer.GetDefinition(chars[i]);
                if (def == null)
                    continue;

                for (int j = 0; j < types.Length; j++)
                {
                    if (def.Type == types[j])
                        return true;
                }
            }
            return false;
        }

        public static Token GetNextToken(IList<Token> tokens, int index)
        {
            return index + 1 < tokens.Count ? tokens[index + 1] : default;
        }

        public static int AsLongAsMatch(
            IList<Token> tokens, int offset, Predicate<Token> predicate)
        {
            for (; offset < tokens.Count; offset++)
            {
                if (!predicate.Invoke(tokens[offset]))
                    break;
            }
            return offset;
        }

        private static bool MergeGroup(
            StringBuilder builder, List<Token> tokens, int offset, int length, bool removeTokens,
            TokenType resultType, out Token resultToken)
        {
            if (length > 1)
            {
                builder.Clear();
                for (int i = 0; i < length; i++)
                {
                    var groupToken = tokens[i + offset];
                    if (!(groupToken is ValueToken groupValueToken))
                        throw new Exception("Groups may only consist out of value tokens.");

                    if (groupValueToken.Type == TokenType.DecimalSeparator)
                        builder.Append(DecimalSeparator);
                    else
                        builder.Append(groupValueToken.Value);

                    if (!removeTokens)
                        offset++;
                }

                if (removeTokens)
                    tokens.RemoveRange(offset, length);

                resultToken = new ValueToken(resultType, builder.ToString().AsMemory());
                return true;
            }
            resultToken = default;
            return false;
        }

        #endregion
    }
}
