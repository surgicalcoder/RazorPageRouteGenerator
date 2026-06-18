using GoLive.Generator.RazorPageRoute.ExperimentalGenerator;

var projectDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
while (!Directory.Exists(Path.Combine(projectDir, "TestFiles")))
    projectDir = Path.GetDirectoryName(projectDir)!;

var testFiles = Directory.GetFiles(Path.Combine(projectDir, "TestFiles"), "*.razor");

Console.WriteLine("=== File-based extraction ===");
foreach (var file in testFiles)
{
    var fileName = Path.GetFileNameWithoutExtension(file);
    try
    {
        var routes = RouteExtractorExperimental.ExtractRoutesFromFile(file);
        var routeStr = routes.Count == 0 ? "(none)" : string.Join(", ", routes);
        Console.WriteLine($"[{fileName}] -> {routeStr}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{fileName}] ERROR: {ex}");
    }
}

Console.WriteLine("\n=== Content-based extraction ===");
var testContents = new (string Name, string Content)[]
{
    ("Basic", "@page \"/counter\"\n@page \"/counter/{initialCount:int}\"\n\n<h3>Counter</h3>"),
    ("Home", "@page \"/\"\n\n<Welcome />"),
    ("MultiRoute", "@page \"/admin\"\n@page \"/admin/dashboard\"\n@attribute [Authorize]"),
    ("NoPage", "@* no page directive *@\n<h3>Just a component</h3>"),
    ("WithCode", "@page \"/fetchdata\"\n\n@code {\n    private int count;\n}"),
    ("Inherits", "@page \"/inherited\"\n@inherits BaseComponent"),
};

foreach (var (name, content) in testContents)
{
    try
    {
        var routes = RouteExtractorExperimental.ExtractRoutesFromContent(content);
        var routeStr = routes.Count == 0 ? "(none)" : string.Join(", ", routes);
        Console.WriteLine($"[{name}] -> {routeStr}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{name}] ERROR: {ex}");
    }
}
