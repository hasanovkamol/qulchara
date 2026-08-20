using System;

namespace OpenBudget.Domain.Helpers;

public static class DateTimeHelper
{
    public static DateTime UzbekistanNow => DateTime.UtcNow.AddHours(5);
}
