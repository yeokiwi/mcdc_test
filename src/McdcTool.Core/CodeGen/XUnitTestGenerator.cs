using System.Text;
using McdcTool.Core.Models;
using McdcTool.Core.Parsing;

namespace McdcTool.Core.CodeGen;

public class XUnitTestGenerator
{
    public string Generate(DecisionInfo decision, McdcResult result)
    {
        var sb = new StringBuilder();
        var className = BuildClassName(decision);
        var methodName = $"Decision_Line{decision.LineNumber}";

        sb.AppendLine("using Xunit;");
        sb.AppendLine();
        sb.AppendLine($"// Auto-generated MC/DC test cases for: {decision.LocationString}");
        sb.AppendLine($"// Original expression: {result.ExpressionText}");
        sb.AppendLine($"// Statement type: {decision.StatementType}");
        sb.AppendLine();
        sb.AppendLine("namespace McdcTests;");
        sb.AppendLine();
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        // Write condition mappings as comments
        sb.AppendLine("    // Condition mappings:");
        foreach (var (label, originalText) in result.ConditionMappings.OrderBy(kv => kv.Key))
        {
            sb.AppendLine($"    //   {label} = {originalText}");
        }
        sb.AppendLine();

        // Write the Theory test method
        sb.AppendLine("    [Theory]");

        foreach (var testCase in result.MinimalTestSet)
        {
            var values = result.TruthTable.ConditionLabels
                .Select(l => testCase.Row.ConditionValues[l] ? "true" : "false");
            var inlineData = string.Join(", ", values);
            var expectedStr = testCase.Row.DecisionOutcome ? "true" : "false";
            var coversStr = string.Join(", ", testCase.CoversConditions);

            sb.AppendLine($"    // Row {testCase.Row.RowIndex}: [{string.Join(", ", result.TruthTable.ConditionLabels.Select(l => $"{l}={testCase.Row.ConditionValues[l]}"))}] → {testCase.Row.DecisionOutcome} (covers: {coversStr})");
            sb.AppendLine($"    [InlineData({inlineData}, {expectedStr})]");
        }

        // Method parameters
        var parameters = result.TruthTable.ConditionLabels
            .Select(l => $"bool {l.ToLower()}")
            .ToList();
        parameters.Add("bool expectedOutcome");
        var paramStr = string.Join(", ", parameters);

        sb.AppendLine($"    public void {methodName}({paramStr})");
        sb.AppendLine("    {");
        sb.AppendLine($"        // TODO: Implement test for expression: {result.ExpressionText}");
        sb.AppendLine($"        // Simplified: {result.DisplayExpression}");
        sb.AppendLine("        //");
        sb.AppendLine("        // Example:");
        sb.AppendLine($"        // bool actual = /* evaluate: {result.ExpressionText} */;");
        sb.AppendLine("        // Assert.Equal(expectedOutcome, actual);");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GenerateAll(List<(DecisionInfo Decision, McdcResult Result)> analyses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Xunit;");
        sb.AppendLine();
        sb.AppendLine("// Auto-generated MC/DC test cases");
        sb.AppendLine($"// Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("namespace McdcTests;");
        sb.AppendLine();

        foreach (var (decision, result) in analyses)
        {
            var className = BuildClassName(decision);
            var methodName = $"Decision_Line{decision.LineNumber}";

            sb.AppendLine($"// Expression: {result.ExpressionText}");
            sb.AppendLine($"// Location: {decision.LocationString} ({decision.StatementType})");
            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");

            // Condition mappings
            sb.AppendLine("    // Condition mappings:");
            foreach (var (label, originalText) in result.ConditionMappings.OrderBy(kv => kv.Key))
            {
                sb.AppendLine($"    //   {label} = {originalText}");
            }
            sb.AppendLine();

            sb.AppendLine("    [Theory]");

            foreach (var testCase in result.MinimalTestSet)
            {
                var values = result.TruthTable.ConditionLabels
                    .Select(l => testCase.Row.ConditionValues[l] ? "true" : "false");
                var inlineData = string.Join(", ", values);
                var expectedStr = testCase.Row.DecisionOutcome ? "true" : "false";
                var coversStr = string.Join(", ", testCase.CoversConditions);

                sb.AppendLine($"    // Row {testCase.Row.RowIndex}: [{string.Join(", ", result.TruthTable.ConditionLabels.Select(l => $"{l}={testCase.Row.ConditionValues[l]}"))}] → {testCase.Row.DecisionOutcome} (covers: {coversStr})");
                sb.AppendLine($"    [InlineData({inlineData}, {expectedStr})]");
            }

            var parameters = result.TruthTable.ConditionLabels
                .Select(l => $"bool {l.ToLower()}")
                .ToList();
            parameters.Add("bool expectedOutcome");
            var paramStr = string.Join(", ", parameters);

            sb.AppendLine($"    public void {methodName}({paramStr})");
            sb.AppendLine("    {");
            sb.AppendLine($"        // TODO: Implement test for expression: {result.ExpressionText}");
            sb.AppendLine($"        // Simplified: {result.DisplayExpression}");
            sb.AppendLine("        // Assert.Equal(expectedOutcome, actual);");
            sb.AppendLine("    }");

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildClassName(DecisionInfo decision)
    {
        var fileName = Path.GetFileNameWithoutExtension(decision.FilePath);
        // Sanitize for valid C# identifier
        var sanitized = new string(fileName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        return $"{sanitized}_Line{decision.LineNumber}_McdcTests";
    }
}
