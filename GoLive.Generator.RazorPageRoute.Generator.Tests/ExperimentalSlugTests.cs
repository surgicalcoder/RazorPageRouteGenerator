using GoLive.Generator.RazorPageRoute.Generator.Experimental;
using Xunit;

namespace GoLive.Generator.RazorPageRoute.Generator.Tests;

public class ExperimentalSlugTests
{
    [Fact]
    public void ExperimentalSlug_PascalCasePreserved_WithToLowerFalse()
    {
        var result = Slug.Create("Admin.Tenant.Add", new SlugOptions { ToLower = false });
        Assert.Equal("Admin_Tenant_Add", result);
    }

    [Fact]
    public void ExperimentalSlug_LowercaseInput_AutoCapitalizesWithToLowerFalse()
    {
        var result = Slug.Create("admin.dashboard", new SlugOptions { ToLower = false });
        Assert.Equal("Admin_Dashboard", result);
    }

    [Fact]
    public void ExperimentalSlug_DefaultLowercases()
    {
        var result = Slug.Create("Admin");
        Assert.Equal("admin", result);
    }

    [Fact]
    public void ExperimentalSlug_NullText_ReturnsNull()
    {
        Assert.Null(Slug.Create(null));
    }

    [Fact]
    public void ExperimentalSlug_EmptyString_ReturnsEmpty()
    {
        var result = Slug.Create(string.Empty, new SlugOptions { ToLower = false });
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ExperimentalSlug_PreservesCaseForPermissionsRoute()
    {
        var result = Slug.Create("Permissions", new SlugOptions { ToLower = false });
        Assert.Equal("Permissions", result);
    }

    [Fact]
    public void ExperimentalSlug_PreservesCaseForPluginsRoute()
    {
        var result = Slug.Create("Plugins", new SlugOptions { ToLower = false });
        Assert.Equal("Plugins", result);
    }
}
