using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace GoLive.Generator.RazorPageRoute.ExperimentalGenerator;

public static class RouteExtractorExperimental
{
    private static readonly Type RouteAttributeExtensionNodeType;
    private static readonly PropertyInfo TemplateProperty;

    static RouteExtractorExperimental()
    {
        var asm = typeof(DocumentIntermediateNode).Assembly;
        RouteAttributeExtensionNodeType = asm.GetType(
            "Microsoft.AspNetCore.Razor.Language.Components.RouteAttributeExtensionNode")
            ?? throw new InvalidOperationException("Could not find RouteAttributeExtensionNode type");

        TemplateProperty = RouteAttributeExtensionNodeType.GetProperty("Template")
            ?? throw new InvalidOperationException("Could not find Template property");
    }

    public static List<string> ExtractRoutesFromFile(string razorFilePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(razorFilePath));
        var fileSystem = RazorProjectFileSystem.Create(dir!);
        var engine = RazorProjectEngine.Create(
            RazorConfiguration.Default,
            fileSystem,
            builder => { });

        var projectItem = fileSystem.GetItem(razorFilePath, FileKinds.Component);
        var codeDoc = engine.ProcessDeclarationOnly(projectItem);
        var docNode = codeDoc.GetDocumentIntermediateNode();

        return ExtractPageRoutes(docNode);
    }

    public static List<string> ExtractRoutesFromContent(string content)
    {
        var sourceDoc = RazorSourceDocument.Create(content, "test.razor");
        var fileSystem = RazorProjectFileSystem.Create(".");
        var engine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder => { });
        var codeDoc = engine.ProcessDeclarationOnly(
            sourceDoc,
            FileKinds.Component,
            Array.Empty<RazorSourceDocument>(),
            Array.Empty<TagHelperDescriptor>());
        var docNode = codeDoc.GetDocumentIntermediateNode();

        return ExtractPageRoutes(docNode);
    }

    private static List<string> ExtractPageRoutes(DocumentIntermediateNode docNode)
    {
        var routes = new List<string>();
        CollectRoutes(docNode, routes);
        return routes;
    }

    private static void CollectRoutes(IntermediateNode node, List<string> routes)
    {
        if (RouteAttributeExtensionNodeType.IsInstanceOfType(node))
        {
            var value = TemplateProperty.GetValue(node);
            if (value != null)
            {
                var template = value is object[] arr
                    ? string.Concat(arr.Select(o => o?.ToString()))
                    : value.ToString();
                if (template != null)
                    routes.Add(template);
            }
        }

        foreach (var child in node.Children)
        {
            CollectRoutes(child, routes);
        }
    }
}
