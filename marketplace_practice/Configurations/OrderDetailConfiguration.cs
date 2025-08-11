using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(EntityTypeBuilder<OrderDetail> builder)
        {
            builder.ToTable("order_delails");
            builder.HasKey(od => od.Id);
            builder.Property(od => od.Id).HasColumnName("id");
            builder.Property(od => od.OrderId).HasColumnName("order_id").IsRequired();
            builder.Property(od => od.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
            builder.Property(od => od.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50).IsRequired();
            builder.Property(od => od.Country).HasColumnName("country").HasMaxLength(200).IsRequired();
            builder.Property(od => od.PostalCode).HasColumnName("postal_code").IsRequired();

            builder.HasOne(od => od.Order)
                  .WithOne(o => o.OrderDetail)
                  .HasForeignKey<OrderDetail>(od => od.OrderId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();
        }
    }
}
