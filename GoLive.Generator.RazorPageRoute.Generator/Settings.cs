using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoLive.Generator.RazorPageRoute.Generator;

public class Settings
{
    public string Namespace { get; set; }
    public string ClassName { get; set; }

    [JsonConverter(typeof(StringOrArrayJsonConverter))]
    public List<string> OutputToFiles { get; set; } = [];

    public string DebugOutputFile { get; set; }

    public bool OutputLastCreatedTime { get; set; }
        
    public bool OutputExtensionMethod { get; set; }

    public bool EnableRouteGrouping { get; set; }

    public Settings_JSInvokables Invokables { get; set; } = new();

    public List<Settings_Auth> Auth { get; set; } = [];
    public bool OutputIAuthorizeData { get; set; }
    
    [JsonConverter(typeof(StringOrArrayJsonConverter))]
    public List<string> JsonRepresentation { get; set; } = [];
}

public class Settings_JSInvokables
{
    public bool Enabled { get; set; }
    
    [JsonConverter(typeof(StringOrArrayJsonConverter))]
    public List<string> OutputToFiles { get; set; } = [];
    
    public string JSClassName { get; set; }
}

public class Settings_Auth
{
    [JsonConverter(typeof(StringOrArrayJsonConverter))]
    public List<string> Attribute { get; set; }
    public string PolicyTransformer { get; set; }
    public string RolesTransformer { get; set; }
    public string AuthenticationSchemeTransformer { get; set; }
}