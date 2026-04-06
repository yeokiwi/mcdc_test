using McdcTool.Models;

namespace McdcTool.Analysis;

public class TruthTableGenerator
{
    public TruthTable Generate(BooleanExpression expression, List<Condition> conditions)
    {
        var labels = conditions.Select(c => c.Label).ToList();
        int n = conditions.Count;
        int totalRows = 1 << n; // 2^n
        var rows = new List<TruthTableRow>();

        for (int i = 0; i < totalRows; i++)
        {
            var values = new Dictionary<string, bool>();
            for (int j = 0; j < n; j++)
            {
                // MSB first: condition A is the leftmost bit
                bool val = ((i >> (n - 1 - j)) & 1) == 1;
                values[labels[j]] = val;
            }

            bool outcome = expression.Evaluate(values);
            rows.Add(new TruthTableRow(i, values, outcome));
        }

        return new TruthTable(labels, rows);
    }
}
