using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services.dto
{
    public class RegisterResultDto
    {
        //public Token AccessToken { get; set; }
        //public Token RefreshToken { get; set; }

        public UserDto User { get; set; }
        public string emailVerificationToken { get; set; } // временно (пока не настроим почту)
    }
}
