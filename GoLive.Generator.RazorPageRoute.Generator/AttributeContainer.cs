using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator;

public class AttributeContainer
{
    public string Name { get; set; }
    public List<object?> Values { get; set; } = new();
}