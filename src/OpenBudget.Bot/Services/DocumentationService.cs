using System.Collections.Generic;
using Telegram.Bot.Types.ReplyMarkups;
using OpenBudget.Domain.Enums;
using OpenBudget.Bot.Helpers;

namespace OpenBudget.Bot.Services;

public class DocumentationService : IDocumentationService
{
    public (string Text, InlineKeyboardMarkup Keyboard) GetMainMenu(UserRole role)
    {
        var text = "📚 <b>Bot Qullanmasi (Documentation)</b>\n\nIltimos, o'zingizga kerakli bo'limni tanlang:";
        var buttons = new List<InlineKeyboardButton[]>();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("📌 Loyiha ma'lumoti", "doc_project_info") });

        if (role == UserRole.SuperAdmin || role == UserRole.Admin)
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("👨‍💻 Boshqaruv qo'llanmasi", "doc_admin_guide") });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("👥 Brokerlarni boshqarish", "doc_broker_management") });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("✅ Ovoz tasdiqlash", "doc_vote_confirmation") });
            
            if (role == UserRole.SuperAdmin)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⚙️ Sozlamalar va QR kod", "doc_superadmin_settings") });
            }
        }
        else if (role == UserRole.Broker)
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("📝 Ovoz qanday qo'shiladi?", "doc_vote_guide") });
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("📊 Statistika va Ovozlarim", "doc_broker_stats") });
        }
        else // Guest
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🤝 Qanday qilib broker bo'lish mumkin?", "doc_guest_guide") });
        }

        return (text, new InlineKeyboardMarkup(buttons));
    }

    public (string Text, InlineKeyboardMarkup Keyboard) GetSectionContent(string sectionId, UserRole role)
    {
        var text = "";
        
        switch (sectionId)
        {
            case "doc_project_info":
                text = BotConstants.ProjectInfoHtml;
                break;
                
            case "doc_admin_guide":
                text = "👨‍💻 <b>Boshqaruv qo'llanmasi</b>\n\n" +
                       "Admin sifatida siz botning maxsus boshqaruv menyusidan foydalanasiz. Menyu orqali siz brokerlar statistikasini ko'rishingiz, tizimga yangi broker qo'shishingiz va yuborilgan ovozlarni tasdiqlashingiz mumkin.\n\n" +
                       "Pastki menyudagi tugmalardan foydalanib o'zingizga kerakli amalni bajarishingiz mumkin.";
                break;
                
            case "doc_broker_management":
                text = "👥 <b>Brokerlarni boshqarish</b>\n\n" +
                       "<b>1. Ro'yxatni ko'rish:</b> <code>👥 Brokerlar ro'yxati</code> orqali barcha faol va faol bo'lmagan brokerlarni ko'rasiz.\n\n" +
                       "<b>2. Broker qo'shish:</b> <code>➕ Broker qo'shish</code> orqali kimningdir Telegram ID va usernamesini kiritib broker qilasiz yoki o'sha odam yuborgan xabarni botga forward qilasiz.\n\n" +
                       "<b>3. So'rovlarni tasdiqlash:</b> Mehmon (Guest) o'z ID/telefonini yuborib brokerlik so'raganda, sizga xabar keladi. Siz uni <code>✅ Qabul qilish</code> orqali brokerga aylantirasiz.";
                break;
                
            case "doc_vote_confirmation":
                text = "✅ <b>Ovoz tasdiqlash jarayoni</b>\n\n" +
                       "Botga kirgazilgan barcha ovozlar avtomatik ravishta 'Kutilmoqda' (Pending) holatida turadi.\n\n" +
                       "Ovoz qabul qilinganligi tasdiqlash uchun <code>✅ Ovoz tasdiqlash</code> menyusiga kiring va ochiq byudjet SMS matnini botga yuboring. Tizim SMS matnidagi telefon raqam oxirgi raqamlarini hamda sms vaqtini analiz qilib, kerakli ovozni topadi va uning holatini 'Tasdiqlandi' ga o'zgartiradi.";
                break;
                
            case "doc_superadmin_settings":
                text = "⚙️ <b>Sozlamalar va QR kod (SuperAdmin)</b>\n\n" +
                       "<b>1. Ovoz tasdiqlash sozlamasi:</b> Tizim SMS orqali tasdiqlashda telefon raqamning nechta xonasiga qarab solishtirishini <code>⚙️ Sozlamalar</code> orqali o'zgartirishingiz mumkin. Odatda oxirgi 3-xona yetarli.\n\n" +
                       "<b>2. QR kod yuborish:</b> <code>🔲 QR Kod yuborish</code> tugmasi orqali Ochiq Byudjet loyihasi havolasini yuborsangiz, tizim avtomatik ravishda tayyor QR Kod rasmini generatsiya qilib, uni barcha brokerlarga ommaviy tarzda yuboradi.";
                break;
                
            case "doc_vote_guide":
                text = "📝 <b>Ovoz qanday qo'shiladi?</b>\n\n" +
                       "<b>1-qadam:</b> Ovoz beruvchining telefon raqamini botga kiriting.\n" +
                       "<b>2-qadam:</b> Bu raqamga Ochiq Byudjet SMS yuboradi.\n" +
                       "<b>3-qadam:</b> SMS orqali kelgan kod orqali ovozni muvaffaqiyatli bering.\n\n" +
                       "<b>4-qadam:</b> Muvaffaqiyatli SMS javobini adminga yuboring, shunda ular ovozingizni 'Tasdiqlangan' holatga o'tkazishadi. O'z ovozlaringizni <code>📋 Mening ovozlarim</code> qismidan kuzatib boring.";
                break;
                
            case "doc_broker_stats":
                text = "📊 <b>Statistika va Ovozlarim</b>\n\n" +
                       "<b>📋 Mening ovozlarim:</b> Siz tomondan kiritilgan oxirgi ovozlar va ularning joriy holati (Kutilmoqda, Tasdiqlandi, Bekor qilindi) ro'yxatini ko'rsatadi.\n\n" +
                       "<b>📊 Statistikam:</b> Umumiy va kunlik hisobda qancha ovoz yig'ganingizni va shundan qanchasi qabul qilinganligini infografika sifatida ko'rsatadi.";
                break;
                
            case "doc_guest_guide":
                text = "🤝 <b>Qanday qilib broker bo'lish mumkin?</b>\n\n" +
                       "Ovoz yig'uvchi (broker) sifatida tizimdan to'liq foydalanish uchun <code>📩 Brokerlik so'rovi yuborish</code> tugmasini bosing.\n\n" +
                       "So'ng, o'zingizni tanishtiruvchi ism-sharifingizni yoki telefon raqamingizni kiriting. So'rovingiz adminlarga yuboriladi va ular qabul qilgach, sizga broker menyulari ochiladi.";
                break;
                
            default:
                text = "Noma'lum bo'lim.";
                break;
        }

        var backButton = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔙 Orqaga (Menyu)", "doc_main") }
        });

        return (text, backButton);
    }
}
