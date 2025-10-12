using FiapCloudGames.Application.Services;
using FiapCloudGames.Domain.Entities;
using FiapCloudGames.Domain.Interfaces.Repositories;
using FiapCloudGames.Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging; // Adicionado para ILogger

namespace FiapCloudGames.Tests.Services
{
    public class LibraryServiceTests
    {
        private readonly Mock<ILibraryRepository> _mockLibraryRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IGameRepository> _mockGameRepo;
        private readonly Mock<IPromotionService> _mockPromotionService;
        private readonly Mock<ILogger<LibraryService>> _mockLogger; // Adicionado
        private readonly LibraryService _libraryService;

        public LibraryServiceTests()
        {
            _mockLibraryRepo = new Mock<ILibraryRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockGameRepo = new Mock<IGameRepository>();
            _mockPromotionService = new Mock<IPromotionService>();
            _mockLogger = new Mock<ILogger<LibraryService>>(); // Adicionado
            _libraryService = new LibraryService(
                _mockLibraryRepo.Object,
                _mockUserRepo.Object,
                _mockGameRepo.Object,
                _mockPromotionService.Object,
                _mockLogger.Object // Adicionado
            );
        }

        [Fact]
        public async Task GetUserLibraryAsync_WithValidUserId_ReturnsLibraryEntries()
        {
            // Arrange
            int userId = 1;
            var expectedLibraries = new List<Library>
                {
                    new Library { Id = 1, UserId = userId, GameId = 10 },
                    new Library { Id = 2, UserId = userId, GameId = 20 }
                };

            _mockUserRepo.Setup(r => r.ExistsAsync(userId)).ReturnsAsync(true);
            _mockLibraryRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(expectedLibraries);

            // Act
            var result = await _libraryService.GetUserLibraryAsync(userId);

            // Assert
            result.Should().BeEquivalentTo(expectedLibraries);
            _mockUserRepo.Verify(r => r.ExistsAsync(userId), Times.Once);
            _mockLibraryRepo.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetLibraryEntryAsync_WithValidId_ReturnsLibraryEntry()
        {
            // Arrange
            int entryId = 1;
            var expectedEntry = new Library { Id = entryId, UserId = 1, GameId = 10 };

            _mockLibraryRepo.Setup(r => r.GetByIdAsync(entryId)).ReturnsAsync(expectedEntry);

            // Act
            var result = await _libraryService.GetLibraryEntryAsync(entryId);

            // Assert
            result.Should().Be(expectedEntry);
            _mockLibraryRepo.Verify(r => r.GetByIdAsync(entryId), Times.Once);
        }

        [Fact]
        public async Task PurchaseGameAsync_WithValidData_CreatesLibraryEntry()
        {
            // Arrange
            int userId = 1;
            int gameId = 10;
            decimal gamePrice = 99.99m;
            decimal discountedPrice = 79.99m;
            var game = new Game { Id = gameId, Price = gamePrice };
            var createdLibrary = new Library { Id = 1, UserId = userId, GameId = gameId };

            _mockUserRepo.Setup(r => r.ExistsAsync(userId)).ReturnsAsync(true);
            _mockGameRepo.Setup(r => r.GetByIdAsync(gameId)).ReturnsAsync(game);
            _mockLibraryRepo.Setup(r => r.UserOwnsGameAsync(userId, gameId)).ReturnsAsync(false);
            _mockPromotionService.Setup(s => s.GetDiscountedPriceAsync(gameId)).ReturnsAsync(discountedPrice);
            _mockLibraryRepo.Setup(r => r.CreateAsync(It.IsAny<Library>())).ReturnsAsync(createdLibrary);

            // Act
            var result = await _libraryService.PurchaseGameAsync(userId, gameId);

            // Assert
            result.Should().Be(createdLibrary);
            _mockUserRepo.Verify(r => r.ExistsAsync(userId), Times.Once);
            _mockGameRepo.Verify(r => r.GetByIdAsync(gameId), Times.Once);
            _mockLibraryRepo.Verify(r => r.UserOwnsGameAsync(userId, gameId), Times.Once);
            _mockPromotionService.Verify(s => s.GetDiscountedPriceAsync(gameId), Times.Once);
            _mockLibraryRepo.Verify(r => r.CreateAsync(It.IsAny<Library>()), Times.Once);
        }
    }
}
