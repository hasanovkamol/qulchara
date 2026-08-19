using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace OpenBudget.Application.Helpers;

public static class TelegramAuthHelper
{
    public static bool ValidateInitData(string initData, string botToken)
    {
        try
        {
            var parsedData = HttpUtility.ParseQueryString(initData);
            var hash = parsedData["hash"];
            if (string.IsNullOrEmpty(hash)) return false;

            var dataCheckString = string.Join("\n", parsedData.AllKeys
                .Where(k => k != "hash")
                .OrderBy(k => k)
                .Select(k => $"{k}={parsedData[k]}"));

            using var hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
            var secretKey = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(botToken));

            using var hmac = new HMACSHA256(secretKey);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
            var computedHashString = BitConverter.ToString(computedHash).Replace("-", "").ToLower();

            return computedHashString == hash;
        }
        catch
        {
            return false;
        }
    }

    public static TelegramUser? ParseInitData(string initData)
    {
        try
        {
            var parsedData = HttpUtility.ParseQueryString(initData);
            var userJson = parsedData["user"];
            if (string.IsNullOrEmpty(userJson)) return null;

            return JsonSerializer.Deserialize<TelegramUser>(userJson);
        }
        catch
        {
            return null;
        }
    }
}

public class TelegramUser
{
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? First_name { get; set; }
    public string? Last_name { get; set; }
}
