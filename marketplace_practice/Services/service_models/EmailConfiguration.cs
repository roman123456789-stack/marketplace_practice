namespace marketplace_practice.Services.service_models
{
    public class EmailConfiguration
    {
        public string FromName { get; set; } 
        public string FromAddress { get; set; }
        public string SmtpServer { get; set; }
        public int Port { get; set; } = 587;
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BaseUrl { get; set; }
    }
}
