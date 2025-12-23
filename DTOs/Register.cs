using System.ComponentModel.DataAnnotations;

namespace V2SubsCombinator.DTOs
{
    public class RegisterRequest
    {
        [MinLength(1)]
        public required string Username { get; set; }
        [MinLength(1)]
        public required string Password { get; set; }
    }

    public class RegisterResult
    {
        public required bool Success { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}