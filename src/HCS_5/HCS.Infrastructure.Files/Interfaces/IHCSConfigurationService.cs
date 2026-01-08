using HCS.Domain.Models;

namespace HCS.Infrastructure.Files.Interfaces
{
    internal interface IHCSConfigurationService
    {
        HCSConfiguration Read(string filePath);
    }
}
