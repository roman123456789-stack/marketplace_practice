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

            builder.HasData(
                new Role
                {
                    Id = 1,
                    Name = "MainAdmin",
                    NormalizedName = "MAINADMIN",
                    Description = "Главный администратор системы. Полный доступ."
                },
                new Role
                {
                    Id = 2,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "Администратор системы. Доступ к управлению ресурсами."
                },
                new Role
                {
                    Id = 3,
                    Name = "Buyer",
                    NormalizedName = "BUYER",
                    Description = "Покупатель. Базовые права на совершение покупок."
                }
            );
        }
    }
}
