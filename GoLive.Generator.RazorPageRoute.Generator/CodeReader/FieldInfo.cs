using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class FieldInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Type)}: {Type}, {nameof(Modifiers)}: {Modifiers}, {nameof(Attributes)}: {Attributes.Count}, {nameof(HasInitializer)}: {HasInitializer}";
    }

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Modifiers { get; set; } = string.Empty;
    public List<AttributeInfo> Attributes { get; set; } = new();
    public bool HasInitializer { get; set; }
}