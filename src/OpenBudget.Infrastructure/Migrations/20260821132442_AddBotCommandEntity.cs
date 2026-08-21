using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpenBudget.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBotCommandEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AllowedRoles = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotCommands", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BotCommands",
                columns: new[] { "Id", "AllowedRoles", "CommandText", "Description", "IsActive" },
                values: new object[,]
                {
                    { 1, "All", "/start", "Botni ishga tushirish va foydalanuvchi roliga (Guest/Broker/Admin) mos bosh menyuni ko'rsatish.", true },
                    { 2, "All", "/cancel", "Joriy boshlangan har qanday jarayonni bekor qilish va boshlang'ich holatga qaytish.", true },
                    { 3, "All", "/info", "Loyiha, OpenBudget haqida va botdan qanday foydalanish to'g'risida asosiy ma'lumotlar.", true },
                    { 4, "Guest", "/request", "Yangi foydalanuvchilar (Guest) tomonidan Adminlarga Broker sifatida ro'yxatdan o'tish so'rovini yuborish.", true },
                    { 5, "Broker,Admin,SuperAdmin", "/vote", "Yangi ovoz qo'shish. Kiritish kerak: [TelefonNomer] [Soat:Minut] [Kun(ixtiyoriy)].", true },
                    { 6, "Broker,Admin,SuperAdmin", "/myvotes", "Broker o'zi kiritgan barcha (Pending/Confirmed) ovozlarning umumiy ro'yxati va holati.", true },
                    { 7, "Broker,Admin,SuperAdmin", "/mystats", "Brokerning shaxsiy statistikasi: to'plagan tasdiqlangan ovozlar soni va umumiy ishlangan pullari.", true },
                    { 8, "Broker,Admin,SuperAdmin", "/myconfirmations", "Brokerning vaqt bo'yicha muvaffaqiyatli tasdiqlangan (Confirmed) ovozlari tarixi.", true },
                    { 9, "Admin,SuperAdmin", "/guests", "Tizimga kirgan va so'rov yuborgan (hali tasdiqlanmagan) mehmonlar ro'yxatini ko'rish va ularni tasdiqlash.", true },
                    { 10, "Admin,SuperAdmin", "/brokers", "Tasdiqlangan barcha faol brokerlar ro'yxati, ularning ma'lumotlari va tizimdan o'chirish imkoniyati.", true },
                    { 11, "Admin,SuperAdmin", "/confirm", "SMS larni bittalab tasdiqlash. Kiritish kerak: [Oxirgi4Raqam] [Soat:Minut] [Kun(ixtiyoriy)].", true },
                    { 12, "Admin,SuperAdmin", "/pendingconfirmations", "Admin kiritgan, lekin hali brokerning ovozi bilan moslashmagan Kutishdagi (Pending) tasdiqlar ro'yxati.", true },
                    { 13, "Admin,SuperAdmin", "/smshistory", "Admin tomonidan tizimga kiritilgan barcha muvaffaqiyatli, rad etilgan va kutilayotgan SMS lar tarixi.", true },
                    { 14, "Admin,SuperAdmin", "/adminstats", "Barcha brokerlar orasida eng ko'p ovoz to'plaganlar reytingi va ularning umumlashgan faolligi.", true },
                    { 15, "Admin,SuperAdmin", "/broadcast", "Botdagi barcha brokerlar yoki foydalanuvchilarga bildirishnoma va e'lon yuborish (rasm, matn).", true },
                    { 16, "SuperAdmin", "/globalstats", "Butun tizimdagi umumiy yig'ilgan ovozlar soni, to'langan pullar va to'liq tizim holati.", true },
                    { 17, "SuperAdmin", "/admins", "Tizimda boshqaruv huquqiga ega bo'lgan mavjud barcha Adminlar ro'yxatini ko'rish.", true },
                    { 18, "SuperAdmin", "/assignadmin", "Oddiy brokerlarni yoki foydalanuvchilarni Admin lavozimiga ko'tarish, yoki Adminlikdan olish.", true },
                    { 19, "SuperAdmin", "/groups", "Ovozlar haqida avtomatik hisobot yuborilishi uchun telegram guruhlarini botga ulash yoki o'chirish.", true },
                    { 20, "SuperAdmin", "/settings", "Tizimning asosiy sozlamalari (ovoz narxi, avto-tasdiqlash vaqti chegarasi va boshqalar) ni o'zgartirish.", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotCommands");
        }
    }
}
