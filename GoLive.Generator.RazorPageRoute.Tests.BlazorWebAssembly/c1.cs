using Microsoft.AspNetCore.Authorization;

namespace GoLive.Generator.RazorPageRoute.Tests.BlazorWebAssembly;

public class CustomAuth : Attribute, IAuthorizeData
{
    public CustomAuth(string[]? roles = null, string[]? policies = null, string[]? authSchemes = null)
    {
        Policy = policies != null ? string.Join(",", policies) : null;
        Roles = roles != null ? string.Join(",", roles) : null;
        AuthenticationSchemes = authSchemes != null ? string.Join(",", authSchemes) : null;
    }

    public string? Policy { get; set; }
    public string? Roles { get; set; }
    public string? AuthenticationSchemes { get; set; }
}

public static class Const
{
    public const string Admin = "Administrator";
}