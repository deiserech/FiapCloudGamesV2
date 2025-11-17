using FiapCloudGames.Application.Services;
using FiapCloudGames.Domain.Entities;
using FiapCloudGames.Domain.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;  

namespace FiapCloudGames.Tests.Services
{
    public class PromotionServiceTests
    {
        private readonly Mock<IPromotionRepository> _mockPromotionRepository;
        private readonly Mock<IGameRepository> _mockGameRepository;
        private readonly Mock<ILogger<PromotionService>> _mockLogger; 
        private readonly PromotionService _promotionService;

        public PromotionServiceTests()
        {
            _mockPromotionRepository = new Mock<IPromotionRepository>();
            _mockGameRepository = new Mock<IGameRepository>();
            _mockLogger = new Mock<ILogger<PromotionService>>(); 
            _promotionService = new PromotionService(
                _mockPromotionRepository.Object,
                _mockGameRepository.Object,
                _mockLogger.Object 
            );
        }

        [Fact]
        public async Task CreatePromotionAsync_WithValidPromotion_ShouldCreateSuccessfully()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = 1,
                Title = "Test Promotion",
                Description = "A test promotion",
                DiscountPercentage = 10m,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                GameId = 1
            };

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(true);

            _mockPromotionRepository.Setup(repo => repo.GetActivePromotionsByGameIdAsync(promotion.GameId))
                                   .ReturnsAsync(new List<Promotion>());

            _mockPromotionRepository.Setup(repo => repo.CreateAsync(promotion))
                                   .ReturnsAsync(promotion);

