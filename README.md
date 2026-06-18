# RazorPageRouteGenerator

Generates strongly-typed route helpers for Razor / Blazor pages from `@page` directives. Two generator implementations available.

---

# Classic Generator (NuGet)

**Package:** [`GoLive.Generator.RazorPageRoute`](https://www.nuget.org/packages/GoLive.Generator.RazorPageRoute/)

Reads compiled `.g.cs` files from disk after the Razor source generator runs. Works on .NET 6+.

## Setup

Add `RazorPageRoutes.json` as an `AdditionalFiles` item in your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="RazorPageRoutes.json" />
  <AdditionalFiles Include="**/*.razor" />
</ItemGroup>
```

For .NET 6, disable the Razor source generator (it runs in a conflicting phase):

```xml
<UseRazorSourceGenerator>false</UseRazorSourceGenerator>
```

## Configuration (`RazorPageRoutes.json`)

```json
{
  "Namespace": "MyApp",
  "ClassName": "PageRoutes",
  "OutputToFiles": "PageRoutes.cs",
  "OutputLastCreatedTime": false,
  "OutputExtensionMethod": true
}
```

### Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Namespace` | string | required | Namespace for the generated class |
| `ClassName` | string | required | Class name (e.g. `PageRoutes`) |
| `OutputToFiles` | string[] | `[]` | File paths to write the generated C# (relative to project) |
| `DebugOutputFile` | string | `""` | Optional path for debug output |
| `OutputLastCreatedTime` | bool | `false` | Emit a timestamp comment |
| `OutputExtensionMethod` | bool | `false` | Generate `NavigationManager` extension methods |
| `OutputIAuthorizeData` | bool | `false` | Generate `IAuthorizeData` classes for auth pages |
| `JsonRepresentation` | string[] | `[]` | Export route metadata as JSON to these paths |
| `Invokables` | object | `{}` | JSInvokable configuration (see below) |
| `Auth` | array | `[]` | Custom auth transformer configuration |

### JSInvokables

```json
{
  "Invokables": {
    "Enabled": true,
    "OutputToFiles": "wwwroot/js/routes.js",
    "JSClassName": "dotNetInvokables"
  }
}
```

### Auth Transformers

```json
{
  "Auth": [
    {
      "Attribute": "MyCustomAuth",
      "PolicyTransformer": "ConstructorParameters[\"policy\"]",
      "RolesTransformer": "NamedParameters[\"roles\"]"
    }
  ]
}
```

---

# Experimental Generator

**Project:** `GoLive.Generator.RazorPageRoute.Generator.Experimental`

A complete rewrite that parses `.razor` files directly — no dependency on compiled `.g.cs` files, no disk I/O for route extraction. Targets .NET 10+ only.

## Key Improvements Over Classic

| Area | Classic | Experimental |
|------|---------|-------------|
| Route source | Compiled `.g.cs` files on disk | Raw `.razor` files via `AdditionalFiles` |
| Code extraction | Regex on generated C# | `@code` block brace-matching + `CSharpSyntaxTree` |
| .NET support | 6, 7, 8, 9, 10 | 10+ |
| Razor compiler order | Fragile (must run after Razor gen) | Independent |
| Incremental gen | Partial | Full (tracks file hashes) |
| Features | 7 base features | 15 features (base + 8 new) |

## 15 Features

| # | Feature | Config Flag | Description |
|---|---------|-------------|-------------|
| 1 | **Route Methods** | *always on* | `public static string RouteName(params)` per page |
| 2 | **Reverse Component Map** | `GenerateReverseMap` | `ResolveComponent(route)` switch mapping routes to `typeof(Component)` |
| 3 | **Route Parameters** | *always on* | `[Parameter]` matched to route segments (e.g. `{id:int}` → `int id`) |
| 4 | **Validation Guards** | `GenerateValidation` | Placeholder for generated validation rules |
| 5 | **Route Constants** | `GenerateRouteConstants` | `public const string Home_Route = "/"` |
| 6 | **Extension Methods** | `OutputExtensionMethod` | `NavigationManager.Home()` extension methods |
| 7 | **TryMatch** | `GenerateTryMatch` | `TryMatch(path, out routeName, out params)` for server-side routing |
| 8 | **HTTP File** | `GenerateHttpFile` | Generates `.http` file with all endpoints |
| 9 | **Duplicate Warning** | `EnableDuplicateRouteWarning` | BRD003 diagnostic when same route in multiple files |
| 10 | **Fluent Builder** | `GenerateFluentBuilder` | `Route(Action<RouteQuery> configure)` for query params |
| 11 | **Route Grouping** | `EnableRouteGrouping` | Nested static classes by first path segment |
| 12 | **Rename Tracking** | *always on* | BRD004 diagnostic when route method names change |
| 13 | **JSON Manifest** | `JsonRepresentation` | Export full route metadata as JSON |
| 14 | **Auth Data + JSInvokables** | `OutputIAuthorizeData` / `Invokables.Enabled` | Auth summary comments, `IAuthorizeData` classes, JS invokable map |
| 15 | **Exclude Support** | `ExcludePattern` + `[ExcludeFromRouteGeneration]` | Exclude files by regex pattern or opt-out attribute |

## Setup

Add both `RazorPageRoutes.json` and `**/*.razor` as `AdditionalFiles`:

```xml
<ItemGroup>
  <AdditionalFiles Include="RazorPageRoutes.json" />
  <AdditionalFiles Include="**/*.razor" />
</ItemGroup>
```

## Configuration (`RazorPageRoutes.json`)

```json
{
  "Namespace": "MyApp",
  "ClassName": "PageRoutes",
  "OutputToFiles": "PageRoutes.cs",
  "OutputLastCreatedTime": true,
  "OutputExtensionMethod": true,
  "OutputIAuthorizeData": false,
  "ExcludePattern": "^_.*",
  "EnableDuplicateRouteWarning": true,
  "GenerateRouteConstants": true,
  "GenerateTryMatch": true,
  "GenerateReverseMap": true,
  "GenerateFluentBuilder": true,
  "EnableRouteGrouping": false,
  "GenerateValidation": true,
  "GenerateHttpFile": true,
  "HttpFileOutput": "wwwroot/routes.http"
}
```

### All Options

| Option | Type | Default | Experimental | Description |
|--------|------|---------|--------------|-------------|
| `Namespace` | string | required | ✓ | Namespace for generated class |
| `ClassName` | string | required | ✓ | Class name |
| `OutputToFiles` | string[] | `[]` | ✓ | File paths for generated C# |
| `DebugOutputFile` | string | `""` | ✓ | Debug output path |
| `OutputLastCreatedTime` | bool | `false` | ✓ | Emit timestamp in generated code |
| `OutputExtensionMethod` | bool | `false` | ✓ | Generate `NavigationManager` extensions |
| `OutputIAuthorizeData` | bool | `false` | ✓ | Generate `IAuthorizeData` classes |
| `JsonRepresentation` | string[] | `[]` | ✓ | Export JSON metadata |
| `Invokables` | object | `{}` | ✓ | JSInvokable config |
| `Auth` | array | `[]` | ✓ | Auth transformer config |
| `ExcludePattern` | string | `""` | **new** | Regex to exclude razor files by path |
| `GenerateValidation` | bool | `true` | **new** | Generate validation guard clauses |
| `EnableRouteGrouping` | bool | `false` | **new** | Group route methods into nested classes |
| `GenerateTryMatch` | bool | `false` | **new** | Generate `TryMatch()` method |
| `GenerateReverseMap` | bool | `false` | **new** | Generate component-to-route map |
| `GenerateFluentBuilder` | bool | `false` | **new** | Generate fluent query builder |
| `GenerateRouteConstants` | bool | `true` | **new** | Generate route string constants |
| `EnableDuplicateRouteWarning` | bool | `true` | **new** | Emit BRD003 on duplicate routes |
| `GenerateHttpFile` | bool | `false` | **new** | Generate `.http` test file |
| `HttpFileOutput` | string | `""` | **new** | Output path for `.http` file |

## Generated Output Example

For `Pages/Profile.razor` with:
```razor
@page "/profile"
@page "/profile/{id:int}"

@code {
    [SupplyParameterFromQuery]
    public string Name { get; set; }

    [SupplyParameterFromQuery]
    public int Age { get; set; }
}
```

The generator produces:

```csharp
public const string Profile_Route = "/profile";

public static string Profile(string Name = default, int Age = default)
{
    string url = "/profile";
    // ... query string handling
    return url;
}

public static string Profile(int id, string Name = default, int Age = default)
{
    string url = $"/profile/{id.ToString()}";
    // ... query string handling
    return url;
}

public sealed class ProfileQuery
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public static string Profile(Action<ProfileQuery> configure) { ... }

public static void Profile(this NavigationManager manager, ...) { ... }
public static void Profile(this NavigationManager manager, int id, ...) { ... }
```

The `ResolveComponent` method maps routes to types:
```csharp
"/profile" => typeof(MyApp.Pages.Profile),
"/profile/{id:int}" => typeof(MyApp.Pages.Profile),
```

The `TryMatch` switch parses paths at runtime:
```csharp
case "profile":
    routeName = "Profile";
    return true;
case string p when p.StartsWith("profile/"):
    routeName = "Profile";
    parameters["id"] = path.Substring("profile/".Length);
    return true;
```

## Migrating from Classic to Experimental

1. Replace the NuGet package reference with a project reference to the experimental generator
2. Add `**/*.razor` to `AdditionalFiles` in your `.csproj`
3. Add new config options to `RazorPageRoutes.json` (set new flags to `false` for parity, enable selectively)
4. Remove `UseRazorSourceGenerator` (not needed for .NET 10+)
5. Build — the experimental generator produces the same API surface with additional optional members

## Project Structure

```
├── GoLive.Generator.RazorPageRoute.Generator/       # Classic NuGet generator
│   ├── RazorRouteDiscoveryGenerator.cs
│   ├── Settings.cs
│   └── ...
├── GoLive.Generator.RazorPageRoute.Generator.Experimental/  # Experimental generator
│   ├── RazorRouteDiscoveryGenerator.cs
│   ├── RazorFileParser.cs
│   ├── Settings.cs
│   ├── PageRoute.cs
│   └── Routing/
├── GoLive.Generator.RazorPageRoute.Tests.BlazorWebAssembly10/  # Test project (.NET 10)
│   ├── RazorPageRoutes.json
│   └── Pages/
├── GoLive.Generator.RazorPageRoute.Analyzer/         # F12 rename analyzer
│   ├── RouteMethodAnalyzer.cs
│   └── RouteMethodCodeFixProvider.cs
└── docs/
    ├── experimental-feature-checklist.md
    └── experimental-razor-parser.md
```

## Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| BRD003 | Warning | Duplicate route found in multiple files |
| BRD004 | Warning | Route method was renamed (route cache changed) |
| BRD005 | Warning | (Analyzer) Call to missing generated route method |
