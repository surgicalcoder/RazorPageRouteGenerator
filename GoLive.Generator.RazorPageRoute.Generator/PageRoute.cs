using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator
{
    public record PageRoute(string Name, string Route, List<PageRouteQuerystringParameter> QueryString);

    public record PageRouteQuerystringParameter(string Name, string Type);
}