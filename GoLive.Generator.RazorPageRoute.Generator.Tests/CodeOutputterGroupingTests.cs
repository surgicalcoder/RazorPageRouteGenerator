using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace GoLive.Generator.RazorPageRoute.Generator.Tests;

public class CodeOutputterGroupingTests
{
    private static string Render(Settings settings, IEnumerable<PageRoute> routes)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"routes_{Guid.NewGuid():N}.cs");
        try
        {
            var s = new Settings
            {
                Namespace = settings.Namespace,
                ClassName = settings.ClassName,
                OutputExtensionMethod = settings.OutputExtensionMethod,
                EnableRouteGrouping = settings.EnableRouteGrouping,
                OutputToFiles = [tempFile],
            };
            CodeOutputter.GenerateOutput(s, routes.ToList());
            return File.ReadAllText(tempFile);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    private static PageRoute Route(string path) => new("Page", path, null);

    [Fact]
    public void Grouping_NestedClassEmittedWithGroupAndMethod()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = true, EnableRouteGrouping = true };
        var routes = new List<PageRoute> { Route("/FeatureFlag/ImportExport") };

        var output = Render(settings, routes);

        Assert.Contains("public static class FeatureFlag", output);
        Assert.Contains("public static void ImportExport(this NavigationManager manager", output);
    }

    [Fact]
    public void Grouping_RootPageBecomesIndex()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = true, EnableRouteGrouping = true };
        var routes = new List<PageRoute>
        {
            Route("/FeatureFlag/ImportExport"),
            Route("/FeatureFlag"),
        };

        var output = Render(settings, routes);

        Assert.Contains("public static void Index(this NavigationManager manager", output);
        Assert.Contains("public static void ImportExport(this NavigationManager manager", output);
    }

    [Fact]
    public void Grouping_StringMethodsRemainFlat()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = true, EnableRouteGrouping = true };
        var routes = new List<PageRoute>
        {
            Route("/FeatureFlag/ImportExport"),
            Route("/FeatureFlag"),
        };

        var output = Render(settings, routes);

        Assert.Contains("public static string FeatureFlag_ImportExport(", output);
        Assert.Contains("public static string FeatureFlag(", output);
    }

    [Fact]
    public void Grouping_DisabledEmitsFlatExtensionMethod()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = true, EnableRouteGrouping = false };
        var routes = new List<PageRoute> { Route("/FeatureFlag/ImportExport") };

        var output = Render(settings, routes);

        Assert.DoesNotContain("public static class FeatureFlag", output);
        Assert.Contains("public static void FeatureFlag_ImportExport(this NavigationManager manager", output);
    }

    [Fact]
    public void Grouping_NestedClassClosedAfterGroupMethods()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = true, EnableRouteGrouping = true };
        var routes = new List<PageRoute>
        {
            Route("/FeatureFlag/ImportExport"),
            Route("/Counter"),
        };

        var output = Render(settings, routes);

        var featureFlagStart = output.IndexOf("public static class FeatureFlag", System.StringComparison.Ordinal);
        var counterStart = output.IndexOf("public static void Counter(this NavigationManager manager", System.StringComparison.Ordinal);

        Assert.True(featureFlagStart > 0);
        Assert.True(counterStart > 0);
        Assert.True(featureFlagStart < counterStart, "FeatureFlag class should appear before Counter method");

        var between = output.Substring(featureFlagStart, counterStart - featureFlagStart);
        Assert.Contains("public static class FeatureFlag", between);
        Assert.Contains("public static void ImportExport", between);
        Assert.DoesNotContain("public static void Counter", between);
    }

    [Fact]
    public void Grouping_RequiresOutputExtensionMethod()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = false, EnableRouteGrouping = true };
        var routes = new List<PageRoute> { Route("/FeatureFlag/ImportExport") };

        var output = Render(settings, routes);

        Assert.DoesNotContain("public static class FeatureFlag", output);
        Assert.DoesNotContain("this NavigationManager", output);
    }

    [Fact]
    public void Grouping_MultipleGroups()
    {
        var settings = new Settings { Namespace = "T", ClassName = "R", OutputExtensionMethod = true, EnableRouteGrouping = true };
        var routes = new List<PageRoute>
        {
            Route("/FeatureFlag/ImportExport"),
            Route("/FeatureFlag"),
            Route("/Admin/Users"),
            Route("/Admin"),
        };

        var output = Render(settings, routes);

        Assert.Contains("public static class FeatureFlag", output);
        Assert.Contains("public static class Admin", output);
        Assert.Contains("public static void Users(this NavigationManager manager", output);
    }
}
