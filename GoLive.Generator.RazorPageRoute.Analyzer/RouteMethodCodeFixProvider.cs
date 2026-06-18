#nullable enable
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Analyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RouteMethodCodeFixProvider))]
[Shared]
public class RouteMethodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [RouteMethodAnalyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation is null) return;

        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess is null) return;

        var oldName = memberAccess.Name.Identifier.Text;

        var newName = ResolveNewName(context, oldName);
        if (string.IsNullOrEmpty(newName)) return;

        var title = $"Rename to '{newName}'";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => RenameMethodAsync(context.Document, memberAccess, newName, ct),
                equivalenceKey: title),
            diagnostic);
    }

    private static string? ResolveNewName(CodeFixContext context, string oldName)
    {
        var compilation = context.Document.Project.GetCompilationAsync().GetAwaiter().GetResult();
        if (compilation is null) return null;

        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(c => c.Identifier.Text is "PageRoutes" or "PageRoutesExtensions");

            foreach (var classDecl in classDecls)
            {
                var model = compilation.GetSemanticModel(tree);
                var typeSymbol = model.GetDeclaredSymbol(classDecl);
                if (typeSymbol is null) continue;

                foreach (var member in typeSymbol.GetMembers())
                {
                    if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: true, Name: var name })
                    {
                        var similarity = ComputeSimilarity(oldName, name);
                        if (similarity >= 3) return name;
                    }
                }
            }
        }

        return null;
    }

    private static async Task<Document> RenameMethodAsync(
        Document document,
        MemberAccessExpressionSyntax memberAccess,
        string newName,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null) return document;

        var newMemberAccess = memberAccess.WithName(SyntaxFactory.IdentifierName(newName));
        root = root.ReplaceNode(memberAccess, newMemberAccess);

        return document.WithSyntaxRoot(root);
    }

    private static int ComputeSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;

        var len = a.Length < b.Length ? a.Length : b.Length;
        var matches = 0;
        for (var i = 0; i < len; i++)
        {
            if (char.ToLowerInvariant(a[i]) == char.ToLowerInvariant(b[i]))
                matches++;
        }

        return matches;
    }
}
