using System.Security.Cryptography;
using System.Text;
using MongoDB.Driver;
using V2SubsCombinator.Database;
using V2SubsCombinator.DTOs;
using V2SubsCombinator.IServices;
using V2SubsCombinator.Models;
using V2SubsCombinator.Utils;

namespace V2SubsCombinator.Services
{
    public class Authentication(MongoDbContext dbContext, JWTHelper jwtHelper) : IAuthentication
    {
        private readonly MongoDbContext _dbContext = dbContext;
        private readonly JWTHelper _jwtHelper = jwtHelper;

        public async Task<LoginResult> AuthenticateAsync(LoginRequest loginRequest)
        {
            try
            {
                var user = await _dbContext.Users
                    .Find(u => u.Username == loginRequest.Username)
                    .FirstOrDefaultAsync();

                if (user == null || !VerifyPassword(loginRequest.Password, user.PasswordHash))
                {
                    return new LoginResult { Success = false };
                }

                var token = _jwtHelper.GenerateJWT(user.Id, user.Username);
                var expireAt = DateTime.UtcNow.AddHours(24);

                return new LoginResult
                {
                    Success = true,
                    Token = token,
                    ExpireAt = expireAt
                };
            }
            catch (Exception)
            {
                return new LoginResult { Success = false };
            }
        }

        public async Task<RegisterResult> RegisterAsync(RegisterRequest registerRequest)
        {
            try
            {
                var newUser = new User
                {
                    Username = registerRequest.Username,
                    PasswordHash = HashPassword(registerRequest.Password)
                };

                await _dbContext.Users.InsertOneAsync(newUser);
                var token = _jwtHelper.GenerateJWT(newUser.Id, newUser.Username);

                return new RegisterResult
                {
                    Success = true,
                    Token = token,
                    ExpireAt = DateTime.UtcNow.AddHours(24)
                };
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new RegisterResult { Success = false };
            }
        }

        private static string HashPassword(string password)
        {
            var hashedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
    }
}