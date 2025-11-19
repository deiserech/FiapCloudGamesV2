using FiapCloudGames.Users.Application.DTOs;
using FiapCloudGames.Users.Application.Interfaces.Services;
using FiapCloudGames.Users.Domain.Entities;
using FiapCloudGames.Users.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Users.Api.Controllers;

/// <summary>
/// Controller responsável pelo gerenciamento de usuários
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UserController(IUserService service) : ControllerBase
{

    /// <summary>
    /// Obtém um usuário pelo ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Dados do usuário</returns>
    /// <response code="200">Usuário encontrado</response>
    /// <response code="404">Usuário não encontrado</response>
    /// <response code="401">Não autorizado</response>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin, User")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await service.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(UserDto.FromEntity(user));
    }

    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    /// <param name="userDto">Dados do usuário a ser criado</param>
    /// <returns>Usuário criado</returns>
    /// <response code="201">Usuário criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] RegisterDto userDto)
    {
        var user = await service.CreateUserAsync(userDto);
        return CreatedAtAction(nameof(CreateUser), UserDto.FromEntity(user));
    }


    /// <summary>
    /// Obtém a biblioteca completa de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de jogos na biblioteca do usuário</returns>
    /// <response code="200">Retorna a biblioteca do usuário</response>
    /// <response code="404">Usuário não encontrado</response>
    /// <response code="400">Erro na solicitação</response>
    /// <response code="401">Não autorizado</response>
    [HttpGet("/{userId}/library")]
    [Authorize(Roles = "Admin, User")]
    [ProducesResponseType(typeof(IEnumerable<Library>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<LibraryDto>>> GetUserLibrary(Guid userId)
    {
        var library = await service.GetUserLibraryAsync(userId);

        if (library is null)
            return NotFound();

        return Ok(LibraryDto.ListFromEntity(library));
    }
}

