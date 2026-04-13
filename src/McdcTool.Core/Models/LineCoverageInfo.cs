namespace McdcTool.Core.Models;

/// <summary>
/// Coverage information for a single source line, used by the visual coverage display.
/// </summary>
public class LineCoverageInfo
{
    public int LineNumber { get; set; }
    public string SourceText { get; set; } = "";

    /// <summary>True if this line contains an executable statement.</summary>
    public bool IsExecutableStatement { get; set; }

    /// <summary>True if this line contains a compound boolean decision.</summary>
    public bool IsDecisionLine { get; set; }

    /// <summary>Statement covered: the line is in a method exercised by the test set.</summary>
    public bool StatementCovered { get; set; }

    /// <summary>Decision covered: the decision on this line was evaluated both true and false.</summary>
    public bool DecisionCovered { get; set; }

    /// <summary>MC/DC covered: the decision on this line has a full MC/DC test set.</summary>
    public bool McdcCovered { get; set; }
}

/// <summary>
/// Per-file line-level coverage data for visual display.
/// </summary>
public class FileCoverageDisplay
{
    public string FilePath { get; set; } = "";
    public List<LineCoverageInfo> Lines { get; set; } = new();
}
