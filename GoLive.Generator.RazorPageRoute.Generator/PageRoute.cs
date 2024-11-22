using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GoLive.Generator.RazorPageRoute.Generator;

public record PageRoute(string Name, string Route, List<PageRouteQuerystringParameter> QueryString, PageRouteAuth Auth = null);
    
public record PageRouteAuth(List<string> Roles = null, List<string> Policies =null, bool RequiresAuthentication = false, List<PageRouteAuthCustomAuth> CustomAuth = null);
    
public record PageRouteAuthCustomAuth(string Name, Dictionary<string, string> CtorParams, Dictionary<string, string> NamedParams);

public record PageRouteQuerystringParameter(string Name, string Type);