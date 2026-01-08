using HCS.Domain.Models;
using System.Collections.Generic;

namespace HCS.Infrastructure.Files.Interfaces
{
    public interface ITxSettingService
    {
        TxSetting Read(string filePath);
        void Save(List<TxSetting> txSetting, string filePath);
    }
}
