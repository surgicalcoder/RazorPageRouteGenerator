using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GoLive.Generator.RazorPageRoute.Generator;

public record PageRoute(string Name, string Route, List<PageRouteQuerystringParameter> QueryString, PageRouteAuth Auth = null);
    
public class PageRouteAuth{
    public List<string> Roles { get; set; }
    public List<string> Policies { get; set; }
    public bool RequiresAuthentication { get; set; }
    public List<PageRouteAuthCustomAuth> CustomAuth { get; set; }
}

public record PageRouteAuthCustomAuth(string Name, Dictionary<string, string> CtorParams, Dictionary<string, string> NamedParams);

public record PageRouteQuerystringParameter(string Name, string Type);