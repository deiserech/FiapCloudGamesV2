using FiapCloudGames.Domain.DTOs;

namespace FiapCloudGames.Domain.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> Login(LoginDto loginDto);
        Task<AuthResponseDto?> Register(RegisterDto registerDto);
    }
}
