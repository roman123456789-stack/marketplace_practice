using marketplace_practice.Models;

namespace marketplace_practice.Services.dto.Users
{
    public class UserDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<RoleDto> Roles { get; set; }
        public LoyaltyAccountDto? LoyaltyAccount { get; set; }
        public UserDto(User user)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email!;
            IsActive = user.IsActive;
            CreatedAt = user.CreatedAt;
            UpdatedAt = user.UpdatedAt;
        }
    }
}
