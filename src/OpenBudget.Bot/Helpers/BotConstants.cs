using Telegram.Bot.Types.ReplyMarkups;

namespace OpenBudget.Bot.Helpers;

public static class BotConstants
{
    public const string ProjectInfoHtml =
        "ℹ️ <b>Tashabbusli Budjet — Loyiha ma'lumotlari</b>\n" +
        "━━━━━━━━━━━━━━━━━━━━\n" +
        "📌 <b>Tashabbus kodi:</b> <code>055524000008</code>\n" +
        "📍 <b>Manzil:</b> Samarqand viloyati, Ishtixon tumani, Orlot MFY\n" +
        "📝 <b>Loyiha:</b> ORLOT MFY Qulchora qishlog`ining ichki yo`llarini 3 km qismini asfalt qoplamasi bilan yotqizish\n" +
        "💰 <b>Ajratilgan mablag':</b> 1 647 562 500 so'm\n" +
        "📅 <b>Mavsum:</b> 2026-yil, 2-mavsum\n" +
        "🗳 <b>Ovoz berish:</b> 22-avgustdan 31-avgustgacha\n" +
        "━━━━━━━━━━━━━━━━━━━━\n" +
        "🔗 <a href=\"https://openbudget.uz/boards/initiatives/initiative/55/4235e9c5-d4de-4677-8f0d-5438677e5322\">OpenBudget platformasida ko'rish</a>";

    public static ReplyKeyboardMarkup GetCancelKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🔙 Bekor qilish") }
        }) { ResizeKeyboard = true };
    }
}
