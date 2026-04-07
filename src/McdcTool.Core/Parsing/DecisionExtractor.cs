using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace McdcTool.Core.Parsing;

public class DecisionExtractor : CSharpSyntaxWalker
{
    private readonly List<DecisionInfo> _decisions = new();
    private string _filePath = "";

    public List<DecisionInfo> Extract(string filePath, string sourceCode)
    {
        _filePath = filePath;
        _decisions.Clear();

        var tree = CSharpSyntaxTree.ParseText(sourceCode,
            new CSharpParseOptions(LanguageVersion.Preview));
        var root = tree.GetRoot();
        Visit(root);

        return new List<DecisionInfo>(_decisions);
    }

    public override void VisitIfStatement(IfStatementSyntax node)
    {
        TryAddDecision(node.Condition, "if");
        base.VisitIfStatement(node);
    }

    public override void VisitWhileStatement(WhileStatementSyntax node)
    {
        TryAddDecision(node.Condition, "while");
        base.VisitWhileStatement(node);
    }

    public override void VisitForStatement(ForStatementSyntax node)
    {
        if (node.Condition != null)
        {
            TryAddDecision(node.Condition, "for");
        }
        base.VisitForStatement(node);
    }

    public override void VisitDoStatement(DoStatementSyntax node)
    {
        TryAddDecision(node.Condition, "do-while");
        base.VisitDoStatement(node);
    }

    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        TryAddDecision(node.Condition, "ternary");
        base.VisitConditionalExpression(node);
    }

    private void TryAddDecision(ExpressionSyntax condition, string statementType)
    {
        // Only include compound decisions (those with logical operators)
        if (!HasLogicalOperator(condition))
            return;

        var lineSpan = condition.GetLocation().GetLineSpan();
        var startLine = lineSpan.StartLinePosition.Line + 1;
        var startCol = lineSpan.StartLinePosition.Character + 1;

        _decisions.Add(new DecisionInfo(
            filePath: _filePath,
            lineNumber: startLine,
            column: startCol,
            statementType: statementType,
            expressionText: condition.ToString(),
            expressionSyntax: condition));
    }

    private static bool HasLogicalOperator(ExpressionSyntax expression)
    {
        if (expression is BinaryExpressionSyntax binary)
        {
            if (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                binary.IsKind(SyntaxKind.LogicalOrExpression))
                return true;

            return HasLogicalOperator(binary.Left) || HasLogicalOperator(binary.Right);
        }

        if (expression is PrefixUnaryExpressionSyntax prefix &&
            prefix.IsKind(SyntaxKind.LogicalNotExpression))
        {
            return HasLogicalOperator(prefix.Operand);
        }

        if (expression is ParenthesizedExpressionSyntax parens)
        {
            return HasLogicalOperator(parens.Expression);
        }

        return false;
    }
}
