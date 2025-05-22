/// <summary>
/// Provides extension methods for text manipulation and character validation.
///  The stackoverflow page : https://stackoverflow.com/questions/6219454/efficient-way-to-remove-all-whitespace-from-string/37347881#37347881
/// 
/// </summary>
public static class TextUtility
{
    /// <summary>
    /// Determines whether a character is considered whitespace.
    /// Includes all Unicode whitespace characters.
    /// </summary>
    /// <param name="character">The character to check.</param>
    /// <returns>True if the character is whitespace; otherwise, false.</returns>
    public static bool IsWhitespace(this char character)
    {
        switch (character)
        {
            case '\u0020': // Space
            case '\u00A0': // No-Break Space
            case '\u1680': // Ogham Space Mark
            case '\u2000': // En Quad
            case '\u2001': // Em Quad
            case '\u2002': // En Space
            case '\u2003': // Em Space
            case '\u2004': // Three-Per-Em Space
            case '\u2005': // Four-Per-Em Space
            case '\u2006': // Six-Per-Em Space
            case '\u2007': // Figure Space
            case '\u2008': // Punctuation Space
            case '\u2009': // Thin Space
            case '\u200A': // Hair Space
            case '\u202F': // Narrow No-Break Space
            case '\u205F': // Medium Mathematical Space
            case '\u3000': // Ideographic Space
            case '\u2028': // Line Separator
            case '\u2029': // Paragraph Separator
            case '\u0009': // Tab
            case '\u000A': // Line Feed
            case '\u000B': // Vertical Tab
            case '\u000C': // Form Feed
            case '\u000D': // Carriage Return
            case '\u0085': // Next Line
            {
                return true;
            }

            default:
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Removes all whitespace characters from a string.
    /// Uses an optimized in-place algorithm that avoids creating intermediate string objects.
    /// </summary>
    /// <param name="text">The string to remove whitespaces from.</param>
    /// <returns>A new string with all whitespace characters removed.</returns>
    /// <remarks>
    /// Implementation based on: https://stackoverflow.com/a/37368176
    /// Performance benchmarks: https://stackoverflow.com/a/37347881
    /// </remarks>
    public static string RemoveWhitespaces(this string text)
    {
        int textLength = text.Length;

        char[] textCharacters = text.ToCharArray();

        int currentWhitespacelessTextLength = 0;

        for (
            int currentCharacterIndex = 0;
            currentCharacterIndex < textLength;
            ++currentCharacterIndex
        )
        {
            char currentTextCharacter = textCharacters[currentCharacterIndex];

            if (currentTextCharacter.IsWhitespace())
            {
                continue;
            }

            textCharacters[currentWhitespacelessTextLength++] = currentTextCharacter;
        }

        return new string(textCharacters, 0, currentWhitespacelessTextLength);
    }

    /// <summary>
    /// Removes all special characters from a string, keeping only letters, digits, and whitespace.
    /// Uses an optimized in-place algorithm similar to RemoveWhitespaces.
    /// </summary>
    /// <param name="text">The string to remove special characters from.</param>
    /// <returns>A new string with all special characters removed.</returns>
    /// <remarks>
    /// See alternatives: https://stackoverflow.com/questions/3210393/how-do-i-remove-all-non-alphanumeric-characters-from-a-string-except-dash
    /// </remarks>
    public static string RemoveSpecialCharacters(this string text)
    {
        int textLength = text.Length;

        char[] textCharacters = text.ToCharArray();

        int currentWhitespacelessTextLength = 0;

        for (
            int currentCharacterIndex = 0;
            currentCharacterIndex < textLength;
            ++currentCharacterIndex
        )
        {
            char currentTextCharacter = textCharacters[currentCharacterIndex];

            if (!char.IsLetterOrDigit(currentTextCharacter) && !currentTextCharacter.IsWhitespace())
            {
                continue;
            }

            textCharacters[currentWhitespacelessTextLength++] = currentTextCharacter;
        }

        return new string(textCharacters, 0, currentWhitespacelessTextLength);
    }
}
