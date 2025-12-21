using V2SubsCombinator.DTOs;

namespace V2SubsCombinator.IServices
{
    public interface IAuthentication
    {
        public Task<LoginResult> AuthenticateAsync(LoginRequest loginRequest);
        public Task<RegisterRequest> RegisterAsync(RegisterRequest registerRequest);
    }
}