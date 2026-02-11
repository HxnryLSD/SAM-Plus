#if MANAGER_TESTS
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SAM.Manager;
using Xunit;

namespace SAM.Core.Tests.Manager;

public class ConvertersTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void BoolNegationConverter_Convert_TogglesValue(bool input, bool expected)
    {
        var converter = new BoolNegationConverter();
        var result = converter.Convert(input, typeof(bool), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void BoolNegationConverter_ConvertBack_TogglesValue(bool input, bool expected)
    {
        var converter = new BoolNegationConverter();
        var result = converter.ConvertBack(input, typeof(bool), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    public void BoolToVisibilityConverter_Convert_MapsVisibility(bool input, Visibility expected)
    {
        var converter = new BoolToVisibilityConverter();
        var result = converter.Convert(input, typeof(Visibility), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Collapsed, false)]
    public void BoolToVisibilityConverter_ConvertBack_MapsBool(Visibility input, bool expected)
    {
        var converter = new BoolToVisibilityConverter();
        var result = converter.ConvertBack(input, typeof(bool), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("value", Visibility.Visible)]
    [InlineData("  ", Visibility.Collapsed)]
    [InlineData("", Visibility.Collapsed)]
    public void StringToVisibilityConverter_Convert_MapsVisibility(string input, Visibility expected)
    {
        var converter = new StringToVisibilityConverter();
        var result = converter.Convert(input, typeof(Visibility), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, Visibility.Collapsed)]
    [InlineData(false, Visibility.Visible)]
    public void InverseBoolToVisibilityConverter_Convert_MapsVisibility(bool input, Visibility expected)
    {
        var converter = new InverseBoolToVisibilityConverter();
        var result = converter.Convert(input, typeof(Visibility), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(Visibility.Visible, false)]
    [InlineData(Visibility.Collapsed, true)]
    public void InverseBoolToVisibilityConverter_ConvertBack_MapsBool(Visibility input, bool expected)
    {
        var converter = new InverseBoolToVisibilityConverter();
        var result = converter.ConvertBack(input, typeof(bool), null, "");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BoolToGlyphConverter_Convert_UsesTrueGlyph()
    {
        var converter = new BoolToGlyphConverter
        {
            TrueGlyph = "T",
            FalseGlyph = "F"
        };

        var result = converter.Convert(true, typeof(string), null, "");
        Assert.Equal("T", result);
    }

    [Fact]
    public void BoolToGlyphConverter_Convert_UsesFalseGlyph()
    {
        var converter = new BoolToGlyphConverter
        {
            TrueGlyph = "T",
            FalseGlyph = "F"
        };

        var result = converter.Convert(false, typeof(string), null, "");
        Assert.Equal("F", result);
    }

    [Fact]
    public void BoolToColorConverter_Convert_TrueReturnsLimeGreen()
    {
        var converter = new BoolToColorConverter();
        var result = converter.Convert(true, typeof(SolidColorBrush), null, "");
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.LimeGreen, brush.Color);
    }

    [Fact]
    public void BoolToColorConverter_Convert_FalseReturnsGray()
    {
        var converter = new BoolToColorConverter();
        var result = converter.Convert(false, typeof(SolidColorBrush), null, "");
        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.Gray, brush.Color);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    public void CountToVisibilityConverter_Convert_MapsVisibility(int count, bool visible)
    {
        var converter = new CountToVisibilityConverter();
        var expected = visible ? Visibility.Visible : Visibility.Collapsed;
        var result = converter.Convert(count, typeof(Visibility), null, "");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, false)]
    public void CountToVisibilityConverter_Convert_InvertedMapsVisibility(int count, bool visible)
    {
        var converter = new CountToVisibilityConverter { Invert = true };
        var expected = visible ? Visibility.Visible : Visibility.Collapsed;
        var result = converter.Convert(count, typeof(Visibility), null, "");
        Assert.Equal(expected, result);
    }
}
#endif
