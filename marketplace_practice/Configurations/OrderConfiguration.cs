using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("orders");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).HasColumnName("id");
            builder.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
