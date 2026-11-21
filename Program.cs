using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Console.WriteLine("==========================================");
Console.WriteLine("       NEWTON-RAPHSON ROOT FINDER");
Console.WriteLine("==========================================");
Console.WriteLine("Example: x^3 - 2*x - 5");
Console.WriteLine("Functions: sin, cos, tan, exp, log, sqrt, abs");
Console.WriteLine("Use * for multiplication, e.g. 2*x");
Console.WriteLine();

try
{
    Console.Write("Enter f(x): ");
    string expression = Console.ReadLine() ?? "";

    ExpressionParser parser = new ExpressionParser(expression);

    Console.Write("Enter starting value X0: ");
    double x = ReadDouble();

    Console.Write("Enter tolerance (e.g. 0.000001): ");
    double tolerance = ReadDouble();

    Console.Write("Maximum iterations: ");
    int maxIterations = int.Parse(Console.ReadLine() ?? "100");

    if (tolerance <= 0 || maxIterations <= 0)
        throw new Exception("Tolerance and iterations must be positive.");

    List<double> values = new List<double> { x };

    Console.WriteLine();
    Console.WriteLine("---------------------------------------------------------------");
    Console.WriteLine("{0,-8} {1,-18} {2,-18} {3,-18}",
        "Iter", "X", "f(X)", "Error");
    Console.WriteLine("---------------------------------------------------------------");

    bool converged = false;
    double fx = parser.Evaluate(x);
    int iteration = 0;

    Console.WriteLine("{0,-8} {1,-18:F10} {2,-18:F10} {3,-18}",
        0, x, fx, "-");

    for (iteration = 1; iteration <= maxIterations; iteration++)
    {
        fx = parser.Evaluate(x);

        // Numerical derivative using central difference
        double h = 0.000001 * Math.Max(1.0, Math.Abs(x));

        double derivative =
            (parser.Evaluate(x + h) - parser.Evaluate(x - h)) / (2 * h);

        if (Math.Abs(derivative) < 1e-14)
            throw new Exception("Derivative is zero or too small.");

        double nextX = x - fx / derivative;

        if (double.IsNaN(nextX) || double.IsInfinity(nextX))
            throw new Exception("Calculation became invalid.");

        double error = Math.Abs(nextX - x);
        double nextFx = parser.Evaluate(nextX);

        Console.WriteLine("{0,-8} {1,-18:F10} {2,-18:F10} {3,-18:E6}",
            iteration, nextX, nextFx, error);

        values.Add(nextX);

        if (error < tolerance || Math.Abs(nextFx) < tolerance)
        {
            x = nextX;
            converged = true;
            break;
        }

        x = nextX;
    }

    Console.WriteLine("---------------------------------------------------------------");
    Console.WriteLine();

    if (converged)
    {
        Console.WriteLine("Root found successfully!");
        Console.WriteLine("Approximate root = " + x.ToString("F10"));
        Console.WriteLine("f(root) = " + parser.Evaluate(x).ToString("E6"));
        Console.WriteLine("Number of iterations = " + iteration);
    }
    else
    {
        Console.WriteLine("Maximum iterations reached.");
        Console.WriteLine("Last approximation = " + x.ToString("F10"));
    }

    string graphFile = Path.Combine(
        Environment.CurrentDirectory, "NewtonRaphsonGraph.svg");

    CreateGraph(parser, values, graphFile);

    Console.WriteLine();
    Console.WriteLine("Graph saved to:");
    Console.WriteLine(graphFile);
    Console.WriteLine("Open this file with your browser.");

}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("ERROR: " + ex.Message);
}

Console.WriteLine();
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

static double ReadDouble()
{
    string input = Console.ReadLine() ?? "";

    if (!double.TryParse(input, NumberStyles.Float,
        CultureInfo.InvariantCulture, out double value))
    {
        throw new Exception("Please enter a valid number.");
    }

    return value;
}

