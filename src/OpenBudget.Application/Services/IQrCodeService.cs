using System;

namespace OpenBudget.Application.Services;

public interface IQrCodeService
{
    byte[] GenerateQrCode(string url);
}
