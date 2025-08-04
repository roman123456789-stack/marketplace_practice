using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("roles");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("id");
            builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder.Property(r => r.Description).HasColumnName("description");
            builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

            builder.HasMany(r => r.Users)
                .WithMany(u => u.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "user_roles",
                    j => j.HasOne<User>().WithMany()
                        .HasForeignKey("user_id")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Role>().WithMany()
                        .HasForeignKey("role_id")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasKey("user_id", "role_id")
                );
        }
    }
}
