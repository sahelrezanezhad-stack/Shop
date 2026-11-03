using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Persistence
{
    public class ShopDbContext : DbContext
    {
        public ShopDbContext(DbContextOptions<ShopDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<SallyAccount> SallyAccounts => Set<SallyAccount>();
        public DbSet<SallyEmailOutbox> SallyEmailOutbox => Set<SallyEmailOutbox>();

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartLine> ShoppingCartLines { get; set; }
        public DbSet<ProductSizeStock> ProductSizeStocks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SallyAccount>(e => {
                e.ToTable("SallyAccounts");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
                e.Property(x => x.Email).HasMaxLength(256).IsRequired();
                e.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired().IsConcurrencyToken();
                e.HasIndex(x => x.NormalizedEmail).IsUnique();
                e.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired().IsConcurrencyToken();
                e.Property(x => x.VerificationTokenHash).HasMaxLength(64);
            });
            modelBuilder.Entity<SallyEmailOutbox>(e => {
                e.ToTable("SallyEmailOutbox");
                e.HasKey(x => x.Id);
                e.Property(x => x.Kind).HasMaxLength(24).IsRequired();
                e.Property(x => x.ProtectedToken).HasMaxLength(2048);
                e.Property(x => x.LastError).HasMaxLength(250);
                e.HasIndex(x => new { x.Kind, x.OrderId }).IsUnique().HasFilter("[OrderId] IS NOT NULL");
                e.HasIndex(x => x.SentUtc);
            });
            modelBuilder.Entity<Order>(e => {
                e.HasIndex(x => x.AccountId);
            });
            modelBuilder.Entity<ProductSizeStock>(b => {
                b.HasKey(x => x.Id);
                b.Property(x => x.Size).IsRequired().HasMaxLength(4);
                b.Property(x => x.Quantity).IsRequired();
                b.HasIndex(x => new { x.ProductId, x.Size }).IsUnique();
                b.HasOne(x => x.Product).WithMany(p => p.SizeStocks)
                    .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
                b.ToTable(t => t.HasCheckConstraint("CK_ProductSizeStocks_Quantity", "[Quantity] >= 0"));
            });
            modelBuilder.Entity<Order>(b => {
                b.HasIndex(x => x.SourceCartId).IsUnique().HasFilter("[SourceCartId] IS NOT NULL");
                b.Property(x => x.ShippingCity).HasMaxLength(100).IsRequired();
                b.Property(x => x.ShippingAddress).HasMaxLength(300).IsRequired();
                b.Property(x => x.ShippingPostalCode).HasMaxLength(20);
            });
            modelBuilder.Entity<OrderItem>().Property(x => x.Size).HasMaxLength(4).IsRequired();
            modelBuilder.Entity<ShoppingCart>(b => {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.HasMany(x => x.Lines).WithOne(x => x.ShoppingCart)
                    .HasForeignKey(x => x.ShoppingCartId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ShoppingCartLine>(b => {
                b.HasKey(x => x.Id);
                b.Property(x => x.Size).HasMaxLength(4).IsRequired();
                b.HasIndex(x => new { x.ShoppingCartId, x.ProductId, x.Size }).IsUnique();
                b.HasOne(x => x.Product).WithMany()
                    .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<Product>()
    .Property(p => p.Price)
    .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Orders)
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId);
        }

    }
}
