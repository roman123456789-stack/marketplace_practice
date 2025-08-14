namespace marketplace_practice.Services.dto.Users
{
    public class UserBriefInfoDto
    {
        public long Id { get; set; }
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
