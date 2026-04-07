using McdcTool.Core.Models;

namespace McdcTool.Core.Analysis;

public class McdcAnalyzer
{
    private readonly TruthTableGenerator _truthTableGenerator = new();

    public McdcResult Analyze(BooleanExpression expression, List<Condition> conditions, string originalText)
    {
        var truthTable = _truthTableGenerator.Generate(expression, conditions);
        var labels = conditions.Select(c => c.Label).ToList();

        // Step 1: Find all independence pairs for each condition
        var independencePairs = FindIndependencePairs(truthTable, labels);

        // Step 2: Select minimal test set using greedy set cover
        var minimalTestSet = SelectMinimalTestSet(truthTable, independencePairs, labels);

        // Build condition mappings (label -> original text)
        var mappings = conditions.ToDictionary(c => c.Label, c => c.OriginalText);

        return new McdcResult(
            expressionText: originalText,
            displayExpression: expression.ToDisplayString(),
            truthTable: truthTable,
            independencePairs: independencePairs,
            minimalTestSet: minimalTestSet,
            conditionMappings: mappings);
    }

    private Dictionary<string, List<IndependencePair>> FindIndependencePairs(
        TruthTable truthTable, List<string> labels)
    {
        var pairs = new Dictionary<string, List<IndependencePair>>();

        foreach (var label in labels)
        {
            pairs[label] = new List<IndependencePair>();
            var otherLabels = labels.Where(l => l != label).ToList();

            for (int i = 0; i < truthTable.Rows.Count; i++)
            {
                for (int j = i + 1; j < truthTable.Rows.Count; j++)
                {
                    var rowA = truthTable.Rows[i];
                    var rowB = truthTable.Rows[j];

                    // The target condition must differ
                    if (rowA.ConditionValues[label] == rowB.ConditionValues[label])
                        continue;

                    // The decision outcome must differ
                    if (rowA.DecisionOutcome == rowB.DecisionOutcome)
                        continue;

                    // All other conditions must be the same
                    bool othersMatch = otherLabels.All(l =>
                        rowA.ConditionValues[l] == rowB.ConditionValues[l]);

                    if (!othersMatch)
                        continue;

                    // Found an independence pair
                    var trueRow = rowA.ConditionValues[label] ? rowA : rowB;
                    var falseRow = rowA.ConditionValues[label] ? rowB : rowA;
                    pairs[label].Add(new IndependencePair(label, trueRow, falseRow));
                }
            }
        }

        return pairs;
    }

    private List<McdcTestCase> SelectMinimalTestSet(
        TruthTable truthTable,
        Dictionary<string, List<IndependencePair>> independencePairs,
        List<string> labels)
    {
        // Build a mapping: for each row index, which conditions does it help cover
        // We need at least one independence pair per condition to be "covered"
        // A pair is covered if both its rows are in the selected set

        // First, select one pair per condition (prefer pairs that share rows)
        var selectedPairs = SelectBestPairs(independencePairs, labels);

        // Collect all unique row indices needed
        var neededRows = new HashSet<int>();
        var rowConditionMap = new Dictionary<int, List<string>>();

        foreach (var (label, pair) in selectedPairs)
        {
            neededRows.Add(pair.TrueRow.RowIndex);
            neededRows.Add(pair.FalseRow.RowIndex);

            if (!rowConditionMap.ContainsKey(pair.TrueRow.RowIndex))
                rowConditionMap[pair.TrueRow.RowIndex] = new List<string>();
            rowConditionMap[pair.TrueRow.RowIndex].Add(label);

            if (!rowConditionMap.ContainsKey(pair.FalseRow.RowIndex))
                rowConditionMap[pair.FalseRow.RowIndex] = new List<string>();
            rowConditionMap[pair.FalseRow.RowIndex].Add(label);
        }

        // Build the minimal test set
        var testCases = new List<McdcTestCase>();
        foreach (var rowIdx in neededRows.OrderBy(r => r))
        {
            var row = truthTable.Rows[rowIdx];
            var covers = rowConditionMap.ContainsKey(rowIdx)
                ? rowConditionMap[rowIdx].Distinct().ToList()
                : new List<string>();
            testCases.Add(new McdcTestCase(row, covers));
        }

        return testCases;
    }

    private Dictionary<string, IndependencePair> SelectBestPairs(
        Dictionary<string, List<IndependencePair>> independencePairs,
        List<string> labels)
    {
        // Greedy selection: for each condition, pick the pair that maximizes
        // row reuse with already-selected rows
        var selected = new Dictionary<string, IndependencePair>();
        var usedRows = new HashSet<int>();

        // Sort labels by number of available pairs (ascending) - handle most constrained first
        var sortedLabels = labels
            .Where(l => independencePairs[l].Count > 0)
            .OrderBy(l => independencePairs[l].Count)
            .ToList();

        foreach (var label in sortedLabels)
        {
            var pairs = independencePairs[label];
            if (pairs.Count == 0) continue;

            // Pick the pair that reuses the most already-selected rows
            var best = pairs
                .OrderByDescending(p =>
                    (usedRows.Contains(p.TrueRow.RowIndex) ? 1 : 0) +
                    (usedRows.Contains(p.FalseRow.RowIndex) ? 1 : 0))
                .First();

            selected[label] = best;
            usedRows.Add(best.TrueRow.RowIndex);
            usedRows.Add(best.FalseRow.RowIndex);
        }

        return selected;
    }
}
