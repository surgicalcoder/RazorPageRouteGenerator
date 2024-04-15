using System;

namespace GoLive.Generator.RazorPageRoute.Generator
{
    internal static class RouteConstraint
    {
        public static UrlValueConstraint Parse(string template, string segment, string constraint)
        {
            if (string.IsNullOrEmpty(constraint))
            {
                throw new ArgumentException($"Malformed segment '{segment}' in route '{template}' contains an empty constraint.");
            }

            var targetType = GetTargetType(constraint);
            if (targetType is null || !UrlValueConstraint.TryGetByTargetType(targetType, out var result))
            {
                throw new ArgumentException($"Unsupported constraint '{constraint}' in route '{template}'.");
            }

            return result;
        }

        private static Type? GetTargetType(string constraint) => constraint switch
        {
            "bool" => typeof(bool),
            "datetime" => typeof(DateTime),
            "decimal" => typeof(decimal),
            "double" => typeof(double),
            "float" => typeof(float),
            "guid" => typeof(Guid),
            "int" => typeof(int),
            "long" => typeof(long),
            "nonfile" => typeof(string),
            _ => null,
        };
    }
}