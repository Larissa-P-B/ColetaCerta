using ColetaCerta.Api.DTOs.Auth;

namespace ColetaCerta.Api.DTOs.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken);
    }
}