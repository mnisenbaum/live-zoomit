using System.Windows;
using System.Windows.Input;
using LiveZoomIt.Drawing;

namespace LiveZoomIt.Tests;

public class ShapeBuilderTests
{
    private const double Tolerance = 1e-6;

    [Theory]
    [InlineData(ModifierKeys.Control, ShapeKind.Rectangle)]
    [InlineData(ModifierKeys.Shift, ShapeKind.Line)]
    [InlineData(ModifierKeys.Alt, ShapeKind.Ellipse)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Shift, ShapeKind.Arrow)]
    public void FromModifiers_MapsModifierToShape(ModifierKeys modifiers, ShapeKind expected) =>
        Assert.Equal(expected, ShapeBuilder.FromModifiers(modifiers));

    [Theory]
    [InlineData(ModifierKeys.None)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt)] // AltGr em teclados internacionais
    [InlineData(ModifierKeys.Windows)]
    public void FromModifiers_OtherCombinationsAreFreehand(ModifierKeys modifiers) =>
        Assert.Null(ShapeBuilder.FromModifiers(modifiers));

    [Fact]
    public void Rectangle_IsClosedThroughAllFourCorners()
    {
        var points = ShapeBuilder.Build(ShapeKind.Rectangle, new Point(10, 20), new Point(110, 70), 4);

        Assert.Equal(
            [new Point(10, 20), new Point(110, 20), new Point(110, 70), new Point(10, 70), new Point(10, 20)],
            points);
    }

    [Fact]
    public void Ellipse_PointsLieOnEllipseInsideDragBox()
    {
        var points = ShapeBuilder.Build(ShapeKind.Ellipse, new Point(200, 100), new Point(0, 0), 4);

        Assert.True(points.Length >= 25);
        Assert.Equal(points[0], points[^1]);
        foreach (var p in points)
        {
            double nx = (p.X - 100) / 100, ny = (p.Y - 50) / 50;
            Assert.Equal(1, nx * nx + ny * ny, 6);
        }
    }

    [Fact]
    public void Line_NearlyHorizontalSnapsToHorizontal()
    {
        var end = ShapeBuilder.SnapLineEnd(new Point(0, 0), new Point(100, 3));

        Assert.Equal(0, end.Y, 6);
        Assert.Equal(new Vector(100, 3).Length, end.X, 6); // mantém o comprimento arrastado
    }

    [Fact]
    public void Line_NearlyDiagonalSnapsTo45Degrees()
    {
        var end = ShapeBuilder.SnapLineEnd(new Point(50, 50), new Point(150, 147));

        Assert.Equal(end.X - 50, end.Y - 50, 6);
    }

    [Fact]
    public void Line_FarFromSnapAnglesIsUntouched()
    {
        var end = ShapeBuilder.SnapLineEnd(new Point(0, 0), new Point(100, 30)); // ~16,7°

        Assert.Equal(new Point(100, 30), end);
    }

    [Fact]
    public void Arrow_HasSymmetricHeadAtEnd()
    {
        var start = new Point(0, 0);
        var end = new Point(200, 0);
        var points = ShapeBuilder.Build(ShapeKind.Arrow, start, end, 4);

        Assert.Equal(5, points.Length);
        Assert.Equal(start, points[0]);
        Assert.Equal(end, points[1]);
        Assert.Equal(end, points[3]);

        Point left = points[2], right = points[4];
        Assert.Equal((left - end).Length, (right - end).Length, 6);
        Assert.Equal(left.X, right.X, 6);
        Assert.Equal(-left.Y, right.Y, 6);
        Assert.True(left.X < end.X, "a ponta deve apontar para o fim da seta");
    }

    [Fact]
    public void Arrow_HeadNeverLongerThanHalfTheShaft()
    {
        var end = new Point(20, 0);
        var points = ShapeBuilder.Build(ShapeKind.Arrow, new Point(0, 0), end, 40);

        Assert.True((points[2] - end).Length <= 10 + Tolerance);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 0, true)]
    [InlineData(5, 0, false)]
    [InlineData(0, 5, false)]
    public void IsDegenerate_OnlyForTinyDrags(double dx, double dy, bool expected) =>
        Assert.Equal(expected, ShapeBuilder.IsDegenerate(new Point(10, 10), new Point(10 + dx, 10 + dy)));
}
