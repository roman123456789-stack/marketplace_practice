namespace marketplace_practice.Services.dto
{
    public class RecoveryResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ResetToken { get; set; }
    }
}
