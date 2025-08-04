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
            builder.Property(u => u.Id).HasColumnName("id");
            builder.Property(u => u.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            builder.Property(u => u.LastName).HasColumnName("last_name").HasMaxLength(100);
            builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
            builder.Property(u => u.RefreshToken).HasColumnName("refresh_token");
            builder.Property(u => u.ExpiresAt).HasColumnName("expires_at");
            builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(200).IsRequired();
            builder.Property(u => u.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(u => u.IsVerified).HasColumnName("is_verified").IsRequired();
            builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
