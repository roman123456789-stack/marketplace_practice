namespace marketplace_practice.Services.service_models
{
    public class RefreshTokenModel
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }

        public RefreshTokenModel(string token, DateTime expiresAt)
        {
            Token = token;
            ExpiresAt = expiresAt;
        }
    }
}
