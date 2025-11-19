using FiapCloudGames.Users.Domain.Entities;

namespace FiapCloudGames.Users.Application.DTOs
{
    public class UserDto
    {
        public required string Name { get; set; }

        public required string Email { get; set; }

        public required string Role { get; set; }

        public static UserDto FromEntity(User user) => new()
        {
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }
}

