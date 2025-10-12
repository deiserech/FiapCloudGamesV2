using FiapCloudGames.Domain.Entities;

namespace FiapCloudGames.Domain.Interfaces.Repositories
{
    public interface IGameRepository
    {
        Task<Game?> GetByIdAsync(int id);
        Task<IEnumerable<Game>> GetAllAsync();
        Task<Game> CreateAsync(Game game);
        Task<Game> UpdateAsync(Game game);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
