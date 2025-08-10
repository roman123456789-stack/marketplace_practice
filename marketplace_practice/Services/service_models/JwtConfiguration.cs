namespace marketplace_practice.Services.service_models
{
    public class JwtConfiguration
    {
        public string Lifetime { get; set; }
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