static void CreateGraph(ExpressionParser parser,
    List<double> points, string filePath)
{
    int width = 900;
    int height = 600;
    int margin = 60;

    double minX = points.Min() - 3;
    double maxX = points.Max() + 3;

    if (maxX - minX < 1)
    {
        minX -= 2;
        maxX += 2;
    }

    int samples = 500;
    List<(double x, double y)> curve = new();

    for (int i = 0; i <= samples; i++)
    {
        double xx = minX + (maxX - minX) * i / samples;

        try
        {
            double yy = parser.Evaluate(xx);

            if (!double.IsNaN(yy) && !double.IsInfinity(yy))
                curve.Add((xx, yy));
        }
        catch
        {
            // Skip undefined points
        }
    }

    if (curve.Count == 0)
        throw new Exception("Cannot draw this function.");

    double minY = curve.Min(p => p.y);
    double maxY = curve.Max(p => p.y);

    // Limit extreme values so the graph remains readable
    double centerY = (minY + maxY) / 2;
    double rangeY = Math.Max(maxY - minY, 1);

    minY = centerY - rangeY * 0.6;
    maxY = centerY + rangeY * 0.6;

    double MapX(double xx) =>
        margin + (xx - minX) / (maxX - minX) * (width - 2 * margin);

    double MapY(double yy) =>
        height - margin - (yy - minY) / (maxY - minY)
        * (height - 2 * margin);

    StringBuilder svg = new StringBuilder();

    svg.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>");
    svg.AppendLine("<rect width='100%' height='100%' fill='white'/>");
    svg.AppendLine("<text x='450' y='30' text-anchor='middle' font-size='22' font-family='Arial'>Newton-Raphson Graph</text>");

    // X and Y axes
    if (minY <= 0 && maxY >= 0)
    {
        double y0 = MapY(0);
        svg.AppendLine($"<line x1='{margin}' y1='{y0}' x2='{width - margin}' y2='{y0}' stroke='black' stroke-width='2'/>");
    }

    if (minX <= 0 && maxX >= 0)
    {
        double x0 = MapX(0);
        svg.AppendLine($"<line x1='{x0}' y1='{margin}' x2='{x0}' y2='{height - margin}' stroke='black' stroke-width='2'/>");
    }

    // Function curve
    StringBuilder polyline = new();

    foreach (var p in curve)
    {
        double px = MapX(p.x);
        double py = MapY(p.y);

        polyline.Append($"{px:F2},{py:F2} ");
    }

    svg.AppendLine($"<polyline points='{polyline}' fill='none' stroke='#1565C0' stroke-width='3'/>");

    // Iteration points
    for (int i = 0; i < points.Count; i++)
    {
        double px = MapX(points[i]);

        double py;
        try
        {
            py = MapY(parser.Evaluate(points[i]));
        }
        catch
        {
            continue;
        }

        svg.AppendLine($"<circle cx='{px:F2}' cy='{py:F2}' r='6' fill='#E53935'/>");
        svg.AppendLine($"<text x='{px + 8:F2}' y='{py - 8:F2}' font-size='12' font-family='Arial'>x{i}</text>");
    }

    svg.AppendLine("</svg>");

    File.WriteAllText(filePath, svg.ToString());
}

// Expression parser
class ExpressionParser
{
    private readonly string text;
    private int pos;

    public ExpressionParser(string expression)
    {
        text = expression.Replace(" ", "")
                         .Replace("π", "pi");
    }

    public double Evaluate(double x)
    {
        pos = 0;
        double result = ParseExpression(x);

        if (pos < text.Length)
            throw new Exception("Invalid expression near: " + text[pos..]);

        return result;
    }

    private double ParseExpression(double x)
    {
        double value = ParseTerm(x);

        while (pos < text.Length)
        {
            if (Match('+'))
                value += ParseTerm(x);
            else if (Match('-'))
                value -= ParseTerm(x);
            else
                break;
        }

        return value;
    }

    private double ParseTerm(double x)
    {
        double value = ParsePower(x);

        while (pos < text.Length)
        {
            if (Match('*'))
                value *= ParsePower(x);
            else if (Match('/'))
                value /= ParsePower(x);
            else
                break;
        }

        return value;
    }

    private double ParsePower(double x)
    {
        double value = ParseUnary(x);

        if (Match('^'))
        {
            double exponent = ParsePower(x);
            value = Math.Pow(value, exponent);
        }

        return value;
    }

    private double ParseUnary(double x)
    {
        if (Match('+'))
            return ParseUnary(x);

        if (Match('-'))
            return -ParseUnary(x);

        return ParsePrimary(x);
    }

    private double ParsePrimary(double x)
    {
        if (Match('('))
        {
            double value = ParseExpression(x);

            if (!Match(')'))
                throw new Exception("Missing closing parenthesis.");

            return value;
        }

        if (pos < text.Length && char.IsDigit(text[pos]))
            return ParseNumber();

        if (pos < text.Length && char.IsLetter(text[pos]))
        {
            string name = ParseName().ToLower();

            if (name == "x")
                return x;

            if (name == "pi")
                return Math.PI;

            if (name == "e")
                return Math.E;

            if (!Match('('))
                throw new Exception("Function requires parentheses: " + name);

            double argument = ParseExpression(x);

            if (!Match(')'))
                throw new Exception("Missing closing parenthesis.");

            return name switch
            {
                "sin" => Math.Sin(argument),
                "cos" => Math.Cos(argument),
                "tan" => Math.Tan(argument),
                "exp" => Math.Exp(argument),
                "log" => Math.Log(argument),
                "sqrt" => Math.Sqrt(argument),
                "abs" => Math.Abs(argument),
                _ => throw new Exception("Unknown function: " + name)
            };
        }

        throw new Exception("Unexpected character in expression.");
    }

    private double ParseNumber()
    {
        int start = pos;

        while (pos < text.Length &&
              (char.IsDigit(text[pos]) || text[pos] == '.'))
        {
            pos++;
        }

        return double.Parse(text[start..pos],
            CultureInfo.InvariantCulture);
    }

    private string ParseName()
    {
        int start = pos;

        while (pos < text.Length && char.IsLetter(text[pos]))
            pos++;

        return text[start..pos];
    }

    private bool Match(char c)
    {
        if (pos < text.Length && text[pos] == c)
        {
            pos++;
            return true;
        }

        return false;
    }
}