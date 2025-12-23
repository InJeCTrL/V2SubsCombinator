using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace V2SubsCombinator.Utils
{
    public class JWTHelper(IConfiguration configuration)
    {
        private readonly string jwtSecretKey = configuration.GetSection("JWTSettings")["Key"]
            ?? throw new InvalidOperationException("JWT secret key is not configured.");

        public string GenerateJWT(string userId, string userName, int expireHours = 24)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim("userId", userId),
                    new Claim("userName", userName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, 
                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                        ClaimValueTypes.Integer64)
                ]),
                Expires = DateTime.UtcNow.AddHours(expireHours),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        }
    }
}