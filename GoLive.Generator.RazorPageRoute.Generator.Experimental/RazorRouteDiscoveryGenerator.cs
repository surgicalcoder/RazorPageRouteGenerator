using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator.Experimental;

[Generator]
public class BlazorRouteDiscoveryGenerator : IIncrementalGenerator
{
    private const string DiagnosticCategory = "BlazorRouteDiscovery";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AdditionalTextsProvider
            .Where(static t => t.Path.EndsWith("RazorPageRoutes.json", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, ct) =>
            {
                var content = text.GetText(ct)?.ToString();
                return (Path: text.Path, Content: content);
            })
            .Where(static t => t.Content != null);

        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, ct) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                return rootNamespace ?? "DefaultNamespace";
            });

        var razorFileContents = context.AdditionalTextsProvider
            .Where(static f => f.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, ct) =>
            {
                var content = file.GetText(ct)?.ToString();
                return (Path: file.Path, Content: content ?? string.Empty);
            })
            .Collect();

        var combined = configProvider
            .Combine(optionsProvider)
            .Combine(razorFileContents)
            .Combine(context.CompilationProvider)
            .Select(static (pair, ct) =>
            {
                var (((config, rootNamespace), razorFiles), compilation) = pair;
                var settings = ParseSettings(config.Path!, config.Content!, rootNamespace);
                if (settings == null) return default(GenerationResult?);

                var pageRoutes = DiscoverRoutes(razorFiles, settings, compilation);
                return new GenerationResult(settings, pageRoutes);
            })
            .Where(static r => r != null);

        context.RegisterSourceOutput(combined, static (spc, result) =>
        {
            var (settings, routes) = result!.Value;

            var source = BuildSource(settings, routes);
            if (!string.IsNullOrWhiteSpace(source))
            {
                spc.AddSource($"{settings.ClassName}.g.cs", source);
            }

            if (settings.JsonRepresentation.Count > 0 && routes.Count > 0)
            {
                var json = JsonSerializer.Serialize(routes.Select(r => new { r.Name, r.Route, r.QueryString, r.Auth, r.Invokables }), new JsonSerializerOptions { WriteIndented = true });
                foreach (var jsonPath in settings.JsonRepresentation)
                {
                    try { File.WriteAllText(jsonPath, json); } catch { }
                }
            }

            if (settings.Invokables.Enabled && routes.Any(r => r.Invokables?.Count > 0))
            {
                GenerateJSInvokable(settings, routes);
            }
        });
    }

    private static Settings? ParseSettings(string configPath, string configContent, string rootNamespace)
    {
        var config = JsonSerializer.Deserialize<Settings>(configContent);
        if (config == null) return null;

        if (string.IsNullOrEmpty(config.Namespace))
            config.Namespace = rootNamespace;

        var configFileDirectory = Path.GetDirectoryName(configPath);

        if (config.OutputToFiles != null && config.OutputToFiles.Count > 0)
        {
            config.OutputToFiles = config.OutputToFiles.Select(r =>
                Path.GetFullPath(Path.Combine(configFileDirectory!, r))).ToList();
        }

        if (config.JsonRepresentation != null && config.JsonRepresentation.Count > 0)
        {
            config.JsonRepresentation = config.JsonRepresentation.Select(r =>
                Path.GetFullPath(Path.Combine(configFileDirectory!, r))).ToList();
        }

        if (config.Invokables != null && config.Invokables.OutputToFiles.Count > 0)
        {
            for (int i = 0; i < config.Invokables.OutputToFiles.Count; i++)
            {
                config.Invokables.OutputToFiles[i] =
                    Path.GetFullPath(Path.Combine(configFileDirectory!, config.Invokables.OutputToFiles[i]));
            }
        }

        return config;
    }

    private static List<PageRoute> DiscoverRoutes(
        ImmutableArray<(string Path, string Content)> razorFiles,
        Settings settings,
        Compilation compilation)
    {
        var result = new List<PageRoute>();

        foreach (var (filePath, content) in razorFiles)
        {
            var parsed = RazorFileParser.Parse(filePath, content);
            if (parsed.Routes.Length == 0) continue;

            var auth = ExtractAuthFromAttributes(parsed.Attributes, settings, compilation);
            var queryParams = ExtractQueryParams(parsed.Members);
            var invokables = ExtractInvokables(parsed.Members, filePath);

            foreach (var route in parsed.Routes)
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                result.Add(new PageRoute(name, route, queryParams ?? [], auth, invokables ?? []));
            }
        }

        result = result.GroupBy(r => r.Route).Select(g => g.First()).ToList();
        return result;
    }

    private static PageRouteAuth? ExtractAuthFromAttributes(
        ImmutableArray<RazorAttribute> attributes,
        Settings settings,
        Compilation compilation)
    {
        var auth = new PageRouteAuth();
        var compilationUsings = GetCompilationUsings(compilation);

        foreach (var attr in attributes)
        {
            var parsedAttr = ParseAttributeText(attr.Text);
            if (parsedAttr == null) continue;

            var attrName = parsedAttr.Name.ToString();

            if (IsAuthorizeAttribute(attrName))
            {
                auth.RequiresAuthentication = true;
                ExtractAuthorizeArguments(parsedAttr, auth, compilation, compilationUsings);
            }
            else
            {
                TryExtractCustomAuth(parsedAttr, settings, auth, compilation, compilationUsings);
            }
        }

        return auth.RequiresAuthentication ? auth : null;
    }

    private static AttributeSyntax? ParseAttributeText(string attributeText)
    {
        var code = $"class _X {{ [{attributeText}] void _Y() {{ }} }}";
        SyntaxTree tree;
        try
        {
            tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));
        }
        catch
        {
            return null;
        }

        var root = tree.GetRoot();
        return root.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault();
    }

    private static bool IsAuthorizeAttribute(string name)
    {
        return name is "Authorize" or "AuthorizeAttribute"
            || name == "Microsoft.AspNetCore.Authorization.AuthorizeAttribute"
            || name.EndsWith(".AuthorizeAttribute");
    }

    private static void ExtractAuthorizeArguments(
        AttributeSyntax attr,
        PageRouteAuth auth,
        Compilation compilation,
        List<string> compilationUsings)
    {
        if (attr.ArgumentList == null) return;

        foreach (var arg in attr.ArgumentList.Arguments)
        {
            if (arg.NameEquals != null)
            {
                var argName = arg.NameEquals.Name.Identifier.Text;
                var value = GetAttributeArgumentValue(arg, compilation, compilationUsings);

                switch (argName)
                {
                    case "Roles":
                        auth.Roles = value.Split(',').Select(r => r.Trim()).ToList();
                        break;
                    case "Policy":
                        auth.Policies = value.Split(',').Select(p => p.Trim()).ToList();
                        break;
                    case "AuthenticationSchemes":
                        auth.AuthenticationSchemes = value.Split(',').Select(s => s.Trim()).ToList();
                        break;
                }
            }
            else
            {
                var value = GetAttributeArgumentValue(arg, compilation, compilationUsings);
                if (!string.IsNullOrEmpty(value))
                {
                    auth.Roles = value.Split(',').Select(r => r.Trim()).ToList();
                }
            }
        }
    }

    private static void TryExtractCustomAuth(
        AttributeSyntax attr,
        Settings settings,
        PageRouteAuth auth,
        Compilation compilation,
        List<string> compilationUsings)
    {
        if (settings.Auth == null) return;

        var attrName = attr.Name.ToString();
        foreach (var customAuth in settings.Auth)
        {
            if (customAuth.Attribute == null) continue;
            if (!customAuth.Attribute.Any(a => a == attrName || a.EndsWith("." + attrName))) continue;

            auth.RequiresAuthentication = true;

            var ctorArgs = new Dictionary<string, string>();
            var namedArgs = new Dictionary<string, string>();

            if (attr.ArgumentList != null)
            {
                for (int i = 0; i < attr.ArgumentList.Arguments.Count; i++)
                {
                    var arg = attr.ArgumentList.Arguments[i];
                    if (arg.NameEquals != null)
                    {
                        namedArgs[arg.NameEquals.Name.Identifier.Text] = GetAttributeArgumentValue(arg, compilation, compilationUsings);
                    }
                    else
                    {
                        ctorArgs[$"arg{i}"] = GetAttributeArgumentValue(arg, compilation, compilationUsings);
                    }
                }
            }

            auth.CustomAuth ??= new List<PageRouteAuthCustomAuth>();
            auth.CustomAuth.Add(new PageRouteAuthCustomAuth(attrName, ctorArgs, namedArgs));
        }
    }

    private static string GetAttributeArgumentValue(
        AttributeArgumentSyntax arg,
        Compilation compilation,
        List<string> compilationUsings)
    {
        if (arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        var exprStr = arg.Expression.ToString();
        if (!string.IsNullOrEmpty(exprStr))
        {
            var resolved = ResolveConst(exprStr, compilationUsings, compilation);
            if (!string.IsNullOrEmpty(resolved))
                return resolved;
        }

        return exprStr;
    }

    private static List<PageRouteQuerystringParameter>? ExtractQueryParams(ImmutableArray<CodeMember> members)
    {
        var queryParams = new List<PageRouteQuerystringParameter>();

        foreach (var member in members)
        {
            if (member.Kind is "property" or "field" &&
                member.AttributeNames.Any(a => a is "SupplyParameterFromQuery" or "SupplyParameterFromQueryAttribute"
                    || a.EndsWith(".SupplyParameterFromQuery") || a.EndsWith(".SupplyParameterFromQueryAttribute")))
            {
                queryParams.Add(new PageRouteQuerystringParameter(member.Name, member.TypeName));
            }
        }

        return queryParams.Count > 0 ? queryParams : null;
    }

    private static List<Invokable>? ExtractInvokables(ImmutableArray<CodeMember> members, string filePath)
    {
        var invokables = new List<Invokable>();
        var className = Path.GetFileNameWithoutExtension(filePath);

        foreach (var member in members)
        {
            if (member.Kind != "method") continue;

            for (int i = 0; i < member.AttributeNames.Length; i++)
            {
                var attrName = member.AttributeNames[i];
                if (!(attrName is "JSInvokable" or "JSInvokableAttribute"
                    || attrName.EndsWith(".JSInvokable") || attrName.EndsWith(".JSInvokableAttribute")))
                    continue;

                var invokableName = ExtractJSInvokableName(member.AttributeTexts[i]);
                invokables.Add(new Invokable($"{className}.{member.Name}", invokableName ?? member.Name));
            }
        }

        return invokables.Count > 0 ? invokables : null;
    }

    private static string? ExtractJSInvokableName(string attrText)
    {
        var parsed = ParseAttributeText(attrText);
        if (parsed?.ArgumentList?.Arguments.Count > 0
            && parsed.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax les
            && les.IsKind(SyntaxKind.StringLiteralExpression)
            && !string.IsNullOrEmpty(les.Token.ValueText))
        {
            return les.Token.ValueText;
        }

        return null;
    }

    private static string ResolveConst(string constName, List<string> namespaceImports, Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                if (!fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                    continue;

                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (symbol == null || !symbol.HasConstantValue)
                        continue;

                    if (symbol.Name == constName || symbol.ToDisplayString() == constName)
                    {
                        if (namespaceImports == null || namespaceImports.Count == 0 ||
                            namespaceImports.Contains(symbol.ContainingNamespace.ToDisplayString()))
                        {
                            return symbol.ConstantValue.ToString();
                        }
                    }

                    if (namespaceImports != null)
                    {
                        foreach (var nsImport in namespaceImports)
                        {
                            if (symbol.ToDisplayString().EndsWith($"{nsImport}.{constName}"))
                            {
                                return symbol.ConstantValue.ToString();
                            }
                        }
                    }
                }
            }
        }

        foreach (var assembly in compilation.References
                     .Select(r => compilation.GetAssemblyOrModuleSymbol(r))
                     .OfType<IAssemblySymbol>())
        {
            foreach (var nsImport in namespaceImports ?? [])
            {
                var ns = assembly.GlobalNamespace.GetNamespaceMembers()
                    .FirstOrDefault(n => n.ToDisplayString() == nsImport);
                if (ns == null) continue;

                foreach (var type in ns.GetTypeMembers())
                {
                    foreach (var member in type.GetMembers())
                    {
                        if (member is IFieldSymbol fs && fs.IsConst &&
                            (fs.Name == constName || fs.ToDisplayString() == constName))
                        {
                            return fs.ConstantValue.ToString();
                        }
                    }
                }
            }
        }

        return string.Empty;
    }

    private static List<string> GetCompilationUsings(Compilation compilation)
    {
        var usings = new List<string>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var root = tree.GetRoot();
            foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                var name = usingDirective.Name?.ToString();
                if (!string.IsNullOrEmpty(name) && !usings.Contains(name))
                    usings.Add(name);
            }
        }
        return usings;
    }

    private static string GetClassNameFromContent(string content)
    {
        var match = Regex.Match(content, @"@(?:page|attribute|code|functions|using|inject|layout)\s");
        return match.Success ? "RouteComponent" : Path.GetFileNameWithoutExtension(content);
    }

    private static string BuildSource(Settings settings, List<PageRoute> routes)
    {
        var source = new SourceStringBuilder();

        if (settings.OutputLastCreatedTime)
        {
            source.AppendLine($"// This file was generated on {DateTime.Now:R}");
        }

        source.AppendLine("// <auto-generated/>");
        source.AppendLine("#pragma warning disable CS1591");
        source.AppendLine("#nullable enable");
        source.AppendLine();
        source.AppendLine("using System;");
        source.AppendLine("using System.Net.Http;");
        source.AppendLine("using System.Threading.Tasks;");
        source.AppendLine("using System.Net.Http.Json;");
        source.AppendLine("using System.Collections.Generic;");

        if (settings.OutputIAuthorizeData)
        {
            source.AppendLine("using Microsoft.AspNetCore.Authorization;");
        }

        if (settings.OutputIAuthorizeData || settings.OutputExtensionMethod)
        {
            source.AppendLine("using Microsoft.AspNetCore.Components;");
        }

        source.AppendLine($"namespace {settings.Namespace}");
        source.AppendOpenCurlyBracketLine();
        source.AppendLine($"public static partial class {settings.ClassName}");
        source.AppendOpenCurlyBracketLine();

        if (routes.Count == 0)
        {
            source.AppendCloseCurlyBracketLine();
            source.AppendCloseCurlyBracketLine();
            source.AppendLine("#pragma warning restore CS1591");
            return source.ToString();
        }

        List<string> completedAuthData = [];
        foreach (var pageRoute in routes)
        {
            AppendRoute(source, pageRoute, settings, completedAuthData);
        }

        source.AppendCloseCurlyBracketLine();
        source.AppendCloseCurlyBracketLine();
        source.AppendLine("#pragma warning restore CS1591");

        return source.ToString();
    }

    private static void AppendRoute(SourceStringBuilder source, PageRoute pageRoute, Settings settings, List<string> completedAuthData)
    {
        var routeTemplate = Routing.TemplateParser.ParseTemplate(pageRoute.Route);

        var slugName = pageRoute.Route.Length > 1
            ? Slug.Create(string.Join(".", routeTemplate.Segments.Where(f => !f.IsParameter).Select(f => f.Value)))
            : "Home";

        if (string.IsNullOrWhiteSpace(slugName))
        {
            slugName = pageRoute.Name;
        }

        var routeSegments = routeTemplate.Segments
            .Where(e => e.IsParameter)
            .Select(segment =>
            {
                var constraint = segment.Constraints.Any()
                    ? segment.Constraints.FirstOrDefault().GetConstraintType()
                    : null;

                if (constraint == null)
                    return $"string {segment.Value}";

                return segment.IsOptional
                    ? $"{constraint.FullName}? {segment.Value}"
                    : $"{constraint.FullName} {segment.Value}";
            }).ToList();

        if (pageRoute.QueryString is { Count: > 0 })
        {
            routeSegments.AddRange(pageRoute.QueryString
                .Select(prqp => $"{prqp.Type} {prqp.Name} = default"));
        }

        var parameterString = string.Join(", ", routeSegments);

        AppendRouteMethod(source, slugName, parameterString, routeTemplate, pageRoute);

        if (settings.OutputExtensionMethod)
        {
            AppendExtensionMethod(source, slugName, parameterString, routeTemplate, pageRoute, settings);
            if (!completedAuthData.Contains(slugName))
            {
                completedAuthData.Add(slugName);
                AppendAuthDataClass(source, slugName, pageRoute, settings);
            }
        }
    }

    private static void AppendRouteMethod(SourceStringBuilder source, string slugName, string parameterString,
        Routing.RouteTemplate routeTemplate, PageRoute pageRoute)
    {
        source.AppendLine($"public static string {slugName} ({parameterString})");
        source.AppendOpenCurlyBracketLine();

        if (routeTemplate.Segments.Any(e => e.IsParameter))
        {
            source.AppendIndent();
            source.Append("string url = $\"", false);
            foreach (var seg in routeTemplate.Segments)
            {
                source.Append(seg.IsParameter ? $"/{{{seg.Value}.ToString()}}" : $"/{seg.Value}", false);
            }
            source.Append("\";\n", false);
        }
        else
        {
            source.AppendLine($"string url = \"{pageRoute.Route}\";");
        }

        if (pageRoute.QueryString is { Count: > 0 })
        {
            source.AppendLine("Dictionary<string, string> queryString=new();");
            foreach (var prqp in pageRoute.QueryString)
            {
                if (prqp.Type == "System.String" || prqp.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    source.AppendLine($"if (!string.IsNullOrWhiteSpace({prqp.Name})) ");
                }
                else
                {
                    source.AppendLine($"if ({prqp.Name} != default) ");
                }
                source.AppendOpenCurlyBracketLine();
                source.AppendLine($"queryString.Add(\"{prqp.Name}\", {prqp.Name}.ToString());");
                source.AppendCloseCurlyBracketLine();
            }
            source.AppendLine("");
            source.AppendLine("url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);");
        }

        source.AppendLine("return url;");
        source.AppendCloseCurlyBracketLine();
    }

    private static void AppendExtensionMethod(SourceStringBuilder source, string slugName, string parameterString,
        Routing.RouteTemplate routeTemplate, PageRoute pageRoute, Settings settings)
    {
        if (pageRoute.Auth is { RequiresAuthentication: true })
        {
            source.AppendLine("/// <summary>");
            source.AppendLine($"/// Page Requires Authentication{(pageRoute.Auth.CustomAuth != null ? ", Custom Authentication Provider (CustomAuth)" : "")}");
            if (pageRoute.Auth.Roles is { Count: > 0 })
                source.AppendLine($"/// Roles: {string.Join(", ", pageRoute.Auth.Roles)}");
            if (pageRoute.Auth.Policies is { Count: > 0 })
                source.AppendLine($"/// Policies: {string.Join(", ", pageRoute.Auth.Policies)}");
            if (pageRoute.Auth.CustomAuth != null)
            {
                foreach (var ca in pageRoute.Auth.CustomAuth)
                {
                    source.AppendLine($"/// Custom Authentication ProviderName: {ca.Name}");
                    source.AppendLine($"/// Custom Auth Constructor Params: {string.Join(", ", ca.CtorParams)}");
                    source.AppendLine($"/// Custom Auth Named Params: {string.Join(", ", ca.NamedParams)}");
                }
            }
            source.AppendLine("/// </summary>");
        }

        if (string.IsNullOrWhiteSpace(parameterString))
        {
            source.AppendLine($"public static void {slugName} (this NavigationManager manager, bool forceLoad = false, bool replace=false)");
        }
        else
        {
            source.AppendLine($"public static void {slugName} (this NavigationManager manager, {parameterString}, bool forceLoad = false, bool replace=false)");
        }

        source.AppendOpenCurlyBracketLine();

        if (routeTemplate.Segments.Any(e => e.IsParameter))
        {
            source.AppendIndent();
            source.Append("string url = $\"", false);
            foreach (var seg in routeTemplate.Segments)
            {
                source.Append(seg.IsParameter ? $"/{{{seg.Value}.ToString()}}" : $"/{seg.Value}", false);
            }
            source.Append("\";\n", false);
        }
        else
        {
            source.AppendLine($"string url = \"{pageRoute.Route}\";");
        }

        if (pageRoute.QueryString is { Count: > 0 })
        {
            source.AppendLine("Dictionary<string, string> queryString=new();");
            foreach (var prqp in pageRoute.QueryString)
            {
                if (prqp.Type == "System.String" || prqp.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    source.AppendLine($"if (!string.IsNullOrWhiteSpace({prqp.Name})) ");
                }
                else
                {
                    source.AppendLine($"if ({prqp.Name} != default) ");
                }
                source.AppendOpenCurlyBracketLine();
                source.AppendLine($"queryString.Add(\"{prqp.Name}\", {prqp.Name}.ToString());");
                source.AppendCloseCurlyBracketLine();
            }
            source.AppendLine("");
            source.AppendLine("url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);");
        }

        source.AppendLine("manager.NavigateTo(url, forceLoad, replace);");
        source.AppendCloseCurlyBracketLine();
    }

    private static void AppendAuthDataClass(SourceStringBuilder source, string slugName, PageRoute pageRoute, Settings settings)
    {
        if (pageRoute.Auth is not { RequiresAuthentication: true } || !settings.OutputIAuthorizeData)
            return;

        source.AppendLine($"public class {slugName}_AuthData : IAuthorizeData");
        source.AppendOpenCurlyBracketLine();

        if (pageRoute.Auth.CustomAuth is { Count: > 0 })
        {
            var authItem = pageRoute.Auth.CustomAuth.FirstOrDefault();
            var authCfg = settings.Auth.FirstOrDefault(a =>
                a.Attribute != null && a.Attribute.Contains(authItem.Name));

            if (authCfg != null)
            {
                var policyList = EvaluateCode(authCfg.PolicyTransformer, authItem);
                var rolesList = EvaluateCode(authCfg.RolesTransformer, authItem);
                var authSchemesList = EvaluateCode(authCfg.AuthenticationSchemeTransformer, authItem);

                source.AppendLine($"public string Policy {{ get; set; }} = {(policyList?.Count > 0 ? $"\"{string.Join(", ", policyList.Select(QuoteLiteral))}\"" : "String.Empty")}; ");
                source.AppendLine($"public string Roles {{ get; set; }} = {(rolesList?.Count > 0 ? $"\"{string.Join(", ", rolesList.Select(QuoteLiteral))}\"" : "String.Empty")}; ");
                source.AppendLine($"public string AuthenticationSchemes {{ get; set; }} = {(authSchemesList?.Count > 0 ? $"\"{string.Join(", ", authSchemesList.Select(QuoteLiteral))}\"" : "String.Empty")}; ");
            }
        }
        else
        {
            source.AppendLine($"public string Policy {{ get; set; }} = {(pageRoute.Auth.Policies?.Count > 0 ? $"\"{string.Join(",", pageRoute.Auth.Policies)}\"" : "String.Empty")};");
            source.AppendLine($"public string Roles {{ get; set; }} = {(pageRoute.Auth.Roles?.Count > 0 ? $"\"{string.Join(",", pageRoute.Auth.Roles)}\"" : "String.Empty")}; ");
            source.AppendLine($"public string AuthenticationSchemes {{ get; set; }} = {(pageRoute.Auth.AuthenticationSchemes?.Count > 0 ? $"\"{string.Join(", ", pageRoute.Auth.AuthenticationSchemes)}\"" : "String.Empty")}; ");
        }

        source.AppendCloseCurlyBracketLine();
    }

    private static List<string>? EvaluateCode(string? transformer, PageRouteAuthCustomAuth authItem)
    {
        if (string.IsNullOrWhiteSpace(transformer))
            return null;

        try
        {
            var evaluator = new Data.Eval.Evaluator(transformer);
            evaluator.AddUsing("System.Collections.Generic");
            evaluator["ConstructorParameters"] = authItem.CtorParams;
            evaluator["NamedParameters"] = authItem.NamedParams;
            return evaluator.Eval<List<string>>();
        }
        catch
        {
            return null;
        }
    }

    private static string QuoteLiteral(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static void GenerateJSInvokable(Settings settings, List<PageRoute> routes)
    {
        var invokables = routes
            .Where(pr => pr.Invokables != null && pr.Invokables.Any())
            .SelectMany(pr => pr.Invokables)
            .ToList();

        if (invokables.Count == 0) return;

        var jsBuilder = new StringBuilder();
        jsBuilder.AppendLine($"const {settings.Invokables.JSClassName} = {{");

        foreach (var invoke in invokables)
        {
            jsBuilder.AppendLine($"{invoke.InvokableName.Replace(".", "_")}: \"{invoke.MethodName}\", ");
        }

        jsBuilder.AppendLine("};");

        if (settings.Invokables.OutputToFiles.Count > 0)
        {
            foreach (var outputPath in settings.Invokables.OutputToFiles)
            {
                try { File.WriteAllText(outputPath, jsBuilder.ToString()); } catch { }
            }
        }
    }
}

internal record struct GenerationResult(Settings Settings, List<PageRoute> Routes);
