using McdcTool.Core.Parsing;

namespace McdcTool.Tests;

public class DecisionExtractorTests
{
    private readonly DecisionExtractor _extractor = new();

    [Fact]
    public void Extract_IfWithCompoundCondition_FindsDecision()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal("if", decisions[0].StatementType);
        Assert.Contains("&&", decisions[0].ExpressionText);
    }

    [Fact]
    public void Extract_SimpleCondition_IsSkipped()
    {
        var code = @"
class C {
    void M(bool a) {
        if (a) { }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Empty(decisions);
    }

    [Fact]
    public void Extract_WhileLoop_FindsDecision()
    {
        var code = @"
class C {
    void M(int x, int y) {
        while (x > 0 && y < 10) { x--; }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal("while", decisions[0].StatementType);
    }

    [Fact]
    public void Extract_DoWhile_FindsDecision()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        do { } while (a || b);
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal("do-while", decisions[0].StatementType);
    }

    [Fact]
    public void Extract_Ternary_FindsDecision()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        var x = (a && b) ? 1 : 0;
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal("ternary", decisions[0].StatementType);
    }

    [Fact]
    public void Extract_ForLoop_FindsDecision()
    {
        var code = @"
class C {
    void M() {
        for (int i = 0; i < 10 && i > 0; i++) { }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal("for", decisions[0].StatementType);
    }

    [Fact]
    public void Extract_MultipleDecisions_FindsAll()
    {
        var code = @"
class C {
    void M(bool a, bool b, bool c) {
        if (a && b) { }
        while (b || c) { }
        var x = (a && c) ? 1 : 0;
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Equal(3, decisions.Count);
    }

    [Fact]
    public void Extract_NestedIf_FindsBothDecisions()
    {
        var code = @"
class C {
    void M(bool a, bool b, bool c, bool d) {
        if (a && b) {
            if (c || d) { }
        }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Equal(2, decisions.Count);
    }

    [Fact]
    public void Extract_RecordsCorrectLineNumbers()
    {
        var code = @"class C {
    void M(bool a, bool b) {
        if (a && b) { }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal(3, decisions[0].LineNumber);
    }

    [Fact]
    public void Extract_ForLoopWithoutCondition_DoesNotCrash()
    {
        var code = @"
class C {
    void M() {
        for (;;) { break; }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Empty(decisions);
    }

    [Fact]
    public void Extract_NotWrappingCompound_FindsDecision()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        if (!(a && b)) { }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
        Assert.Equal("if", decisions[0].StatementType);
    }

    [Fact]
    public void Extract_ParenthesizedCompound_FindsDecision()
    {
        var code = @"
class C {
    void M(bool a, bool b) {
        if ((a || b)) { }
    }
}";
        var decisions = _extractor.Extract("test.cs", code);

        Assert.Single(decisions);
    }
}
