namespace McdcTool.Core.Models;

public class CoverageOptions
{
    public bool StatementCoverage { get; set; }
    public bool DecisionCoverage { get; set; }
    public bool McdcCoverage { get; set; }

    public bool AnyDecisionLevel => DecisionCoverage || McdcCoverage;
}
