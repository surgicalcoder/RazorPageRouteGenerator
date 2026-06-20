using System;
using System.Collections.Generic;
using System.Linq;

namespace GoLive.Generator.RazorPageRoute.Generator;

public sealed record PlannedRoute(string? Group, string Method, string SlugName, PageRoute Route);

internal static class RouteGrouper
{
    public static List<PlannedRoute> Plan(List<PageRoute> routes, bool enableGrouping)
    {
        var parsed = routes
            .Select(r => (Route: r, Template: Routing.TemplateParser.ParseTemplate(r.Route)))
            .ToList();

        var groupRoots = enableGrouping
            ? DetectGroupRoots(parsed)
            : new HashSet<string>(StringComparer.Ordinal);

        return parsed.Select(p => Build(p.Route, p.Template, enableGrouping, groupRoots)).ToList();
    }

    private static HashSet<string> DetectGroupRoots(
        List<(PageRoute Route, Routing.RouteTemplate Template)> parsed)
    {
        var firstSegments = new List<(string First, int Count)>();

        foreach (var (_, template) in parsed)
        {
            var nonParam = template.Segments.Where(s => !s.IsParameter).Select(s => s.Value).ToList();
            if (nonParam.Count == 0) continue;
            firstSegments.Add((nonParam[0], nonParam.Count));
        }

        return new HashSet<string>(
            firstSegments
                .GroupBy(t => t.First, StringComparer.Ordinal)
                .Where(g => g.Any(t => t.Count > 1))
                .Select(g => g.Key),
            StringComparer.Ordinal);
    }

    private static PlannedRoute Build(PageRoute route, Routing.RouteTemplate template, bool grouping, HashSet<string> groupRoots)
    {
        var nonParam = template.Segments.Where(s => !s.IsParameter).Select(s => s.Value).ToList();

        var slugName = route.Route.Length > 1
            ? Slug.Create(string.Join(".", nonParam), new SlugOptions { ToLower = false })
            : "Home";

        if (string.IsNullOrWhiteSpace(slugName))
        {
            slugName = route.Name;
        }

        string? group = null;
        string method;

        if (nonParam.Count == 0)
        {
            method = "Home";
        }
        else if (nonParam.Count >= 2)
        {
            if (grouping && groupRoots.Contains(nonParam[0]))
            {
                group = nonParam[0];
                method = nonParam[^1];
            }
            else
            {
                method = slugName;
            }
        }
        else
        {
            var seg = nonParam[0];
            if (grouping && groupRoots.Contains(seg) && !template.Segments.Any(s => s.IsParameter))
            {
                group = seg;
                method = "Index";
            }
            else
            {
                method = seg;
            }
        }

        return new PlannedRoute(group, method, slugName, route);
    }
}