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
            builder.Property(r => r.Id);
            builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
            builder.Property(r => r.Description);
            builder.Property(r => r.CreatedAt).IsRequired();
            builder.Property(r => r.UpdatedAt);
        }
    }
}
