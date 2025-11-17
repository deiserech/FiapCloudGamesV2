using FiapCloudGames.Api.Controllers;
using FiapCloudGames.Application.DTOs;
using FiapCloudGames.Application.Interfaces;
using FiapCloudGames.Application.Interfaces.Services;
using FiapCloudGames.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FiapCloudGames.Tests.Controllers
{
    public class GameControllerTests
    {
        private readonly Mock<IGameService> _mockGameService;
        private readonly GameController _gameController;

        public GameControllerTests()
        {
            _mockGameService = new Mock<IGameService>();
            _gameController = new GameController(_mockGameService.Object);
        }

        #region Cadastrar Tests

        [Fact]
        public async Task Cadastrar_WithValidGame_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var game = new GameDto
            {
                Id = 1,
                Title = "Test Game",
                Description = "A test game description",
                Price = 59.99m,
            };

            _mockGameService.Setup(s => s.CreateAsync(It.IsAny<GameDto>())).Verifiable();

            // Act
            var result = await _gameController.CreateGame(game);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result as CreatedAtActionResult;
            createdResult.Should().NotBeNull();
            createdResult!.ActionName.Should().Be(nameof(_gameController.CreateGame));
            createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(game.Id);
            createdResult.Value.Should().Be(game);
            _mockGameService.Verify(s => s.CreateAsync(game), Times.Once);
        }

        [Fact]
        public async Task Cadastrar_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var game = new GameDto();
            _gameController.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _gameController.CreateGame(game);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
            badRequestResult!.Value.Should().BeOfType<SerializableError>();
            _mockGameService.Verify(s => s.CreateAsync(It.IsAny<GameDto>()), Times.Never);
        }

        #endregion

        #region ObterPorId Tests

        [Fact]
        public async Task ObterPorId_WithExistingGame_ShouldReturnOkWithGame()
        {
            // Arrange
            var gameId = 1;
            var expectedGame = new GameDto
            {
                Id = gameId,
                Title = "Test Game",
                Description = "A test game description",
                Price = 59.99m,
            };

            _mockGameService.Setup(s => s.GetByIdAsync(gameId)).ReturnsAsync(expectedGame);

            // Act
            var result = await _gameController.GetGameById(gameId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().Be(expectedGame);
            _mockGameService.Verify(s => s.GetByIdAsync(gameId), Times.Once);
        }

        [Fact]
        public async Task ObterPorId_WithNonExistingGame_ShouldReturnNotFound()
        {
            // Arrange
            var gameId = 999;
            _mockGameService.Setup(s => s.GetByIdAsync(gameId)).ReturnsAsync((GameDto?)null);

            // Act
            var result = await _gameController.GetGameById(gameId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _mockGameService.Verify(s => s.GetByIdAsync(gameId), Times.Once);
        }

        #endregion

        #region ListarTodos Tests

        [Fact]
        public async Task ListarTodos_WithGamesInDatabase_ShouldReturnOkWithGamesList()
        {
            // Arrange
            var expectedGames = new List<GameDto>
            {
                new GameDto { Id = 1, Title = "Game 1", Description = "Description 1", Price = 29.99m },
                new GameDto { Id = 2, Title = "Game 2", Description = "Description 2", Price = 39.99m },
                new GameDto { Id = 3, Title = "Game 3", Description = "Description 3", Price = 49.99m }
            };

            _mockGameService.Setup(s => s.GetAllAsync()).ReturnsAsync(expectedGames);

            // Act
            var result = await _gameController.GetGames();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeEquivalentTo(expectedGames);
            _mockGameService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task ListarTodos_WithEmptyDatabase_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            var emptyGamesList = new List<GameDto>();
            _mockGameService.Setup(s => s.GetAllAsync()).ReturnsAsync(emptyGamesList);

            // Act
            var result = await _gameController.GetGames();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeEquivalentTo(emptyGamesList);
            _mockGameService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task ListarTodos_WithSingleGame_ShouldReturnOkWithSingleGameList()
        {
            // Arrange
            var singleGame = new List<GameDto>
            {
                new GameDto { Id = 1, Title = "Only Game", Description = "The only game", Price = 99.99m}
            };

            _mockGameService.Setup(s => s.GetAllAsync()).ReturnsAsync(singleGame);

            // Act
            var result = await _gameController.GetGames();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeEquivalentTo(singleGame);
            var returnedGames = okResult.Value as IEnumerable<GameDto>;
            returnedGames.Should().HaveCount(1);
            _mockGameService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        #endregion
    }
}
