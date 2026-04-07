using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using McdcTool.Core.Models;

namespace McdcTool.Core.Analysis;

public class ExpressionParser
{
    private int _conditionCounter;
    private readonly List<Condition> _conditions = new();

    public (BooleanExpression Expression, List<Condition> Conditions) Parse(ExpressionSyntax syntax)
    {
        _conditionCounter = 0;
        _conditions.Clear();
        var expression = ParseNode(syntax);
        return (expression, _conditions);
    }

    private BooleanExpression ParseNode(ExpressionSyntax syntax)
    {
        // Unwrap parenthesized expressions
        if (syntax is ParenthesizedExpressionSyntax parens)
        {
            return ParseNode(parens.Expression);
        }

        // Logical AND: &&
        if (syntax is BinaryExpressionSyntax binary)
        {
            if (binary.IsKind(SyntaxKind.LogicalAndExpression))
            {
                var left = ParseNode(binary.Left);
                var right = ParseNode(binary.Right);
                return new AndExpression(left, right);
            }

            if (binary.IsKind(SyntaxKind.LogicalOrExpression))
            {
                var left = ParseNode(binary.Left);
                var right = ParseNode(binary.Right);
                return new OrExpression(left, right);
            }
        }

        // Logical NOT: !
        if (syntax is PrefixUnaryExpressionSyntax prefix &&
            prefix.IsKind(SyntaxKind.LogicalNotExpression))
        {
            var operand = ParseNode(prefix.Operand);
            return new NotExpression(operand);
        }

        // Everything else is an atomic condition
        return CreateCondition(syntax.ToString().Trim());
    }

    private Condition CreateCondition(string text)
    {
        char label = (char)('A' + _conditionCounter);
        _conditionCounter++;
        var condition = new Condition(text) { Label = label.ToString() };
        _conditions.Add(condition);
        return condition;
    }
}
