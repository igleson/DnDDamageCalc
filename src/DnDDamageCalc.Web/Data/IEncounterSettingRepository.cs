using DnDDamageCalc.Web.Models;

namespace DnDDamageCalc.Web.Data;

public interface IEncounterSettingRepository
{
    Task<int> SaveAsync(EncounterSetting setting, string userId, string accessToken);
    Task<EncounterSetting?> GetByIdAsync(int id, string userId, string accessToken);
    Task<List<(int Id, string Name)>> ListAllAsync(string userId, string accessToken);
    Task DeleteAsync(int id, string userId, string accessToken);
}
