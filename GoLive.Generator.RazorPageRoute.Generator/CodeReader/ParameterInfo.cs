namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public class ParameterInfo
{
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Type)}: {Type}, {nameof(HasDefaultValue)}: {HasDefaultValue}";
    }

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool HasDefaultValue { get; set; }
}