            // Act
            var result = await _promotionService.CreatePromotionAsync(promotion);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(promotion);
            _mockGameRepository.Verify(repo => repo.ExistsAsync(promotion.GameId), Times.Once);
            _mockPromotionRepository.Verify(repo => repo.CreateAsync(promotion), Times.Once);
        }

        [Fact]
        public async Task CreatePromotionAsync_WithInvalidStartDate_ShouldThrowArgumentException()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "Test Promotion",
                DiscountPercentage = 10m,
                StartDate = DateTime.Today.AddDays(5),
                EndDate = DateTime.Today.AddDays(1), // End date before start date
                GameId = 1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _promotionService.CreatePromotionAsync(promotion));

            exception.Message.Should().Be("A data de início deve ser anterior à data de fim.");
        }

        [Fact]
        public async Task CreatePromotionAsync_WithNonExistentGame_ShouldThrowArgumentException()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "Test Promotion",
                DiscountPercentage = 10m,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                GameId = 999 // Non-existent game
            };

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _promotionService.CreatePromotionAsync(promotion));

            exception.Message.Should().Be("O jogo especificado não existe.");
        }

        [Fact]
        public async Task CreatePromotionAsync_WithNoDiscount_ShouldThrowArgumentException()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "Test Promotion",
                DiscountPercentage = 0m,
                DiscountAmount = null,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                GameId = 1
            };

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _promotionService.CreatePromotionAsync(promotion));

            exception.Message.Should().Be("Deve ser especificado um desconto percentual ou um valor de desconto.");
        }

        [Fact]
        public async Task CreatePromotionAsync_WithValidDiscountAmount_ShouldCreateSuccessfully()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = 1,
                Title = "Test Promotion",
                Description = "A test promotion",
                DiscountPercentage = 0m,
                DiscountAmount = 5.99m,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                GameId = 1
            };

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(true);

            _mockPromotionRepository.Setup(repo => repo.GetActivePromotionsByGameIdAsync(promotion.GameId))
                                   .ReturnsAsync(new List<Promotion>());

            _mockPromotionRepository.Setup(repo => repo.CreateAsync(promotion))
                                   .ReturnsAsync(promotion);

            // Act
            var result = await _promotionService.CreatePromotionAsync(promotion);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(promotion);
            _mockGameRepository.Verify(repo => repo.ExistsAsync(promotion.GameId), Times.Once);
            _mockPromotionRepository.Verify(repo => repo.CreateAsync(promotion), Times.Once);
        }

        [Fact]
        public async Task CreatePromotionAsync_WithStartDateInPast_ShouldThrowArgumentException()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "Test Promotion",
                DiscountPercentage = 10m,
                StartDate = DateTime.Today.AddDays(-1), // Yesterday
                EndDate = DateTime.Today.AddDays(5),
                GameId = 1
            };

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _promotionService.CreatePromotionAsync(promotion));

            exception.Message.Should().Be("A data de início da promoção não pode ser no passado.");
        }

        [Fact]
        public async Task CreatePromotionAsync_WithMaxActivePromotionsReached_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "Test Promotion",
                DiscountPercentage = 10m,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5),
                GameId = 1
            };

            var existingPromotions = new List<Promotion>
            {
                new Promotion { Id = 1, GameId = 1 },
                new Promotion { Id = 2, GameId = 1 },
                new Promotion { Id = 3, GameId = 1 }
            };

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(true);

            _mockPromotionRepository.Setup(repo => repo.GetActivePromotionsByGameIdAsync(promotion.GameId))
                                   .ReturnsAsync(existingPromotions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _promotionService.CreatePromotionAsync(promotion));

            exception.Message.Should().Contain("limite máximo de 3 promoção(ões) ativa(s)");
        }

        [Fact]
        public async Task CreatePromotionAsync_WithValidBusinessRules_ShouldCreateSuccessfully()
        {
            // Arrange
            var promotion = new Promotion
            {
                Id = 1,
                Title = "Test Promotion",
                DiscountPercentage = 10m,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(5), // Valid 4-day duration
                GameId = 1
            };

            var existingPromotions = new List<Promotion>
            {
                new Promotion { Id = 1, GameId = 1 },
                new Promotion { Id = 2, GameId = 1 }
            }; // Only 2 existing promotions

            _mockGameRepository.Setup(repo => repo.ExistsAsync(promotion.GameId))
                              .ReturnsAsync(true);

            _mockPromotionRepository.Setup(repo => repo.GetActivePromotionsByGameIdAsync(promotion.GameId))
                                   .ReturnsAsync(existingPromotions);

            _mockPromotionRepository.Setup(repo => repo.CreateAsync(promotion))
                                   .ReturnsAsync(promotion);

            // Act
            var result = await _promotionService.CreatePromotionAsync(promotion);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(promotion);
            _mockPromotionRepository.Verify(repo => repo.CreateAsync(promotion), Times.Once);
        }

        [Fact]
        public async Task UpdatePromotionAsync_WithValidBusinessRules_ShouldUpdateSuccessfully()
        {
            // Arrange
            var existingPromotion = new Promotion
            {
                Id = 1,
                Title = "Existing Promotion",
                GameId = 1
            };

            var updatedPromotion = new Promotion
            {
                Id = 1,
                Title = "Updated Promotion",
                DiscountPercentage = 15m,
                StartDate = DateTime.Today.AddDays(1),
                EndDate = DateTime.Today.AddDays(10), // Valid duration
                GameId = 1
            };

            var activePromotions = new List<Promotion>
            {
                existingPromotion // Only the current promotion
            };

            _mockPromotionRepository.Setup(repo => repo.GetByIdAsync(updatedPromotion.Id))
                                   .ReturnsAsync(existingPromotion);

            _mockGameRepository.Setup(repo => repo.ExistsAsync(updatedPromotion.GameId))
                              .ReturnsAsync(true);

            _mockPromotionRepository.Setup(repo => repo.GetActivePromotionsByGameIdAsync(updatedPromotion.GameId))
                                   .ReturnsAsync(activePromotions);

            _mockPromotionRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Promotion>()))
                                   .ReturnsAsync((Promotion p) => p);

            // Act
            var result = await _promotionService.UpdatePromotionAsync(updatedPromotion);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(updatedPromotion.Title);
            result.DiscountPercentage.Should().Be(updatedPromotion.DiscountPercentage);
            _mockPromotionRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Promotion>()), Times.Once);
        }

    }
}
