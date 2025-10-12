using FiapCloudGames.Domain.DTOs;
using FiapCloudGames.Domain.Entities;

namespace FiapCloudGames.Domain.Interfaces.Services
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateUserAsync(RegisterDto user);
    }
}
