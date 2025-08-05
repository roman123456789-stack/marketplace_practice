namespace marketplace_practice.Services.service_models
{
    public class EmailMessage
    {
        public List<string> To { get; set; } = new List<string>();
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
