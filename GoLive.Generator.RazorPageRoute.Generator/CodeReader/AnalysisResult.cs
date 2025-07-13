using System.Collections.Generic;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class AnalysisResult
{
    public override string ToString()
    {
        return $"{nameof(Namespaces)}: {Namespaces.Count}, {nameof(Classes)}: {Classes.Count}, {nameof(Interfaces)}: {Interfaces.Count}, {nameof(Enums)}: {Enums.Count}";
    }

    public List<NamespaceInfo> Namespaces { get; set; } = new();
    public List<ClassInfo> Classes { get; set; } = new();
    public List<InterfaceInfo> Interfaces { get; set; } = new();
    public List<EnumInfo> Enums { get; set; } = new();
}