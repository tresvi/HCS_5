using HCS.Domain.Models;

namespace HCS.Infrastructure.Files.Interfaces
{
    public interface IServiceSecurityService
    {
        TxSetting Read(string filePath);
        void Save(TxSetting txSetting, string filePath);
    }
}
