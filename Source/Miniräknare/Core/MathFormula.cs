﻿using System;
using System.Collections.Generic;
using Miniräknare.Expressions;
using Miniräknare.Expressions.Tokens;

namespace Miniräknare
{
    public class MathFormula : IExpressionTree
    {
        private ExpressionTree _tree;
        private List<ValueToken> _inputs;

        public ExpressionSanitizer.SanitizeResult SanitizeResult { get; }
        public ExpressionParser.ResultCode ParseCode { get; }

        public bool IsValid =>
            SanitizeResult.Code == ExpressionSanitizer.ResultCode.Ok &&
            ParseCode == ExpressionParser.ResultCode.Ok;

        public ReadOnlyMemory<MathFormula> Permutations { get; }

        #region IExpressionTree

        public ExpressionOptions ExpressionOptions => _tree.ExpressionOptions;
        public IReadOnlyList<Token> Tokens => _tree.Tokens;

        #endregion

        public MathFormula(ExpressionOptions options, ReadOnlyMemory<char> formula)
        {
            _tree = new ExpressionTree(options);
            ExpressionTokenizer.Tokenize(formula, _tree.Tokens);

            SanitizeResult = ExpressionSanitizer.Sanitize(_tree);
            if (SanitizeResult.Code != ExpressionSanitizer.ResultCode.Ok)
                return;

            ParseCode = ExpressionParser.Parse(_tree);
            if (ParseCode != ExpressionParser.ResultCode.Ok)
                return;

            _inputs = GatherInputs(_tree);
            Permutations = CreatePermutations();
        }

        public MathFormula(ExpressionOptions options, MathFormulaData data)
            : this(options, data.Formula.AsMemory())
        {
        }

        private static List<ValueToken> GatherInputs(ExpressionTree tree)
        {
            var inputs = new List<ValueToken>();
            var probe = new ExpressionTreeProbe();
            probe.ProbeReference += (reference) =>
            {
                inputs.Add(reference);
            };
            probe.Probe(tree);
            return inputs;
        }

        private MathFormula[] CreatePermutations()
        {
            var permutations = new MathFormula[_inputs.Count];
            for (int i = 0; i < permutations.Length; i++)
            {
                var input = _inputs[i];

                var tmp = new List<Token>();

                for (int j = 0; j < _tree.Tokens.Count; j++)
                {
                    var token = _tree.Tokens[j];
                    if (token is ListToken list)
                    {
                        Console.WriteLine(list.Children[0].Parent);
                        break;
                    }
                }

                throw new NotImplementedException();
            }
            return permutations;
        }
    }
}
