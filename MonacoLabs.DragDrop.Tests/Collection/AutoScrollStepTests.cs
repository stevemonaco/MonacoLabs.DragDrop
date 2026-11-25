using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class AutoScrollStepTests
{
    [Theory]
    [InlineData(150, 0)]
    [InlineData(28, 0)]
    [InlineData(272, 0)]
    [InlineData(14, -7)]
    [InlineData(0, -14)]
    [InlineData(286, 7)]
    [InlineData(300, 14)]
    [InlineData(-10, -14)]
    [InlineData(310, 14)]
    public void TheStepGrowsLinearlyFromTheMarginsInnerEdgeToTheViewersEdge(double position, double expected) =>
        Assert.Equal(expected, AutoScroll.Step(position, extent: 300, margin: 28), precision: 9);

    [Theory]
    [InlineData(30, 0)]
    [InlineData(21, 0)]
    [InlineData(10, -7)]
    [InlineData(0, -14)]
    [InlineData(50, 7)]
    public void TheMarginIsAtMostAThirdOfTheExtent(double position, double expected) =>
        Assert.Equal(expected, AutoScroll.Step(position, extent: 60, margin: 28), precision: 9);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(300, 0)]
    [InlineData(0, -5)]
    [InlineData(300, -5)]
    public void AMarginOfZeroOrLessNeverScrolls(double position, double margin) =>
        Assert.Equal(0, AutoScroll.Step(position, extent: 300, margin));
}
