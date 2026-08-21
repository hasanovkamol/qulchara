using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenBudget.Domain.Entities;
using System.Collections.Generic;

namespace OpenBudget.Infrastructure.Data.Configurations;

public class BotCommandConfiguration : IEntityTypeConfiguration<BotCommand>
{
    public void Configure(EntityTypeBuilder<BotCommand> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CommandText).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.AllowedRoles).HasMaxLength(100);

        // Seed Data
        builder.HasData(GetSeedData());
    }

    private List<BotCommand> GetSeedData()
    {
        return new List<BotCommand>
        {
            // Umumiy
            new BotCommand { Id = 1, CommandText = "/start", Description = "Botni ishga tushirish va foydalanuvchi roliga (Guest/Broker/Admin) mos bosh menyuni ko'rsatish.", AllowedRoles = "All" },
            new BotCommand { Id = 2, CommandText = "/cancel", Description = "Joriy boshlangan har qanday jarayonni bekor qilish va boshlang'ich holatga qaytish.", AllowedRoles = "All" },
            new BotCommand { Id = 3, CommandText = "/info", Description = "Loyiha, OpenBudget haqida va botdan qanday foydalanish to'g'risida asosiy ma'lumotlar.", AllowedRoles = "All" },

            // Guest
            new BotCommand { Id = 4, CommandText = "/request", Description = "Yangi foydalanuvchilar (Guest) tomonidan Adminlarga Broker sifatida ro'yxatdan o'tish so'rovini yuborish.", AllowedRoles = "Guest" },

            // Broker (va Adminlar)
            new BotCommand { Id = 5, CommandText = "/vote", Description = "Yangi ovoz qo'shish. Kiritish kerak: [TelefonNomer] [Soat:Minut] [Kun(ixtiyoriy)].", AllowedRoles = "Broker,Admin,SuperAdmin" },
            new BotCommand { Id = 6, CommandText = "/myvotes", Description = "Broker o'zi kiritgan barcha (Pending/Confirmed) ovozlarning umumiy ro'yxati va holati.", AllowedRoles = "Broker,Admin,SuperAdmin" },
            new BotCommand { Id = 7, CommandText = "/mystats", Description = "Brokerning shaxsiy statistikasi: to'plagan tasdiqlangan ovozlar soni va umumiy ishlangan pullari.", AllowedRoles = "Broker,Admin,SuperAdmin" },
            new BotCommand { Id = 8, CommandText = "/myconfirmations", Description = "Brokerning vaqt bo'yicha muvaffaqiyatli tasdiqlangan (Confirmed) ovozlari tarixi.", AllowedRoles = "Broker,Admin,SuperAdmin" },

            // Admin va SuperAdmin
            new BotCommand { Id = 9, CommandText = "/guests", Description = "Tizimga kirgan va so'rov yuborgan (hali tasdiqlanmagan) mehmonlar ro'yxatini ko'rish va ularni tasdiqlash.", AllowedRoles = "Admin,SuperAdmin" },
            new BotCommand { Id = 10, CommandText = "/brokers", Description = "Tasdiqlangan barcha faol brokerlar ro'yxati, ularning ma'lumotlari va tizimdan o'chirish imkoniyati.", AllowedRoles = "Admin,SuperAdmin" },
            new BotCommand { Id = 11, CommandText = "/confirm", Description = "SMS larni bittalab tasdiqlash. Kiritish kerak: [Oxirgi4Raqam] [Soat:Minut] [Kun(ixtiyoriy)].", AllowedRoles = "Admin,SuperAdmin" },
            new BotCommand { Id = 12, CommandText = "/pendingconfirmations", Description = "Admin kiritgan, lekin hali brokerning ovozi bilan moslashmagan Kutishdagi (Pending) tasdiqlar ro'yxati.", AllowedRoles = "Admin,SuperAdmin" },
            new BotCommand { Id = 13, CommandText = "/smshistory", Description = "Admin tomonidan tizimga kiritilgan barcha muvaffaqiyatli, rad etilgan va kutilayotgan SMS lar tarixi.", AllowedRoles = "Admin,SuperAdmin" },
            new BotCommand { Id = 14, CommandText = "/adminstats", Description = "Barcha brokerlar orasida eng ko'p ovoz to'plaganlar reytingi va ularning umumlashgan faolligi.", AllowedRoles = "Admin,SuperAdmin" },
            new BotCommand { Id = 15, CommandText = "/broadcast", Description = "Botdagi barcha brokerlar yoki foydalanuvchilarga bildirishnoma va e'lon yuborish (rasm, matn).", AllowedRoles = "Admin,SuperAdmin" },

            // Faqat SuperAdmin
            new BotCommand { Id = 16, CommandText = "/globalstats", Description = "Butun tizimdagi umumiy yig'ilgan ovozlar soni, to'langan pullar va to'liq tizim holati.", AllowedRoles = "SuperAdmin" },
            new BotCommand { Id = 17, CommandText = "/admins", Description = "Tizimda boshqaruv huquqiga ega bo'lgan mavjud barcha Adminlar ro'yxatini ko'rish.", AllowedRoles = "SuperAdmin" },
            new BotCommand { Id = 18, CommandText = "/assignadmin", Description = "Oddiy brokerlarni yoki foydalanuvchilarni Admin lavozimiga ko'tarish, yoki Adminlikdan olish.", AllowedRoles = "SuperAdmin" },
            new BotCommand { Id = 19, CommandText = "/groups", Description = "Ovozlar haqida avtomatik hisobot yuborilishi uchun telegram guruhlarini botga ulash yoki o'chirish.", AllowedRoles = "SuperAdmin" },
            new BotCommand { Id = 20, CommandText = "/settings", Description = "Tizimning asosiy sozlamalari (ovoz narxi, avto-tasdiqlash vaqti chegarasi va boshqalar) ni o'zgartirish.", AllowedRoles = "SuperAdmin" }
        };
    }
}
