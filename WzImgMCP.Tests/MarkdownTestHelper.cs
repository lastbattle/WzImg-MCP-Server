using System.Text.RegularExpressions;

namespace WzImgMCP.Tests;

public static class MarkdownTestHelper
{
    public static void AssertSuccess(string markdown)
        => Assert.Contains("- success: true", markdown, StringComparison.OrdinalIgnoreCase);

    public static void AssertFailure(string markdown)
    {
        Assert.DoesNotContain("- success: true", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("error", markdown, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertHasKey(string markdown, string key)
        => Assert.Matches(new Regex(@"^\s*-\s*" + Regex.Escape(key) + @"\s*:", RegexOptions.Multiline | RegexOptions.IgnoreCase), markdown);

    public static string? GetFirstValue(string markdown, string key)
    {
        var m = Regex.Match(markdown, @"^\s*-\s*" + Regex.Escape(key) + @"\s*:\s*(.*?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static int GetFirstInt(string markdown, string key, int fallback = 0)
    {
        var v = GetFirstValue(markdown, key);
        return int.TryParse(v, out var i) ? i : fallback;
    }

    public static List<string> GetAllValues(string markdown, string key)
    {
        return Regex.Matches(markdown, @"^\s*-\s*" + Regex.Escape(key) + @"\s*:\s*(.*?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .ToList();
    }
}
