namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class NamespaceInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(FullName)}: {FullName}";
    }

    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}