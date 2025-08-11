using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
    {
        public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
        {
            builder.ToTable("loyalty_accounts");
            builder.HasKey(la => la.Id);
            builder.Property(la => la.Id).HasColumnName("id");
            builder.Property(la => la.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(la => la.Balance).HasColumnName("balance").IsRequired();
            builder.Property(la => la.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(la => la.UpdatedAt).HasColumnName("updated_at");

            builder.HasOne(la => la.User)
                  .WithOne(u => u.LoyaltyAccount)
                  .HasForeignKey<LoyaltyAccount>(la => la.UserId)
                  .IsRequired();
        }
    }
}
