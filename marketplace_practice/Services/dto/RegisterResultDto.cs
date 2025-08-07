namespace marketplace_practice.Services.dto
{
    public class RegisterResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;

        public UserDto User { get; set; }
        public string emailVerificationToken { get; set; } // временно (пока не настроим почту)
    }
}
