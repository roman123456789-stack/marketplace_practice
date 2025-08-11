using marketplace_practice.Models;

namespace marketplace_practice.Services.dto
{
    public class UserDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<RoleDto> Roles { get; set; } = new List<RoleDto>();
        public LoyaltyAccountDto LoyaltyAccount { get; set; }
        public UserDto(User user)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            RefreshToken = user.RefreshToken;
            ExpiresAt = user.ExpiresAt;
            PasswordHash = user.PasswordHash;
            IsActive = user.IsActive;
            EmailConfirmed = user.EmailConfirmed;
        }
    }
}
