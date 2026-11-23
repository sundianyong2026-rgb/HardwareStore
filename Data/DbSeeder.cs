using HardwareStore.Models;
using HardwareStore.Models.Entities;
using HardwareStore.Services;

namespace HardwareStore.Data;

/// <summary>
/// Inserts minimal demo data when the database is empty.
/// Passwords are stored as SHA-256 hashes; the plain-text demo passwords
/// are listed in README.md for local testing.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(StoreDbContext db)
    {
        if (!db.Admins.Any())
        {
            db.Admins.Add(new Admin
            {
                Username = "admin",
                PasswordHash = PasswordHasher.Hash("Admin@123"),
                FullName = "Platform Administrator"
            });
        }

        if (!db.Customers.Any())
        {
            db.Customers.Add(new Customer
            {
                Username = "customer",
                PasswordHash = PasswordHasher.Hash("Customer@123"),
                FullName = "Demo Customer",
                Phone = "+375 29 000 0001"
            });
        }

        if (!db.Merchants.Any())
        {
            db.Merchants.Add(new Merchant
            {
                ShopName = "TechStore",
                ContactName = "John Smith",
                Phone = "+375 29 000 0002",
                Email = "techstore@example.com",
                PasswordHash = PasswordHasher.Hash("Merchant@123"),
                Description = "Demo approved hardware store.",
                Status = "approved",
                ApprovedAt = AppTime.Now
            });
        }

        if (!db.Categories.Any())
        {
            var computers = new Category { Name = "Computers", SortOrder = 1 };
            var components = new Category { Name = "Components", SortOrder = 2 };
            var peripherals = new Category { Name = "Peripherals", SortOrder = 3 };
            var others = new Category { Name = "Others", SortOrder = 4 };

            db.Categories.AddRange(computers, components, peripherals, others);
            await db.SaveChangesAsync();

            db.Categories.AddRange(
                new Category { Name = "Laptops", ParentId = computers.CategoryId, SortOrder = 1 },
                new Category { Name = "Desktops", ParentId = computers.CategoryId, SortOrder = 2 },
                new Category { Name = "Processors", ParentId = components.CategoryId, SortOrder = 1 },
                new Category { Name = "Motherboards", ParentId = components.CategoryId, SortOrder = 2 },
                new Category { Name = "Storage", ParentId = components.CategoryId, SortOrder = 3 },
                new Category { Name = "Cooling", ParentId = components.CategoryId, SortOrder = 4 },
                new Category { Name = "Mice", ParentId = peripherals.CategoryId, SortOrder = 1 },
                new Category { Name = "Keyboards", ParentId = peripherals.CategoryId, SortOrder = 2 }
            );
            await db.SaveChangesAsync();
        }

        if (!db.Brands.Any())
        {
            db.Brands.AddRange(
                new Brand { Name = "ASUS", SortOrder = 1 },
                new Brand { Name = "Dell", SortOrder = 2 },
                new Brand { Name = "Logitech", SortOrder = 3 }
            );
            await db.SaveChangesAsync();
        }

        if (!db.Products.Any())
        {
            var merchant = db.Merchants.First();
            var laptopCat = db.Categories.First(c => c.Name == "Laptops");
            var mouseCat = db.Categories.First(c => c.Name == "Mice");
            var asus = db.Brands.First(b => b.Name == "ASUS");
            var logitech = db.Brands.First(b => b.Name == "Logitech");

            var laptop = new Product
            {
                MerchantId = merchant.MerchantId,
                CategoryId = laptopCat.CategoryId,
                BrandId = asus.BrandId,
                ProductName = "ASUS Vivobook 15 Laptop",
                Description = "15.6-inch laptop, 16 GB RAM, 512 GB SSD.",
                MainImage = "/images/products/laptop-1.jpg",
                Status = "on",
                SalesCount = 12
            };
            laptop.Skus.Add(new ProductSku
            {
                SkuSpec = "16GB / 512GB",
                Price = 899.00m,
                Stock = 20,
                ImageUrl = "/images/products/laptop-1.jpg"
            });
            laptop.Images.Add(new ProductImage { ImageUrl = "/images/products/laptop-1.jpg", SortOrder = 1 });

            var mouse = new Product
            {
                MerchantId = merchant.MerchantId,
                CategoryId = mouseCat.CategoryId,
                BrandId = logitech.BrandId,
                ProductName = "Logitech MX Master 3S Mouse",
                Description = "Wireless ergonomic mouse, silent clicks.",
                MainImage = "/images/products/mouse-1.jpg",
                Status = "on",
                SalesCount = 35
            };
            mouse.Skus.Add(new ProductSku { SkuSpec = "Black", Price = 79.90m, Stock = 50, ImageUrl = "/images/products/mouse-1.jpg" });
            mouse.Skus.Add(new ProductSku { SkuSpec = "Grey", Price = 79.90m, Stock = 30, ImageUrl = "/images/products/mouse-2.jpg" });
            mouse.Images.Add(new ProductImage { ImageUrl = "/images/products/mouse-1.jpg", SortOrder = 1 });

            db.Products.AddRange(laptop, mouse);
            await db.SaveChangesAsync();
        }

        if (!db.Banners.Any())
        {
            db.Banners.AddRange(
                new Banner { Title = "Spring Sale", ImageUrl = "/images/banners/banner-1.jpg", LinkUrl = "/Product/List", SortOrder = 1 },
                new Banner { Title = "New Arrivals", ImageUrl = "/images/banners/banner-2.jpg", LinkUrl = "/Product/List", SortOrder = 2 }
            );
        }

        if (!db.Notices.Any())
        {
            db.Notices.Add(new Notice
            {
                Title = "Welcome",
                Content = "Free shipping on all orders above 99."
            });
        }

        await db.SaveChangesAsync();
    }
}
