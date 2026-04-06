using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace McdcTool.Parsing;

public class DecisionInfo
{
    public string FilePath { get; }
    public int LineNumber { get; }
    public int Column { get; }
    public string StatementType { get; }
    public string ExpressionText { get; }
    public ExpressionSyntax ExpressionSyntax { get; }

    public DecisionInfo(
        string filePath,
        int lineNumber,
        int column,
        string statementType,
        string expressionText,
        ExpressionSyntax expressionSyntax)
    {
        FilePath = filePath;
        LineNumber = lineNumber;
        Column = column;
        StatementType = statementType;
        ExpressionText = expressionText;
        ExpressionSyntax = expressionSyntax;
    }

    public string LocationString => $"{Path.GetFileName(FilePath)}:{LineNumber}:{Column}";
}
