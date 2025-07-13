using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class EnumInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Modifiers)}: {Modifiers}, {nameof(Attributes)}: {Attributes.Count}, {nameof(Members)}: {Members.Count}";
    }

    public string Name { get; set; } = string.Empty;
    public string Modifiers { get; set; } = string.Empty;
    public List<AttributeInfo> Attributes { get; set; } = new();
    public List<string> Members { get; set; } = new();
}