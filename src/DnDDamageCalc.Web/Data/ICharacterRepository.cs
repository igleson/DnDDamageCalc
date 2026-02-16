using DnDDamageCalc.Web.Models;

namespace DnDDamageCalc.Web.Data;

public interface ICharacterRepository
{
    Task<int> SaveAsync(Character character, string userId, string accessToken);
    Task<Character?> GetByIdAsync(int id, string userId, string accessToken);
    Task<List<(int Id, string Name)>> ListAllAsync(string userId, string accessToken);
    Task DeleteAsync(int id, string userId, string accessToken);
}
