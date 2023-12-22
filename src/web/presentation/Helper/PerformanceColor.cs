namespace presentation.Helper;

public static class PerformanceColor
{
    public static string GetPerformanceColor(double? value)
    {
        if (value is null or 0)
        {
            return "";
        }

        return $"color: {(value.Value > 0 ? " green" : "red")}";
    }
}