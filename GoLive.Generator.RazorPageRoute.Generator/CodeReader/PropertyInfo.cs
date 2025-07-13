using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class PropertyInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Type)}: {Type}, {nameof(Modifiers)}: {Modifiers}, {nameof(Attributes)}: {Attributes.Count}, {nameof(HasGetter)}: {HasGetter}, {nameof(HasSetter)}: {HasSetter}";
    }

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Modifiers { get; set; } = string.Empty;
    public List<AttributeInfo> Attributes { get; set; } = new();
    public bool HasGetter { get; set; }
    public bool HasSetter { get; set; }
}