using System.Threading;
using System.Threading.Tasks;
using OpenBudget.Domain.Interfaces;

namespace OpenBudget.Application.Services;

public class BotSettingService : IBotSettingService
{
    private const string LastDigitsCountKey = "VoteConfirmLastDigitsCount";
    private const int DefaultLastDigitsCount = 3;

    private readonly IBotSettingRepository _settingRepository;

    public BotSettingService(IBotSettingRepository settingRepository)
    {
        _settingRepository = settingRepository;
    }

    public async Task<int> GetLastDigitsCountAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingRepository.GetByKeyAsync(LastDigitsCountKey, cancellationToken);
        if (setting != null && int.TryParse(setting.Value, out int count) && count >= 2 && count <= 10)
        {
            return count;
        }

        return DefaultLastDigitsCount;
    }

    public async Task<bool> SetLastDigitsCountAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count < 2 || count > 10) return false;

        await _settingRepository.SetValueAsync(
            LastDigitsCountKey, 
            count.ToString(), 
            "Ovoz tasdiqlashda tekshiriladigan oxirgi raqamlar soni", 
            cancellationToken);

        return true;
    }

    public async Task<bool> GetAllowGuestRegistrationAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingRepository.GetByKeyAsync("AllowGuestRegistration", cancellationToken);
        if (setting != null && bool.TryParse(setting.Value, out bool allow))
        {
            return allow;
        }

        return true; // Default is allowed
    }

    public async Task<bool> SetAllowGuestRegistrationAsync(bool allow, CancellationToken cancellationToken = default)
    {
        await _settingRepository.SetValueAsync(
            "AllowGuestRegistration", 
            allow.ToString(), 
            "Yangi foydalanuvchilar (Mehmonlar) botga qo'shilishiga ruxsat", 
            cancellationToken);

        return true;
    }
}
