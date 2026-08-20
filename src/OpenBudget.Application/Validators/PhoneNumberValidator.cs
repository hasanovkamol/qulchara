using System.Text.RegularExpressions;

namespace OpenBudget.Application.Validators;

public static class PhoneNumberValidator
{
    // O'zbekiston operator kodlari
    private static readonly HashSet<string> ValidOperatorCodes =
    [
        "20", "33", "55", "65", "66", "67", "69",
        "70", "71", "73", "74", "75", "77", "78", "79",
        "88", "89", "90", "91", "93", "94", "95", "97", "98", "99"
    ];

    public static (bool IsValid, string FormattedNumber) ValidateAndFormat(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (false, string.Empty);

        var cleaned = new string(input.Where(char.IsDigit).ToArray());

        // +998XXXXXXXXX (12 raqam) yoki 998XXXXXXXXX formatni tozalash
        if (cleaned.Length == 12 && cleaned.StartsWith("998"))
        {
            cleaned = cleaned[3..];
        }
        else if (cleaned.Length == 10 && cleaned.StartsWith("0"))
        {
            cleaned = cleaned[1..];
        }

        // Aniq 9 xona bo'lishi shart
        if (cleaned.Length != 9)
        {
            return (false, string.Empty);
        }

        // Faqat raqamlardan iborat ekanligi
        if (!cleaned.All(char.IsDigit))
        {
            return (false, string.Empty);
        }

        // Operator kodi to'g'riligini tekshirish
        var operatorCode = cleaned[..2];
        if (!ValidOperatorCodes.Contains(operatorCode))
        {
            return (false, string.Empty);
        }

        return (true, "+998" + cleaned);
    }
}
