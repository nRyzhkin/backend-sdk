using System;
using System.Globalization;
using System.Text;

namespace BackendSdk
{
    public static class DisplayNameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = 32;

        public const string TooShort = "display_name_too_short";
        public const string TooLong = "display_name_too_long";
        public const string Invalid = "display_name_invalid";
        public const string Profanity = "display_name_profanity";

        static readonly string[] Slurs =
        {
            "fuck", "shit", "bitch", "cunt", "nigger", "nigga", "retard", "whore", "slut",
            "dick", "cock", "pussy", "penis", "porn", "rape",
            "хуй", "хуя", "хуе", "хуё", "пизд", "ебл", "ебат", "ебан", "бляд", "сука", "сучк",
            "мудак", "мудил", "пидор", "пидар", "педик", "залуп", "дроч", "гандон", "чмо"
        };

        static readonly string[] ExactTokens =
        {
            "ass", "sex", "fag", "бля"
        };

        public static bool IsPlaceholder(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return true;

            var trimmed = displayName.Trim();
            if (string.Equals(trimmed, "Player", StringComparison.OrdinalIgnoreCase))
                return true;

            return trimmed.StartsWith("Guest-", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryNormalize(string raw, out string normalized, out string errorCode)
        {
            normalized = string.Empty;
            errorCode = Invalid;

            if (string.IsNullOrWhiteSpace(raw))
            {
                errorCode = TooShort;
                return false;
            }

            var builder = new StringBuilder(raw.Length);
            var pendingSpace = false;
            foreach (var c in raw.Trim())
            {
                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (c == '-' || c == '_')
                {
                    if (pendingSpace && builder.Length > 0)
                        builder.Append(' ');
                    pendingSpace = false;
                    builder.Append(c);
                    continue;
                }

                if (!IsAllowedChar(c))
                {
                    errorCode = Invalid;
                    return false;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(c);
            }

            var value = builder.ToString().Trim();
            if (value.Length < MinLength)
            {
                errorCode = TooShort;
                return false;
            }

            if (value.Length > MaxLength)
            {
                errorCode = TooLong;
                return false;
            }

            if (IsPlaceholder(value) || IsDigitsOnly(value))
            {
                errorCode = Invalid;
                return false;
            }

            if (ContainsProfanity(value))
            {
                errorCode = Profanity;
                return false;
            }

            normalized = value;
            errorCode = string.Empty;
            return true;
        }

        public static string L10nKey(string errorCode) => errorCode switch
        {
            TooShort => "nick.error.too_short",
            TooLong => "nick.error.too_long",
            Profanity => "nick.error.profanity",
            _ => "nick.error.invalid"
        };

        static bool IsAllowedChar(char c)
        {
            var category = char.GetUnicodeCategory(c);
            return category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.DecimalDigitNumber;
        }

        static bool IsDigitsOnly(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                    return false;
            }

            return value.Length > 0;
        }

        static bool ContainsProfanity(string value)
        {
            var compact = Compact(value);
            for (var i = 0; i < Slurs.Length; i++)
            {
                if (compact.IndexOf(Slurs[i], StringComparison.Ordinal) >= 0)
                    return true;
            }

            var tokens = value.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (var t = 0; t < tokens.Length; t++)
            {
                var compactToken = Compact(tokens[t]);
                for (var i = 0; i < ExactTokens.Length; i++)
                {
                    if (string.Equals(compactToken, ExactTokens[i], StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        static string Compact(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var raw in value)
            {
                var c = char.ToLowerInvariant(raw);
                switch (c)
                {
                    case 'ё': c = 'е'; break;
                    case '@': c = 'a'; break;
                    case '0': c = 'o'; break;
                    case '1': c = 'i'; break;
                    case '3': c = 'e'; break;
                    case '4': c = 'a'; break;
                    case '$': c = 's'; break;
                }

                if (char.IsLetterOrDigit(c))
                    builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
