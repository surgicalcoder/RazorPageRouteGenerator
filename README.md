This will generate you a class, that has strongly typed methods, that will represent Razor and Blazor Page routes. You can then use this to navigate to those URLs. Also supports some forms of parameters in the URLs.

 # How to use

Firstly, add the project from Nuget - [GoLive.Generator.RazorPageRoute](https://www.nuget.org/packages/GoLive.Generator.RazorPageRoute/), then add an AdditionalFile in your .csproj named "RazorPageRoutes.json", like so:

```
<ItemGroup>
     <AdditionalFiles Include="RazorPageRoutes.json" />
</ItemGroup>
```

Once that's done, add the settings file and change as required:


```
{
  "Namespace": "GoLive.Generator.RazorPageRoute.Tests.BlazorWebAsssembly",
  "ClassName": "PageRoutes",
  "OutputToFile": "PageRoutes.cs",
  "OutputLastCreatedTime": false
}
```

For `OutputFile` the path will be calculated as relative, so you can put in `..\WebAssembly\File.cs`