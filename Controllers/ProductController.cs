using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

public class ProductController : BaseController
{
    public ProductController(StoreDbContext db, CurrentUserSession session) : base(db, session) { }

    // ---------------- Product list with paging, sorting and filtering ----------------

    public async Task<IActionResult> List(string keyword, int? categoryId, int? brandId,
        decimal? minPrice, decimal? maxPrice, int? minRating, string sort, int page = 1)
    {
        // The filter forms send 0 (or an empty string) for "any", so normalise it away.
        if (categoryId.HasValue && categoryId.Value <= 0) categoryId = null;
        if (brandId.HasValue && brandId.Value <= 0) brandId = null;
        if (minRating.HasValue && minRating.Value <= 0) minRating = null;
        if (minPrice.HasValue && minPrice.Value <= 0) minPrice = null;
        if (maxPrice.HasValue && maxPrice.Value <= 0) maxPrice = null;

        var categories = CategoryTree.Flatten(await Db.Categories.Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ToListAsync());
        var brands = await Db.Brands.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToListAsync();

        var query = Db.Products.Include(p => p.Skus).Include(p => p.Brand).Include(p => p.Category)
            .Where(p => p.Status == "on");

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            query = query.Where(p => p.ProductName.ToLower().Contains(k)
                                     || (p.Description != null && p.Description.ToLower().Contains(k)));
        }

        if (categoryId.HasValue)
        {
            var ids = new List<int> { categoryId.Value };
            ids.AddRange(categories.Where(c => c.ParentId == categoryId.Value).Select(c => c.CategoryId));
            query = query.Where(p => ids.Contains(p.CategoryId));
        }

        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);

        var products = await query.ToListAsync();

        // Price filter works on the minimum SKU price.
        var cards = products.Select(p => new ProductCardVm
        {
            Product = p,
            MinPrice = p.Skus.Any() ? p.Skus.Min(s => s.Price) : 0m,
            TotalStock = p.Skus.Sum(s => s.Stock)
        }).ToList();

        var ratingStats = await Db.Reviews.Where(r => r.IsVisible)
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Sum = g.Sum(r => (int)r.Rating), Count = g.Count() })
            .ToListAsync();
        var statMap = ratingStats.ToDictionary(
            s => s.ProductId,
            s => (Avg: s.Count == 0 ? 0d : s.Sum / (double)s.Count, Count: s.Count));

        foreach (var c in cards)
        {
            var st = statMap.TryGetValue(c.Product.ProductId, out var v) ? v : (Avg: 0d, Count: 0);
            c.AvgRating = st.Avg;
            c.ReviewCount = st.Count;
        }

        if (minPrice.HasValue) cards = cards.Where(c => c.MinPrice >= minPrice.Value).ToList();
        if (maxPrice.HasValue) cards = cards.Where(c => c.MinPrice <= maxPrice.Value).ToList();
        if (minRating.HasValue) cards = cards.Where(c => c.AvgRating >= minRating.Value).ToList();

        cards = (sort ?? string.Empty) switch
        {
            "newest" => cards.OrderByDescending(c => c.Product.CreatedAt)
                .ThenByDescending(c => c.Product.ProductId).ToList(),
            "price_asc" => cards.OrderBy(c => c.MinPrice).ThenBy(c => c.Product.ProductName).ToList(),
            "price_desc" => cards.OrderByDescending(c => c.MinPrice).ThenBy(c => c.Product.ProductName).ToList(),
            "rating" => cards.OrderByDescending(c => c.AvgRating)
                .ThenByDescending(c => c.ReviewCount).ToList(),
            // Default "featured": the order the administrator arranged inside each category.
            _ => cards.OrderBy(c => c.Product.CategoryId)
                .ThenBy(c => c.Product.SortOrder)
                .ThenByDescending(c => c.Product.CreatedAt).ToList()
        };

        const int pageSize = 12;
        var total = cards.Count;
        var items = cards.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList();

        var vm = new ProductListViewModel
        {
            Items = items,
            Page = Math.Max(page, 1),
            PageSize = pageSize,
            Total = total,
            Keyword = keyword,
            CategoryId = categoryId,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinRating = minRating,
            Sort = sort,
            Categories = categories,
            Brands = brands,
            Title = categoryId.HasValue
                ? categories.FirstOrDefault(c => c.CategoryId == categoryId.Value)?.Name ?? "Products"
                : "All products"
        };

        return View(vm);
    }

    // ---------------- AJAX suggestion for the search box ----------------

    [HttpGet]
    public async Task<IActionResult> Suggest(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return Json(new object[0]);

        var k = term.Trim().ToLower();
        var items = await Db.Products
            .Where(p => p.Status == "on" && p.ProductName.ToLower().Contains(k))
            .OrderByDescending(p => p.SalesCount)
            .Take(8)
            .Select(p => new { id = p.ProductId, name = p.ProductName })
            .ToListAsync();

        return Json(items);
    }

    // ---------------- Product detail ----------------

    public async Task<IActionResult> Detail(int id, int? rating)
    {
        var product = await Db.Products
            .Include(p => p.Skus)
            .Include(p => p.Images)
            .Include(p => p.Attributes).ThenInclude(a => a.Values)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Merchant)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return NotFound();

        // Record one visit per entry and aggregate it per day.
        await TrackVisit(product.ProductId);

        var reviewQuery = Db.Reviews.Include(r => r.Customer)
            .Where(r => r.ProductId == id && r.IsVisible);
        if (rating.HasValue) reviewQuery = reviewQuery.Where(r => r.Rating == rating.Value);
        var reviews = await reviewQuery.OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync();

        var allStats = await Db.Reviews.Where(r => r.ProductId == id && r.IsVisible)
            .GroupBy(r => r.ProductId)
            .Select(g => new { Sum = g.Sum(x => (int)x.Rating), Count = g.Count() })
            .FirstOrDefaultAsync();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            Reviews = reviews,
            AvgRating = allStats == null || allStats.Count == 0 ? 0 : allStats.Sum / (double)allStats.Count,
            ReviewCount = allStats?.Count ?? 0,
            RatingFilter = rating,
            IsFavorite = IsCustomer && await Db.Favorites.AnyAsync(
                f => f.CustomerId == CurrentCustomerId && f.ProductId == id)
        };

        return View(vm);
    }

    private async Task TrackVisit(int productId)
    {
        try
        {
            if (IsCustomer)
            {
                Db.ViewLogs.Add(new ViewLog
                {
                    CustomerId = CurrentCustomerId,
                    ProductId = productId,
                    ViewedAt = AppTime.Now
                });
            }

            var today = AppTime.Today;
            var daily = await Db.DailyVisits
                .FirstOrDefaultAsync(v => v.StatDate == today && v.ProductId == productId);
            if (daily == null)
            {
                Db.DailyVisits.Add(new DailyVisit { StatDate = today, ProductId = productId, Visits = 1 });
            }
            else
            {
                daily.Visits += 1;
            }

            await Db.SaveChangesAsync();
        }
        catch
        {
            // Visit tracking must never break the product page.
        }
    }
}
