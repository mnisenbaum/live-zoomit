using System.Windows;
using System.Windows.Input;

namespace LiveZoomIt.Drawing;

public enum ShapeKind
{
    Line,
    Rectangle,
    Ellipse,
    Arrow,
}

/// <summary>
/// Gera os pontos das formas desenhadas com modificador. Cada forma vira um único traço
/// de tinta, então cor, espessura, desfazer e "limpar" funcionam igual ao traço livre.
/// </summary>
public static class ShapeBuilder
{
    /// <summary>Linhas a até este ângulo de 0°/45°/90° se encaixam no ângulo exato.</summary>
    public const double SnapToleranceDegrees = 4;

    private const double MinDragDistance = 2;
    private const double ArrowHeadAngleDegrees = 28;

    /// <summary>Forma escolhida pelas teclas seguradas no início do arraste; null = traço livre.</summary>
    public static ShapeKind? FromModifiers(ModifierKeys modifiers) => modifiers switch
    {
        ModifierKeys.Control | ModifierKeys.Shift => ShapeKind.Arrow,
        ModifierKeys.Control => ShapeKind.Rectangle,
        ModifierKeys.Shift => ShapeKind.Line,
        ModifierKeys.Alt => ShapeKind.Ellipse,
        _ => null,
    };

    public static bool IsDegenerate(Point start, Point end) =>
        Math.Abs(end.X - start.X) < MinDragDistance && Math.Abs(end.Y - start.Y) < MinDragDistance;

    public static Point[] Build(ShapeKind kind, Point start, Point end, double penWidth) => kind switch
    {
        ShapeKind.Line => [start, SnapLineEnd(start, end)],
        ShapeKind.Rectangle => Rectangle(start, end),
        ShapeKind.Ellipse => Ellipse(start, end),
        ShapeKind.Arrow => Arrow(start, SnapLineEnd(start, end), penWidth),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static Point SnapLineEnd(Point start, Point end)
    {
        Vector v = end - start;
        if (v.Length < MinDragDistance)
            return end;

        double angle = Math.Atan2(v.Y, v.X) * 180 / Math.PI;
        double snapped = Math.Round(angle / 45) * 45;
        if (Math.Abs(angle - snapped) > SnapToleranceDegrees)
            return end;

        double radians = snapped * Math.PI / 180;
        return start + new Vector(Math.Cos(radians), Math.Sin(radians)) * v.Length;
    }

    private static Point[] Rectangle(Point a, Point b) =>
        [a, new Point(b.X, a.Y), b, new Point(a.X, b.Y), a];

    private static Point[] Ellipse(Point a, Point b)
    {
        double rx = Math.Abs(b.X - a.X) / 2;
        double ry = Math.Abs(b.Y - a.Y) / 2;
        var center = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);

        // Um ponto a cada ~4 px de perímetro deixa a curva lisa sem precisar de suavização.
        double perimeter = 2 * Math.PI * Math.Sqrt((rx * rx + ry * ry) / 2);
        int segments = Math.Clamp((int)(perimeter / 4), 24, 360);

        var points = new Point[segments + 1];
        for (int i = 0; i < segments; i++)
        {
            double t = 2 * Math.PI * i / segments;
            points[i] = new Point(center.X + rx * Math.Cos(t), center.Y + ry * Math.Sin(t));
        }
        points[segments] = points[0];
        return points;
    }

    /// <summary>Haste e ponta num único traço: início → fim → aba esquerda → fim → aba direita.</summary>
    private static Point[] Arrow(Point start, Point end, double penWidth)
    {
        Vector shaft = end - start;
        if (shaft.Length < MinDragDistance)
            return [start, end];

        double headLength = Math.Min(Math.Max(14, penWidth * 4), shaft.Length * 0.5);
        Vector back = -shaft / shaft.Length * headLength;

        return [start, end, end + Rotate(back, ArrowHeadAngleDegrees), end, end + Rotate(back, -ArrowHeadAngleDegrees)];
    }

    private static Vector Rotate(Vector v, double degrees)
    {
        double r = degrees * Math.PI / 180;
        double cos = Math.Cos(r), sin = Math.Sin(r);
        return new Vector(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
