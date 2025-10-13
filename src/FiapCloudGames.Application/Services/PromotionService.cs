using FiapCloudGames.Application.Tracing;
using FiapCloudGames.Domain.Entities;
using FiapCloudGames.Domain.Interfaces.Repositories;
using FiapCloudGames.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Application.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IGameRepository _gameRepository;
        private readonly ILogger<PromotionService> _logger;

        private const int MaximumPromotionDurationDays = 30;
        private const int MaxActivePromotionsPerGame = 3;

        public PromotionService(IPromotionRepository promotionRepository, IGameRepository gameRepository, ILogger<PromotionService> logger)
        {
            _promotionRepository = promotionRepository;
            _gameRepository = gameRepository;
            _logger = logger;
        }


        public async Task<Promotion?> GetPromotionByIdAsync(int id)
        {
            using var activity = this.StartApiActivity($"{nameof(PromotionService)}.GetPromotionByIdAsync");
            _logger.LogInformation("Buscando promoção por ID: {Id}", id);
            return await _promotionRepository.GetByIdAsync(id);
        }


        public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
        {
            using var activity = this.StartApiActivity($"{nameof(PromotionService)}.GetActivePromotionsAsync");
            _logger.LogInformation("Buscando promoções ativas");
            return await _promotionRepository.GetActivePromotionsAsync();
        }


        public async Task<IEnumerable<Promotion>> GetActivePromotionsByGameIdAsync(int gameId)
        {
            using var activity = this.StartApiActivity($"{nameof(PromotionService)}.GetActivePromotionsByGameIdAsync");
            _logger.LogInformation("Buscando promoções ativas para o jogo {GameId}", gameId);
            return await _promotionRepository.GetActivePromotionsByGameIdAsync(gameId);
        }

        public async Task<Promotion> CreatePromotionAsync(Promotion promotion)
        {
            using var activity = this.StartApiActivity($"{nameof(PromotionService)}.CreatePromotionAsync");
            _logger.LogInformation("Criando promoção para o jogo {GameId}", promotion.GameId);
            if (promotion.StartDate >= promotion.EndDate)
            {
                _logger.LogWarning("Data de início maior ou igual à data de fim para promoção do jogo {GameId}", promotion.GameId);
                throw new ArgumentException("A data de início deve ser anterior à data de fim.");
            }

            if (!await _gameRepository.ExistsAsync(promotion.GameId))
            {
                _logger.LogWarning("Jogo não existe para promoção: {GameId}", promotion.GameId);
                throw new ArgumentException("O jogo especificado não existe.");
            }

            if (promotion.DiscountPercentage <= 0 && (!promotion.DiscountAmount.HasValue || promotion.DiscountAmount <= 0))
            {
                _logger.LogWarning("Desconto inválido para promoção do jogo {GameId}", promotion.GameId);
                throw new ArgumentException("Deve ser especificado um desconto percentual ou um valor de desconto.");
            }

            await ValidatePromotionBusinessRules(promotion);

            var created = await _promotionRepository.CreateAsync(promotion);
            _logger.LogInformation("Promoção criada com sucesso para o jogo {GameId}", promotion.GameId);
            return created;
        }

        public async Task<Promotion> UpdatePromotionAsync(Promotion promotion)
        {
            using var activity = this.StartApiActivity($"{nameof(PromotionService)}.UpdatePromotionAsync");
            var existing = await _promotionRepository.GetByIdAsync(promotion.Id);
            if (existing == null)
            {
                throw new ArgumentException("Promoção não encontrada.");
            }

            if (promotion.StartDate >= promotion.EndDate)
            {
                throw new ArgumentException("A data de início deve ser anterior à data de fim.");
            }

            if (!await _gameRepository.ExistsAsync(promotion.GameId))
            {
                throw new ArgumentException("O jogo especificado não existe.");
            }

            if (promotion.DiscountPercentage <= 0 && (!promotion.DiscountAmount.HasValue || promotion.DiscountAmount <= 0))
            {
                throw new ArgumentException("Deve ser especificado um desconto percentual ou um valor de desconto.");
            }

            await ValidatePromotionBusinessRules(promotion, promotion.Id);

            existing.Title = promotion.Title;
            existing.Description = promotion.Description;
            existing.DiscountPercentage = promotion.DiscountPercentage;
            existing.DiscountAmount = promotion.DiscountAmount;
            existing.StartDate = promotion.StartDate;
            existing.EndDate = promotion.EndDate;
            existing.IsActive = promotion.IsActive;
            existing.GameId = promotion.GameId;
            return await _promotionRepository.UpdateAsync(existing);
        }

        public async Task DeletePromotionAsync(int id)
        {
            using var activity = this.StartApiActivity($"{nameof(PromotionService)}.DeletePromotionAsync");
            var promotion = await _promotionRepository.GetByIdAsync(id);
            if (promotion == null)
            {
                throw new ArgumentException("Promoção não encontrada.");
            }

            await _promotionRepository.DeleteAsync(id);
        }

        public async Task<decimal> GetDiscountedPriceAsync(int gameId)
        {
            var bestPromotion = await GetBestPromotionForGameAsync(gameId);


            return bestPromotion?.CalculateDiscountedPrice()??0;
        }

        public async Task<Promotion?> GetBestPromotionForGameAsync(int gameId)
        {
            var activePromotions = await GetActivePromotionsByGameIdAsync(gameId);

            if (!activePromotions.Any())
            {
                return null;
            }

            var game = await _gameRepository.GetByIdAsync(gameId);
            if (game == null)
            {
                return null;
            }

            var originalPrice = game.Price;

            return activePromotions
                .OrderBy(p => p.CalculateDiscountedPrice())
                .FirstOrDefault();
        }

        private async Task ValidatePromotionBusinessRules(Promotion promotion, int? excludePromotionId = null)
        {
            ValidatePromotionDuration(promotion);

            ValidatePromotionStartDate(promotion);

            await ValidateActivePromotionsLimit(promotion.GameId, excludePromotionId);
        }

        private static void ValidatePromotionDuration(Promotion promotion)
        {
            var duration = promotion.EndDate - promotion.StartDate;
            var durationDays = (int)duration.TotalDays;

            if (durationDays > MaximumPromotionDurationDays)
            {
                throw new ArgumentException($"A duração da promoção não pode exceder {MaximumPromotionDurationDays} dias.");
            }
        }


        private static void ValidatePromotionStartDate(Promotion promotion)
        {
            var today = DateTime.Today;
            if (promotion.StartDate.Date < today)
            {
                throw new ArgumentException("A data de início da promoção não pode ser no passado.");
            }
        }

        private async Task ValidateActivePromotionsLimit(int gameId, int? excludePromotionId)
        {
            var activePromotions = await GetActivePromotionsByGameIdAsync(gameId);

             var activeCount = activePromotions.Count();

            if (activeCount >= MaxActivePromotionsPerGame)
            {
                throw new InvalidOperationException($"O jogo já possui o limite máximo de {MaxActivePromotionsPerGame} promoção(ões) ativa(s). Desative uma promoção existente antes de criar uma nova.");
            }
        }
    }
}
