namespace marketplace_practice.Controllers.dto
{
    public class CreateUserResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;

        public UserDto User { get; set; }
        public string emailVerificationToken { get; set; } // временно (пока не настроим почту)
    }
}
