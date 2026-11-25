using Xunit;

namespace MonacoLabs.DragDrop.Tests;

public class ThrowIfTests
{
    [Fact]
    public void TrueThrowsOnlyForTrue()
    {
        Assert.Throws<ArgumentException>(() => ThrowIf.True(true));
        ThrowIf.True(false);
    }

    [Fact]
    public void FalseThrowsOnlyForFalse()
    {
        Assert.Throws<ArgumentException>(() => ThrowIf.False(false));
        ThrowIf.False(true);
    }

    [Fact]
    public void TrueNamesTheConditionAsTrue()
    {
        var flag = true;

        Assert.Equal("flag was true", Assert.Throws<ArgumentException>(() => ThrowIf.True(flag)).Message);
    }

    [Fact]
    public void FalseNamesTheConditionAsFalse()
    {
        var flag = false;

        Assert.Equal("flag was false", Assert.Throws<ArgumentException>(() => ThrowIf.False(flag)).Message);
    }
    [Fact]
    public void NullNamesTheNullExpression()
    {
        object? value = null;

        Assert.Equal("value was null", Assert.Throws<ArgumentException>(() => ThrowIf.Null(value)).Message);
        Assert.Equal("Context: value was null", Assert.Throws<ArgumentException>(() => ThrowIf.Null(value, "Context")).Message);
        ThrowIf.Null(new object());
    }

    [Fact]
    public void NotNullNamesTheNonNullExpression()
    {
        object? value = new();

        Assert.Equal("value was not null", Assert.Throws<ArgumentException>(() => ThrowIf.NotNull(value)).Message);
        Assert.Equal("Context: value was not null", Assert.Throws<ArgumentException>(() => ThrowIf.NotNull(value, "Context")).Message);
        ThrowIf.NotNull(null);
    }
}
