using Xunit;

namespace GoLive.Generator.RazorPageRoute.Generator.Tests;

public class SlugTests
{
    [Fact]
    public void Create_NullText_ReturnsNull()
    {
        var result = Slug.Create(null);
        Assert.Null(result);
    }

    [Fact]
    public void Create_NullText_WithOptions_ReturnsNull()
    {
        var result = Slug.Create(null, new SlugOptions { ToLower = false });
        Assert.Null(result);
    }

    [Fact]
    public void Create_DefaultOptions_LowercasesInput()
    {
        var result = Slug.Create("FeatureFlag");
        Assert.Equal("featureflag", result);
    }

    [Fact]
    public void Create_DefaultOptions_ReplacesSeparatorsWithUnderscore()
    {
        var result = Slug.Create("Feature Flag");
        Assert.Equal("feature_flag", result);
    }

    [Fact]
    public void Create_PascalCase_WithToLowerFalse_PreservesCase()
    {
        var result = Slug.Create("FeatureFlag", new SlugOptions { ToLower = false });
        Assert.Equal("FeatureFlag", result);
    }

    [Fact]
    public void Create_PascalCaseMultiSegment_WithToLowerFalse_PreservesCase()
    {
        var input = string.Join(".", new[] { "FeatureFlag", "ImportExport" });
        var result = Slug.Create(input, new SlugOptions { ToLower = false });
        Assert.Equal("FeatureFlag_ImportExport", result);
    }

    [Fact]
    public void Create_MixedCase_WithToLowerFalse_PreservesCase()
    {
        var result = Slug.Create("FeatureFlag.ImportExport", new SlugOptions { ToLower = false });
        Assert.Equal("FeatureFlag_ImportExport", result);
    }

    [Fact]
    public void Create_LowercaseInput_WithToLowerFalse_CapitalizesFirstChar()
    {
        var result = Slug.Create("counter", new SlugOptions { ToLower = false });
        Assert.Equal("Counter", result);
    }

    [Fact]
    public void Create_LowercaseAfterSeparator_WithToLowerFalse_CapitalizesSegment()
    {
        var result = Slug.Create("feature_flag", new SlugOptions { ToLower = false });
        Assert.Equal("Feature_Flag", result);
    }

    [Fact]
    public void Create_EmptyString_ReturnsEmpty()
    {
        var result = Slug.Create(string.Empty, new SlugOptions { ToLower = false });
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SlugOptions_DefaultToLower_IsTrue()
    {
        var options = new SlugOptions();
        Assert.True(options.ToLower);
        Assert.False(options.ToUpper);
        Assert.Equal("_", options.Separator);
    }

    [Fact]
    public void Create_ToLowerFalse_NoLowercaseTransform()
    {
        var options = new SlugOptions { ToLower = false };
        var result = Slug.Create("ABCdef", options);
        Assert.Equal("ABCdef", result);
    }
}
