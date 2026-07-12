using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.Experimental;

public record PageRoute(
    string Name,
    string Route,
    List<PageRouteQuerystringParameter> QueryString,
    PageRouteAuth Auth = null,
    List<Invokable> Invokables = null,
    List<RouteParameterInfo> RouteParams = null,
    List<ValidationRule> Validations = null)
{
    public string FullTypeName { get; init; } = Name;
    public string RootNamespace { get; init; } = Name;
}

public record RouteParameterInfo(
    string Name,
    string Type,
    bool HasParameterAttr,
    string Constraint);

public record ValidationRule(
    string PropertyName,
    string RuleType,
    string Param,
    string Message);

public class PageRouteAuth
{
    public List<string> Roles { get; set; }
    public List<string> Policies { get; set; }
    public List<string> AuthenticationSchemes { get; set; }
    public bool RequiresAuthentication { get; set; }
    public List<PageRouteAuthCustomAuth> CustomAuth { get; set; }
}

public record Invokable(string MethodName, string InvokableName);

public record PageRouteAuthCustomAuth(string Name, Dictionary<string, string> CtorParams, Dictionary<string, string> NamedParams);

public record PageRouteQuerystringParameter(string Name, string Type);
