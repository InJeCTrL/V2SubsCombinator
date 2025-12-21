namespace V2SubsCombinator.DTOs
{
    public class RegisterRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterResult
    {
        public bool Success { get; set; } = false;
        public string? Token { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}