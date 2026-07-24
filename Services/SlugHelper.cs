using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Webpcvaphukienkethopchatbot.Services;

public static partial class SlugHelper
{
    public static string Generate(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character == 'đ' ? 'd' : character);
            }
        }

        var slug = InvalidCharacters().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-");
        return RepeatedHyphens().Replace(slug, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidCharacters();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedHyphens();
}
