using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CIEL.Reconciliation.Services;

public static class NameTools
{
    private static readonly HashSet<string> Titles = new(StringComparer.OrdinalIgnoreCase)
    { "mr", "mrs", "ms", "miss", "dr", "prof", "sir" };

    public static string Normalize(string? value)
    {
        var text = (value ?? "").Normalize(NormalizationForm.FormD).ToLowerInvariant().Replace(',', ' ');
        var sb = new StringBuilder();
        foreach (var ch in text)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        var tokens = Regex.Matches(sb.ToString(), "[a-z0-9]+")
            .Select(m => m.Value)
            .Where(t => !Titles.Contains(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();
        return string.Join(' ', tokens);
    }
}
