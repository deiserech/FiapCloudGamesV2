using FiapCloudGames.Users.Domain.Entities;

namespace FiapCloudGames.Users.Application.DTOs;

public class LibraryDto
{
    public string? UserEmail { get; set; } = string.Empty;
    public string? UserName { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public int GameCode { get; set; }

    public static List<LibraryDto> ListFromEntity(IEnumerable<Library> libraries)
    {
       return [.. libraries.Select(library => new LibraryDto
        {
            UserEmail = library.User.Email,
            UserName = library.User.Name,
            GameCode = library.Game.Code,
            GameName = library.Game.Title
        })];
    }
}
