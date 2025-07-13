using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class MethodInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(ReturnType)}: {ReturnType}, {nameof(Modifiers)}: {Modifiers}, {nameof(Attributes)}: {Attributes.Count}, {nameof(Parameters)}: {Parameters.Count}";
    }

    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Modifiers { get; set; } = string.Empty;
    public List<AttributeInfo> Attributes { get; set; } = new();
    public List<ParameterInfo> Parameters { get; set; } = new();
}