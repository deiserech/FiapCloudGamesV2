using FiapCloudGames.Application.Tracings;
using FiapCloudGames.Domain.Entities;
using FiapCloudGames.Domain.Interfaces.Repositories;
using FiapCloudGames.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Application.Services
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _repo;
        private readonly ILogger<GameService> _logger;

        public GameService(IGameRepository repo, ILogger<GameService> logger)
        {
            _repo = repo;
            _logger = logger;
        }


        public async Task<IEnumerable<Game>> GetallAsync()
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(GameService)}.GetallAsync");
            _logger.LogInformation("Listando todos os jogos");
            return await _repo.GetAllAsync();
        }


        public async Task<Game?> GetByIdAsync(int id)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(GameService)}.GetByIdAsync");
            _logger.LogInformation("Buscando jogo por ID: {Id}", id);
            return await _repo.GetByIdAsync(id);
        }

        public async Task<Game> CreateAsync(Game game)
        {
            using var activity = Tracing.ActivitySource.StartActivity($"{nameof(GameService)}.CreateAsync");
            _logger.LogInformation("Criando jogo: {Title}", game.Title);
            if (string.IsNullOrWhiteSpace(game.Title))
            {
                _logger.LogWarning("Tentativa de criar jogo sem título");
                throw new ArgumentException("O título do jogo é obrigatório.");
            }

            if (game.Price <= 0)
            {
                _logger.LogWarning("Tentativa de criar jogo com preço inválido: {Price}", game.Price);
                throw new ArgumentException("O preço do jogo deve ser maior que zero.");
            }

            var created = await _repo.CreateAsync(game);
            _logger.LogInformation("Jogo criado com sucesso: {Title}", game.Title);
            return created;
        }
    }
}
