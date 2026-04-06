namespace McdcTool.Models;

public abstract class BooleanExpression
{
    public abstract bool Evaluate(Dictionary<string, bool> values);
    public abstract List<Condition> GetConditions();
    public abstract string ToDisplayString();
}

public class Condition : BooleanExpression
{
    public string Label { get; set; } = "";
    public string OriginalText { get; }

    public Condition(string originalText)
    {
        OriginalText = originalText;
    }

    public override bool Evaluate(Dictionary<string, bool> values)
    {
        return values[Label];
    }

    public override List<Condition> GetConditions() => new() { this };

    public override string ToDisplayString() => Label;
}

public class AndExpression : BooleanExpression
{
    public BooleanExpression Left { get; }
    public BooleanExpression Right { get; }

    public AndExpression(BooleanExpression left, BooleanExpression right)
    {
        Left = left;
        Right = right;
    }

    public override bool Evaluate(Dictionary<string, bool> values)
    {
        return Left.Evaluate(values) && Right.Evaluate(values);
    }

    public override List<Condition> GetConditions()
    {
        var conditions = Left.GetConditions();
        conditions.AddRange(Right.GetConditions());
        return conditions;
    }

    public override string ToDisplayString() => $"({Left.ToDisplayString()} && {Right.ToDisplayString()})";
}

public class OrExpression : BooleanExpression
{
    public BooleanExpression Left { get; }
    public BooleanExpression Right { get; }

    public OrExpression(BooleanExpression left, BooleanExpression right)
    {
        Left = left;
        Right = right;
    }

    public override bool Evaluate(Dictionary<string, bool> values)
    {
        return Left.Evaluate(values) || Right.Evaluate(values);
    }

    public override List<Condition> GetConditions()
    {
        var conditions = Left.GetConditions();
        conditions.AddRange(Right.GetConditions());
        return conditions;
    }

    public override string ToDisplayString() => $"({Left.ToDisplayString()} || {Right.ToDisplayString()})";
}

public class NotExpression : BooleanExpression
{
    public BooleanExpression Operand { get; }

    public NotExpression(BooleanExpression operand)
    {
        Operand = operand;
    }

    public override bool Evaluate(Dictionary<string, bool> values)
    {
        return !Operand.Evaluate(values);
    }

    public override List<Condition> GetConditions() => Operand.GetConditions();

    public override string ToDisplayString() => $"!{Operand.ToDisplayString()}";
}
