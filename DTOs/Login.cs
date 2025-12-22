namespace V2SubsCombinator.DTOs
{
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class LoginResult
    {
        public required bool Success { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}