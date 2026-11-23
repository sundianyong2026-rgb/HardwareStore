using HardwareStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Data;

public class StoreDbContext : DbContext
{
    public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options) { }

    public DbSet<Admin> Admins { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Merchant> Merchants { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductSku> ProductSkus { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<OrderInfo> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<ViewLog> ViewLogs { get; set; }
    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<Notice> Notices { get; set; }
    public DbSet<DailyVisit> DailyVisits { get; set; }
    public DbSet<PriceChangeLog> PriceChangeLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite primary key for the aggregated daily visit table.
        modelBuilder.Entity<DailyVisit>()
            .HasKey(v => new { v.StatDate, v.ProductId });

        // Unique constraints.
        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Username).IsUnique();
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Username).IsUnique();
        modelBuilder.Entity<Merchant>()
            .HasIndex(m => m.Email).IsUnique();
        modelBuilder.Entity<Brand>()
            .HasIndex(b => b.Name).IsUnique();
        modelBuilder.Entity<OrderInfo>()
            .HasIndex(o => o.OrderNo).IsUnique();
        modelBuilder.Entity<CartItem>()
            .HasIndex(x => new { x.CustomerId, x.ProductId, x.SkuId }).IsUnique();
        modelBuilder.Entity<Favorite>()
            .HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();

        // Check constraints matching the course work schema.
        modelBuilder.Entity<Merchant>().ToTable(t => t.HasCheckConstraint(
            "ck_merchant_status",
            "status in ('pending','approved','rejected')"));
        modelBuilder.Entity<Product>().ToTable(t => t.HasCheckConstraint(
            "ck_product_status",
            "status in ('on','off')"));
        modelBuilder.Entity<ProductSku>().ToTable(t => t.HasCheckConstraint(
            "ck_sku_stock", "stock >= 0"));
        modelBuilder.Entity<OrderInfo>().ToTable(t => t.HasCheckConstraint(
            "ck_order_status",
            "status in ('pending_payment','paid','delivered','received','completed','canceled')"));
        modelBuilder.Entity<Review>().ToTable(t => t.HasCheckConstraint(
            "ck_review_rating", "rating between 1 and 5"));
        modelBuilder.Entity<CartItem>().ToTable(t => t.HasCheckConstraint(
            "ck_cart_quantity", "quantity > 0"));
        modelBuilder.Entity<ContactMessage>().ToTable(t => t.HasCheckConstraint(
            "ck_message_status", "status in ('new','answered')"));
    }
}
