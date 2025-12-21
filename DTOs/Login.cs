namespace V2SubsCombinator.DTOs
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResult
    {
        public bool Success { get; set; } = false;
        public string? Token { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}