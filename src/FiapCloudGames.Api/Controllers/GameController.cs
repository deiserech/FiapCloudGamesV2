using FiapCloudGames.Domain.Entities;
using FiapCloudGames.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Api.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de jogos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GameController : ControllerBase
    {
        private readonly IGameService _service;

        public GameController(IGameService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Cadastra um novo jogo
        /// </summary>
        /// <remarks>
        /// Apenas administradores podem cadastrar jogos.
        /// </remarks>
        /// <param name="game">Dados do jogo a ser cadastrado</param>
        /// <returns>O jogo cadastrado</returns>
        /// <response code="201">Jogo cadastrado com sucesso</response>
        /// <response code="400">Dados inválidos</response>
        /// <response code="401">Não autorizado</response>
        /// <response code="403">Acesso negado - apenas administradores</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateGame([FromBody] Game game)
        {
            if (game == null)
                return BadRequest("Game data is required");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _service.CreateAsync(game);
            return CreatedAtAction(nameof(CreateGame), new { id = game.Id }, game);
        }

        /// <summary>
        /// Obtém um jogo pelo ID
        /// </summary>
        /// <param name="id">ID do jogo</param>
        /// <returns>O jogo encontrado</returns>
        /// <response code="200">Jogo encontrado</response>
        /// <response code="404">Jogo não encontrado</response>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User")]
        [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGameById(int id)
        {
            var game = await _service.GetByIdAsync(id);
            if (game == null) return NotFound();
            return Ok(game);
        }

        /// <summary>
        /// Lista todos os jogos cadastrados
        /// </summary>
        /// <returns>Lista de jogos</returns>
        /// <response code="200">Lista de jogos retornada com sucesso</response>
        [HttpGet]
        [Authorize(Roles = "Admin, User")]
        [ProducesResponseType(typeof(IEnumerable<Game>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGames()
        {
            return Ok(await _service.GetallAsync());
        }
    }
}
