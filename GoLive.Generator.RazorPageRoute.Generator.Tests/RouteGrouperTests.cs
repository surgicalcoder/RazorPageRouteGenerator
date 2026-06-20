using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GoLive.Generator.RazorPageRoute.Generator.Tests;

public class RouteGrouperTests
{
    private static PageRoute Route(string name, string path) => new(name, path, null);

    [Fact]
    public void Plan_NoGrouping_LeavesGroupNullAndUsesSlugName()
    {
        var routes = new List<PageRoute>
        {
            Route("FeatureFlag", "/FeatureFlag/ImportExport"),
            Route("FeatureFlagHome", "/FeatureFlag"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: false);

        Assert.All(planned, p => Assert.Null(p.Group));
        Assert.Equal("FeatureFlag_ImportExport", planned[0].Method);
        Assert.Equal("FeatureFlag", planned[1].Method);
    }

    [Fact]
    public void Plan_Grouping_NestsMultiSegmentUnderFirstSegment()
    {
        var routes = new List<PageRoute>
        {
            Route("ImportExport", "/FeatureFlag/ImportExport"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);

        Assert.Single(planned);
        Assert.Equal("FeatureFlag", planned[0].Group);
        Assert.Equal("ImportExport", planned[0].Method);
    }

    [Fact]
    public void Plan_Grouping_RootPageBecomesIndex()
    {
        var routes = new List<PageRoute>
        {
            Route("ImportExport", "/FeatureFlag/ImportExport"),
            Route("FeatureFlagHome", "/FeatureFlag"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);
        var byRoute = planned.ToDictionary(p => p.Route.Route);

        Assert.Equal("FeatureFlag", byRoute["/FeatureFlag/ImportExport"].Group);
        Assert.Equal("ImportExport", byRoute["/FeatureFlag/ImportExport"].Method);
        Assert.Equal("FeatureFlag", byRoute["/FeatureFlag"].Group);
        Assert.Equal("Index", byRoute["/FeatureFlag"].Method);
    }

    [Fact]
    public void Plan_Grouping_SingleSegmentWithoutSiblings_StaysFlat()
    {
        var routes = new List<PageRoute>
        {
            Route("Counter", "/Counter"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);

        Assert.Null(planned[0].Group);
        Assert.Equal("Counter", planned[0].Method);
    }

    [Fact]
    public void Plan_Grouping_RootRouteIsHome_NoGroup()
    {
        var routes = new List<PageRoute>
        {
            Route("Home", "/"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);

        Assert.Null(planned[0].Group);
        Assert.Equal("Home", planned[0].Method);
    }

    [Fact]
    public void Plan_Grouping_MultipleGroups_EachGetsOwnBucket()
    {
        var routes = new List<PageRoute>
        {
            Route("FlagImport", "/FeatureFlag/ImportExport"),
            Route("FlagHome", "/FeatureFlag"),
            Route("AdminUsers", "/Admin/Users"),
            Route("AdminHome", "/Admin"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);
        var byRoute = planned.ToDictionary(p => p.Route.Route);

        Assert.Equal("FeatureFlag", byRoute["/FeatureFlag/ImportExport"].Group);
        Assert.Equal("ImportExport", byRoute["/FeatureFlag/ImportExport"].Method);
        Assert.Equal("FeatureFlag", byRoute["/FeatureFlag"].Group);
        Assert.Equal("Index", byRoute["/FeatureFlag"].Method);
        Assert.Equal("Admin", byRoute["/Admin/Users"].Group);
        Assert.Equal("Users", byRoute["/Admin/Users"].Method);
        Assert.Equal("Admin", byRoute["/Admin"].Group);
        Assert.Equal("Index", byRoute["/Admin"].Method);
    }

    [Fact]
    public void Plan_Grouping_RootWithParameters_NotTreatedAsIndex()
    {
        var routes = new List<PageRoute>
        {
            Route("FlagEdit", "/FeatureFlag/Edit/{id}"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);

        Assert.Equal("FeatureFlag", planned[0].Group);
        Assert.Equal("Edit", planned[0].Method);
    }

    [Fact]
    public void Plan_Grouping_SlugNamePreserved()
    {
        var routes = new List<PageRoute>
        {
            Route("ImportExport", "/FeatureFlag/ImportExport"),
        };

        var planned = RouteGrouper.Plan(routes, enableGrouping: true);

        Assert.Equal("FeatureFlag_ImportExport", planned[0].SlugName);
    }
}
