using marketplace_practice.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace marketplace_practice
{
    public class AppDbContext : IdentityDbContext<User, Role, long>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<FavoriteProduct> FavoriteProducts { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<Category> Categories { get; set; }
    }
}
