namespace McdcTool.Core.Models;

public class FileCoverageInfo
{
    public string FilePath { get; set; } = "";
    public int TotalStatements { get; set; }
    public int CoveredStatements { get; set; }
    public double StatementCoveragePercent =>
        TotalStatements == 0 ? 0.0 : (double)CoveredStatements / TotalStatements * 100;
    public int TotalDecisions { get; set; }
    public int CoveredDecisions { get; set; }
    public double DecisionCoveragePercent =>
        TotalDecisions == 0 ? 0.0 : (double)CoveredDecisions / TotalDecisions * 100;
}

public class CoverageReport
{
    public List<FileCoverageInfo> Files { get; set; } = new();
    public int TotalStatements => Files.Sum(f => f.TotalStatements);
    public int CoveredStatements => Files.Sum(f => f.CoveredStatements);
    public double StatementCoveragePercent =>
        TotalStatements == 0 ? 0.0 : (double)CoveredStatements / TotalStatements * 100;
    public int TotalDecisions => Files.Sum(f => f.TotalDecisions);
    public int CoveredDecisions => Files.Sum(f => f.CoveredDecisions);
    public double DecisionCoveragePercent =>
        TotalDecisions == 0 ? 0.0 : (double)CoveredDecisions / TotalDecisions * 100;
}
