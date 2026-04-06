namespace SampleApp;

public class SampleCode
{
    public void ProcessData(bool a, bool b, bool c, int x, int y)
    {
        // Simple two-condition AND
        if (a && b)
        {
            Console.WriteLine("Both true");
        }

        // Three conditions with mixed operators
        if (a && (b || c))
        {
            Console.WriteLine("Complex condition");
        }

        // While loop with compound condition
        while (x > 0 && y < 10)
        {
            x--;
            y++;
        }

        // Ternary with compound condition
        var result = (a || b) ? "yes" : "no";

        // Negation
        if (!a && b)
        {
            Console.WriteLine("Not a and b");
        }

        // Simple condition (should be skipped - no MC/DC needed)
        if (a)
        {
            Console.WriteLine("Simple");
        }

        // Do-while
        do
        {
            x++;
        } while (a && !b);

        // Deeply nested
        if ((a && b) || (c && !a))
        {
            Console.WriteLine("Nested");
        }
    }
}
