namespace McdcTool.Models;

public class TruthTableRow
{
    public Dictionary<string, bool> ConditionValues { get; }
    public bool DecisionOutcome { get; }
    public int RowIndex { get; }

    public TruthTableRow(int rowIndex, Dictionary<string, bool> conditionValues, bool decisionOutcome)
    {
        RowIndex = rowIndex;
        ConditionValues = conditionValues;
        DecisionOutcome = decisionOutcome;
    }
}

public class TruthTable
{
    public List<string> ConditionLabels { get; }
    public List<TruthTableRow> Rows { get; }

    public TruthTable(List<string> conditionLabels, List<TruthTableRow> rows)
    {
        ConditionLabels = conditionLabels;
        Rows = rows;
    }
}

public class IndependencePair
{
    public string ConditionLabel { get; }
    public TruthTableRow TrueRow { get; }
    public TruthTableRow FalseRow { get; }

    public IndependencePair(string conditionLabel, TruthTableRow trueRow, TruthTableRow falseRow)
    {
        ConditionLabel = conditionLabel;
        TrueRow = trueRow;
        FalseRow = falseRow;
    }
}

public class McdcTestCase
{
    public TruthTableRow Row { get; }
    public List<string> CoversConditions { get; }

    public McdcTestCase(TruthTableRow row, List<string> coversConditions)
    {
        Row = row;
        CoversConditions = coversConditions;
    }
}

public class McdcResult
{
    public string ExpressionText { get; }
    public string DisplayExpression { get; }
    public TruthTable TruthTable { get; }
    public Dictionary<string, List<IndependencePair>> IndependencePairs { get; }
    public List<McdcTestCase> MinimalTestSet { get; }
    public Dictionary<string, string> ConditionMappings { get; }

    public McdcResult(
        string expressionText,
        string displayExpression,
        TruthTable truthTable,
        Dictionary<string, List<IndependencePair>> independencePairs,
        List<McdcTestCase> minimalTestSet,
        Dictionary<string, string> conditionMappings)
    {
        ExpressionText = expressionText;
        DisplayExpression = displayExpression;
        TruthTable = truthTable;
        IndependencePairs = independencePairs;
        MinimalTestSet = minimalTestSet;
        ConditionMappings = conditionMappings;
    }
}
