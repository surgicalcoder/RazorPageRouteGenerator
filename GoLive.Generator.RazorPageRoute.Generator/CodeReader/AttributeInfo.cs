using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class AttributeInfo
{
    public override string ToString() => $"{nameof(Name)}: {Name}, {nameof(Arguments)}: {Arguments.Count}, {nameof(NamedArguments)}: {NamedArguments.Count}";

    public string Name { get; set; } = string.Empty;
    public List<string> Arguments { get; set; } = new();
    public Dictionary<string, string> NamedArguments { get; set; } = new();
}