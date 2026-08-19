using System.Text.RegularExpressions;

namespace OpenBudget.Application.Validators;

public static class PhoneNumberValidator
{
    public static (bool IsValid, string FormattedNumber) ValidateAndFormat(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return (false, string.Empty);

        var cleaned = new string(input.Where(char.IsDigit).ToArray());

        if (cleaned.StartsWith("998"))
        {
            cleaned = cleaned.Substring(3);
        }
        else if (cleaned.StartsWith("0"))
        {
            cleaned = cleaned.Substring(1);
        }

        if (cleaned.Length != 9)
        {
            return (false, string.Empty);
        }

        return (true, "+998" + cleaned);
    }
}
