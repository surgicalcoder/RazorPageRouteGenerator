using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class ClassInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Modifiers)}: {Modifiers}, {nameof(BaseTypes)}: {BaseTypes.Count}, {nameof(Attributes)}: {Attributes.Count}, {nameof(Properties)}: {Properties.Count}, {nameof(Fields)}: {Fields.Count}, {nameof(Methods)}: {Methods.Count}";
    }

    public string Name { get; set; } = string.Empty;
    public string Modifiers { get; set; } = string.Empty;
    public List<string> BaseTypes { get; set; } = new();
    public List<AttributeInfo> Attributes { get; set; } = new();
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<FieldInfo> Fields { get; set; } = new();
    public List<MethodInfo> Methods { get; set; } = new();
}