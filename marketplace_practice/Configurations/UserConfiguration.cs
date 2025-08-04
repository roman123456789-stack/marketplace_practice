using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id);
            builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(100);
            builder.Property(u => u.Email).HasMaxLength(100).IsRequired();
            builder.Property(u => u.RefreshToken);
            builder.Property(u => u.ExpiresAt);
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.IsVerified).IsRequired();
            builder.Property(u => u.CreatedAt).IsRequired();
            builder.Property(u => u.UpdatedAt);
        }
    }
}
