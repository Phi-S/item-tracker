using System.Text.RegularExpressions;

namespace infrastructure.Database;

public static partial class SchemaHelper
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var startUnderscores = SnakeCaseDetectRegex().Match(input);
        return startUnderscores + SnakeCaseReplaceRegex().Replace(input, "$1_$2").ToLower();
    }

    [GeneratedRegex(@"^_+")]
    private static partial Regex SnakeCaseDetectRegex();
    
    [GeneratedRegex(@"([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseReplaceRegex();
}