using HardwareStore.Data;
using HardwareStore.Models;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

public class HomeController : BaseController
{
    public HomeController(StoreDbContext db, CurrentUserSession session) : base(db, session) { }

    public async Task<IActionResult> Index()
    {
        var categories = await Db.Categories.Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ToListAsync();

        var tree = categories.Where(c => c.ParentId == null)
            .Select(c => new CategoryNode
            {
                Category = c,
                Children = categories.Where(x => x.ParentId == c.CategoryId).ToList()
            }).ToList();

        var products = await Db.Products
            .Include(p => p.Skus)
            .Include(p => p.Brand)
            .Where(p => p.Status == "on")
            .ToListAsync();

        var ratingStats = await Db.Reviews.Where(r => r.IsVisible)
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Sum = g.Sum(r => (int)r.Rating), Count = g.Count() })
            .ToListAsync();

        var statMap = ratingStats.ToDictionary(
            s => s.ProductId,
            s => (Avg: s.Count == 0 ? 0d : s.Sum / (double)s.Count, Count: s.Count));

        var vm = new HomeViewModel
        {
            Banners = await Db.Banners.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToListAsync(),
            Notices = await Db.Notices.Where(n => n.IsActive)
                .OrderByDescending(n => n.CreatedAt).Take(4).ToListAsync(),
            Categories = tree
        };

        foreach (var node in tree)
        {
            var row = new HomeRow
            {
                Title = node.Category.Name,
                CategoryId = node.Category.CategoryId
            };
            var ids = new List<int> { node.Category.CategoryId };
            ids.AddRange(node.Children.Select(c => c.CategoryId));

            foreach (var p in products.Where(x => ids.Contains(x.CategoryId))
                         .OrderByDescending(x => x.SalesCount).Take(4))
            {
                var st = statMap.TryGetValue(p.ProductId, out var v) ? v : (Avg: 0d, Count: 0);
                row.Products.Add(new ProductCardVm
                {
                    Product = p,
                    MinPrice = p.Skus.Any() ? p.Skus.Min(s => s.Price) : 0m,
                    TotalStock = p.Skus.Sum(s => s.Stock),
                    AvgRating = st.Avg,
                    ReviewCount = st.Count
                });
            }

            if (row.Products.Any()) vm.Rows.Add(row);
        }

        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => Problem("An unexpected error occurred.");
}
