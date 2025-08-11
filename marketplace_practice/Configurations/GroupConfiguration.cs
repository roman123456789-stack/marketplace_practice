using marketplace_practice.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace marketplace_practice.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable("groups");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Id).HasColumnName("id");
            builder.Property(g => g.CategoryId).HasColumnName("category_id").IsRequired();
            builder.Property(g => g.SubcategoryId).HasColumnName("subcategory_id");

            builder.HasOne(g => g.Category)
                .WithMany(c => c.Groups)
                .HasForeignKey(g => g.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.HasOne(g => g.Subcategory)
                .WithOne(s => s.Group)
                .HasForeignKey<Group>(g => g.SubcategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
