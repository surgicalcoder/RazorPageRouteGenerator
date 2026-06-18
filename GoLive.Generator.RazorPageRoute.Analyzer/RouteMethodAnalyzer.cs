using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable
namespace GoLive.Generator.RazorPageRoute.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RouteMethodAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "BRD005";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Route method call may be stale",
        "Route method '{0}' not found. Did you mean '{1}'?",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Detects calls to generated route methods that no longer exist after a route rename.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess) return;

        var methodName = memberAccess.Name.Identifier.Text;
        var semanticModel = context.SemanticModel;

        var targetType = semanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
        if (targetType == null) return;

        if (targetType.Name is not ("PageRoutes" or "PageRoutesExtensions")) return;

        var routeMethods = GetRouteMethods(targetType);
        if (routeMethods.Contains(methodName)) return;

        var closest = FindClosestMatch(methodName, routeMethods);
        if (closest == null) return;

        var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), methodName, closest);
        context.ReportDiagnostic(diagnostic);
    }

    private static HashSet<string> GetRouteMethods(ITypeSymbol type)
    {
        var methods = new HashSet<string>();
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: true } method)
            {
                methods.Add(method.Name);
            }
        }

        if (type.Name == "PageRoutes")
        {
            var ns = type.ContainingNamespace;
            if (ns != null)
            {
                var extType = ns.GetTypeMembers("PageRoutesExtensions").FirstOrDefault();
                if (extType != null)
                {
                    foreach (var member in extType.GetMembers())
                    {
                        if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: true, Name: var name })
                            methods.Add(name);
                    }
                }
            }
        }

        return methods;
    }

    private static string? FindClosestMatch(string name, HashSet<string> candidates)
    {
        string? best = null;
        var bestScore = 0;

        foreach (var candidate in candidates)
        {
            var score = ComputeSimilarity(name, candidate);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return bestScore >= 3 ? best : null;
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
