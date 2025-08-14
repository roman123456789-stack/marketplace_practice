namespace marketplace_practice.Services.dto.Users
{
    public class RoleDto
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
