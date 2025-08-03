using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
    {
        public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
        {
            builder.ToTable("loyalty_transactions");
            builder.HasKey(lt => lt.Id);
            builder.Property(lt => lt.Id).HasColumnName("id");
            builder.Property(lt => lt.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(lt => lt.OrderId).HasColumnName("order_id");
            builder.Property(lt => lt.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
            builder.Property(lt => lt.Points).HasColumnName("points").IsRequired();
            builder.Property(lt => lt.Description).HasColumnName("description");
            builder.Property(lt => lt.CreatedAt).HasColumnName("created_at").IsRequired();

            builder.HasOne(lt => lt.User)
                  .WithMany(u => u.LoyaltyTransactions)
                  .HasForeignKey(lt => lt.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(lt => lt.Order)
                  .WithOne(o => o.LoyaltyTransaction)
                  .HasForeignKey<LoyaltyTransaction>(lt => lt.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
