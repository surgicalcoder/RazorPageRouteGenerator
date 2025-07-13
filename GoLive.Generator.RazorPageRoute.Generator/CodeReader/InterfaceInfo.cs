using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class InterfaceInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Modifiers)}: {Modifiers}, {nameof(BaseTypes)}: {BaseTypes.Count}, {nameof(Attributes)}: {Attributes.Count}";
    }

    public string Name { get; set; } = string.Empty;
    public string Modifiers { get; set; } = string.Empty;
    public List<string> BaseTypes { get; set; } = new();
    public List<AttributeInfo> Attributes { get; set; } = new();
}