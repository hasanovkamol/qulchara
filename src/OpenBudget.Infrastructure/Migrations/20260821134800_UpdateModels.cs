using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpenBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Botni ishga tushirish va foydalanuvchi roliga (Guest/Broker/Admin) mos bosh menyuni ko'rsatish.");

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Joriy boshlangan har qanday jarayonni bekor qilish va boshlang'ich holatga qaytish.");

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CommandText", "Description" },
                values: new object[] { "/info", "Loyiha, OpenBudget haqida va botdan qanday foydalanish to'g'risida asosiy ma'lumotlar." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Guest", "/request", "Yangi foydalanuvchilar (Guest) tomonidan Adminlarga Broker sifatida ro'yxatdan o'tish so'rovini yuborish." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "/vote", "Yangi ovoz qo'shish. Kiritish kerak: [TelefonNomer] [Soat:Minut] [Kun(ixtiyoriy)]." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "/myvotes", "Broker o'zi kiritgan barcha (Pending/Confirmed) ovozlarning umumiy ro'yxati va holati." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "/mystats", "Brokerning shaxsiy statistikasi: to'plagan tasdiqlangan ovozlar soni va umumiy ishlangan pullari." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CommandText", "Description" },
                values: new object[] { "/myconfirmations", "Brokerning vaqt bo'yicha muvaffaqiyatli tasdiqlangan (Confirmed) ovozlari tarixi." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/guests", "Tizimga kirgan va so'rov yuborgan (hali tasdiqlanmagan) mehmonlar ro'yxatini ko'rish va ularni tasdiqlash." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/brokers", "Tasdiqlangan barcha faol brokerlar ro'yxati, ularning ma'lumotlari va tizimdan o'chirish imkoniyati." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/confirm", "SMS larni bittalab tasdiqlash. Kiritish kerak: [Oxirgi4Raqam] [Soat:Minut] [Kun(ixtiyoriy)]." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/pendingconfirmations", "Admin kiritgan, lekin hali brokerning ovozi bilan moslashmagan Kutishdagi (Pending) tasdiqlar ro'yxati." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/smshistory", "Admin tomonidan tizimga kiritilgan barcha muvaffaqiyatli, rad etilgan va kutilayotgan SMS lar tarixi." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/adminstats", "Barcha brokerlar orasida eng ko'p ovoz to'plaganlar reytingi va ularning umumlashgan faolligi." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/broadcast", "Botdagi barcha brokerlar yoki foydalanuvchilarga bildirishnoma va e'lon yuborish (rasm, matn)." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "SuperAdmin", "/globalstats", "Butun tizimdagi umumiy yig'ilgan ovozlar soni, to'langan pullar va to'liq tizim holati." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "SuperAdmin", "/admins", "Tizimda boshqaruv huquqiga ega bo'lgan mavjud barcha Adminlar ro'yxatini ko'rish." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "SuperAdmin", "/assignadmin", "Oddiy brokerlarni yoki foydalanuvchilarni Admin lavozimiga ko'tarish, yoki Adminlikdan olish." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "SuperAdmin", "/groups", "Ovozlar haqida avtomatik hisobot yuborilishi uchun telegram guruhlarini botga ulash yoki o'chirish." });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "SuperAdmin", "/settings", "Tizimning asosiy sozlamalari (ovoz narxi, avto-tasdiqlash vaqti chegarasi va boshqalar) ni o'zgartirish." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Botni ishga tushirish");

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Jarayonni bekor qilish");

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CommandText", "Description" },
                values: new object[] { "🔙 Bekor qilish", "Jarayonni bekor qilish" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "All", "/info", "Loyiha ma'lumotlari" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "All", "ℹ️ Loyiha ma'lumotlari", "Loyiha ma'lumotlari" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Guest", "/request", "Brokerlik so'rovi yuborish" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Guest", "📩 Brokerlik so'rovi yuborish", "Brokerlik so'rovi yuborish" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CommandText", "Description" },
                values: new object[] { "/vote", "Yangi ovoz qo'shish" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "📝 Ovoz qo'shish", "Yangi ovoz qo'shish" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "/myvotes", "Mening ovozlarim" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "📋 Mening ovozlarim", "Mening ovozlarim" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "/mystats", "Shaxsiy statistika" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "📊 Statistikam", "Shaxsiy statistika" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "📊 Mening statistikam", "Shaxsiy statistika" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "/myconfirmations", "Mening tasdiqlanganlarim" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Broker,Admin,SuperAdmin", "✅ Mening tasdiqlanganlarim", "Mening tasdiqlanganlarim" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/guests", "Mehmonlar ro'yxati" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "🚶‍♂️ Mehmonlar ro'yxati", "Mehmonlar ro'yxati" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "/brokers", "Brokerlar ro'yxati" });

            migrationBuilder.UpdateData(
                table: "BotCommands",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "AllowedRoles", "CommandText", "Description" },
                values: new object[] { "Admin,SuperAdmin", "👥 Brokerlar ro'yxati", "Brokerlar ro'yxati" });

            migrationBuilder.InsertData(
                table: "BotCommands",
                columns: new[] { "Id", "AllowedRoles", "CommandText", "Description", "IsActive" },
                values: new object[,]
                {
                    { 21, "Admin,SuperAdmin", "➕ Broker qo'shish", "Yangi broker qo'shish", true },
                    { 22, "Admin,SuperAdmin", "/confirm", "Ovoz tasdiqlash", true },
                    { 23, "Admin,SuperAdmin", "✅ Ovoz tasdiqlash", "Ovoz tasdiqlash", true },
                    { 24, "Admin,SuperAdmin", "/pendingconfirmations", "Kutishdagi tasdiqlar", true },
                    { 25, "Admin,SuperAdmin", "✅ OB da tasdiqlanganlar", "Kutishdagi tasdiqlar", true },
                    { 26, "Admin,SuperAdmin", "/smshistory", "SMS lar tarixi", true },
                    { 27, "Admin,SuperAdmin", "📜 SMS lar tarixi", "SMS lar tarixi", true },
                    { 28, "Admin,SuperAdmin", "/adminstats", "Brokerlar statistikasi", true },
                    { 29, "Admin,SuperAdmin", "📊 Brokerlar statistikasi", "Brokerlar statistikasi", true },
                    { 30, "Admin,SuperAdmin", "/broadcast", "Ommaviy xabar", true },
                    { 31, "Admin,SuperAdmin", "📨 Ommaviy xabar", "Ommaviy xabar", true },
                    { 32, "Admin,SuperAdmin", "🔲 QR Kod yuborish", "QR Kod yuborish", true },
                    { 33, "SuperAdmin", "/globalstats", "Global Statistika", true },
                    { 34, "SuperAdmin", "🌍 Global Statistika", "Global Statistika", true },
                    { 35, "SuperAdmin", "/admins", "Adminlar ro'yxati", true },
                    { 36, "SuperAdmin", "🛡 Adminlar ro'yxati", "Adminlar ro'yxati", true },
                    { 37, "SuperAdmin", "/assignadmin", "Admin tayinlash", true },
                    { 38, "SuperAdmin", "🛡 Admin tayinlash", "Admin tayinlash", true },
                    { 39, "SuperAdmin", "/groups", "Ulangan guruhlar", true },
                    { 40, "SuperAdmin", "📢 Ulangan guruhlar", "Ulangan guruhlar", true },
                    { 41, "SuperAdmin", "/settings", "Sozlamalar", true },
                    { 42, "SuperAdmin", "⚙️ Sozlamalar", "Sozlamalar", true }
                });
        }
    }
}
