using marketplace_practice.Models.Enums;

namespace marketplace_practice.Services.interfaces
{
    public interface IUser
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string Email { get; set; }
        Role Role { get; set; }
        DateTime? ExpiresAt { get; set; }
        string PasswordHash { get; set; }
        bool IsActive { get; set; }
        bool IsVerified { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }
}
