using FiapCloudGames.Domain.Entities;

namespace FiapCloudGames.Domain.Interfaces.Repositories
{
    public interface IPromotionRepository
    {
        Task<Promotion?> GetByIdAsync(int id);
        Task<IEnumerable<Promotion>> GetActivePromotionsAsync();
        Task<IEnumerable<Promotion>> GetActivePromotionsByGameIdAsync(int gameId);
        Task<Promotion> CreateAsync(Promotion promotion);
        Task<Promotion> UpdateAsync(Promotion promotion);
        Task DeleteAsync(int id);
    }
}
