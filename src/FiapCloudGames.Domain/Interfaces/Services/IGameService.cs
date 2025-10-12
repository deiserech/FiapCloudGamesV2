using FiapCloudGames.Domain.Entities;

namespace FiapCloudGames.Domain.Interfaces.Services
{
    public interface IGameService
    {
        Task<IEnumerable<Game>> GetallAsync();
        Task<Game?> GetByIdAsync(int id);
        Task<Game> CreateAsync(Game game);
    }
}
