using System.Text;

namespace NetExtension.OpenAPI.SdkGen.Core;

/// <summary>
/// Turns free-form declared text into a legal, stable PascalCase C# identifier.
/// </summary>
internal static class CSharpIdentifier
{
    /// <summary>
    /// Splits <paramref name="text"/> on any character that cannot appear in an identifier,
    /// PascalCases each remaining word and joins them. A leading digit is prefixed with an
    /// underscore, since an identifier may not start with one.
    /// </summary>
    public static string FromWords(string text)
    {
        StringBuilder builder = new(text.Length + 1);
        bool atWordStart = true;

        foreach (char character in text)
        {
            if (!char.IsLetterOrDigit(character))
            {
                // Any run of punctuation or whitespace is a word boundary and is dropped.
                atWordStart = true;
                continue;
            }

            if (builder.Length == 0 && char.IsDigit(character))
            {
                builder.Append('_');
            }

            builder.Append(atWordStart ? char.ToUpperInvariant(character) : character);
            atWordStart = false;
        }

        return builder.ToString();
    }
}
