using Telegram.Bot.Types.ReplyMarkups;
using OpenBudget.Domain.Enums;

namespace OpenBudget.Bot.Services;

public interface IDocumentationService
{
    (string Text, InlineKeyboardMarkup Keyboard) GetMainMenu(UserRole role);
    (string Text, InlineKeyboardMarkup Keyboard) GetSectionContent(string sectionId, UserRole role);
}
