namespace marketplace_practice.Services.dto
{
    public class AccessTokenResult
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
