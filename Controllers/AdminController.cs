using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

/// <summary>Super-administrator backend: platform wide management and statistics.</summary>
public class AdminController : BaseController
{
    private readonly FileStorage _files;

    public AdminController(StoreDbContext db, CurrentUserSession session, FileStorage files)
        : base(db, session)
    {
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var orders = await Db.Orders.ToListAsync();

        var vm = new AdminDashboardVm
        {
            CustomerCount = await Db.Customers.CountAsync(),
            MerchantCount = await Db.Merchants.CountAsync(m => m.Status == "approved"),
            PendingMerchantCount = await Db.Merchants.CountAsync(m => m.Status == "pending"),
            ProductCount = await Db.Products.CountAsync(),
            OrderCount = orders.Count,
            SalesAmount = orders.Where(o => o.Status != "pending_payment" && o.Status != "canceled")
                .Sum(o => o.TotalAmount),
            PendingReviewCount = await Db.Reviews.CountAsync(r => !r.IsVisible)
        };

        BuildCharts(vm, orders, await Db.DailyVisits.ToListAsync(), 7);
        return View(vm);
    }

    // ---------------- Merchants ----------------

    public async Task<IActionResult> Merchants(string status)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var query = Db.Merchants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(m => m.Status == status);

        var list = await query.OrderByDescending(m => m.DeletionRequestedAt != null)
            .ThenByDescending(m => m.SubmittedAt).ToListAsync();
        ViewBag.Status = status;
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveMerchant(int merchantId, string decision)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var merchant = await Db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant != null)
        {
            merchant.Status = decision == "reject" ? "rejected" : "approved";
            merchant.ApprovedAt = AppTime.Now;
            merchant.ApprovedBy = Session.UserId;
            await Db.SaveChangesAsync();
            TempData["AdminMessage"] = $"Shop \"{merchant.ShopName}\" was {merchant.Status}.";
        }
        return RedirectToAction("Merchants");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleMerchant(int merchantId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var merchant = await Db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant != null)
        {
            merchant.IsDisabled = !merchant.IsDisabled;
            await Db.SaveChangesAsync();
            TempData["AdminMessage"] = merchant.IsDisabled
                ? $"Shop \"{merchant.ShopName}\" was disabled."
                : $"Shop \"{merchant.ShopName}\" was enabled again.";
        }
        return RedirectToAction("Merchants");
    }

    [HttpPost]
    public async Task<IActionResult> ResetMerchantPassword(int merchantId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var merchant = await Db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant != null)
        {
            merchant.PasswordHash = PasswordHasher.Hash("Reset@123");
            await Db.SaveChangesAsync();
            TempData["AdminMessage"] = "The shop password was reset to Reset@123.";
        }
        return RedirectToAction("Merchants");
    }

    // ---------------- Account closure requests ----------------

    /// <summary>Decides a closure request. Accounts referenced by orders are anonymised instead of deleted.</summary>
    [HttpPost]
    public async Task<IActionResult> DecideDeletion(string role, int id, string decision)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        if (role == "customer")
        {
            var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return RedirectToAction("Customers");

            if (decision == "reject")
            {
                customer.DeletionRequestedAt = null;
                customer.IsDisabled = false;
                await Db.SaveChangesAsync();
                TempData["AdminMessage"] = "The closure request was rejected; the account works again.";
                return RedirectToAction("Customers");
            }

            var hasOrders = await Db.Orders.AnyAsync(o => o.CustomerId == id);

            Db.Messages.RemoveRange(Db.Messages.Where(m =>
                (m.SenderRole == "customer" && m.SenderId == id) ||
                (m.ReceiverRole == "customer" && m.ReceiverId == id)));

            if (!hasOrders)
            {
                Db.Favorites.RemoveRange(Db.Favorites.Where(f => f.CustomerId == id));
                Db.CartItems.RemoveRange(Db.CartItems.Where(c => c.CustomerId == id));
                Db.ViewLogs.RemoveRange(Db.ViewLogs.Where(v => v.CustomerId == id));
                Db.Addresses.RemoveRange(Db.Addresses.Where(a => a.CustomerId == id));
                Db.Reviews.RemoveRange(Db.Reviews.Where(r => r.CustomerId == id));
                Db.Customers.Remove(customer);
                await Db.SaveChangesAsync();
                TempData["AdminMessage"] = "The account was closed and removed.";
            }
            else
            {
                customer.Username = "closed_" + id;
                customer.FullName = null;
                customer.Phone = null;
                customer.PasswordHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N"));
                customer.IsDisabled = true;
                customer.DeletionRequestedAt = null;
                await Db.SaveChangesAsync();
                TempData["AdminMessage"] = "The account was closed. Order history is kept, so the profile was anonymised.";
            }
            return RedirectToAction("Customers");
        }

        if (role == "merchant")
        {
            var merchant = await Db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == id);
            if (merchant == null) return RedirectToAction("Merchants");

            if (decision == "reject")
            {
                merchant.DeletionRequestedAt = null;
                merchant.IsDisabled = false;
                await Db.SaveChangesAsync();
                TempData["AdminMessage"] = "The shop closure request was rejected.";
                return RedirectToAction("Merchants");
            }

            Db.Messages.RemoveRange(Db.Messages.Where(m =>
                (m.SenderRole == "merchant" && m.SenderId == id) ||
                (m.ReceiverRole == "merchant" && m.ReceiverId == id)));

            var hasProducts = await Db.Products.AnyAsync(p => p.MerchantId == id);
            var hasOrders = await Db.Orders.AnyAsync(o => o.MerchantId == id);

            if (!hasProducts && !hasOrders)
            {
                Db.Merchants.Remove(merchant);
                await Db.SaveChangesAsync();
                TempData["AdminMessage"] = "The shop account was closed and removed.";
            }
            else
            {
                merchant.ShopName = "closed_shop_" + id;
                merchant.ContactName = null;
                merchant.Phone = null;
                merchant.Email = "closed_" + id + "@example.com";
                merchant.Description = null;
                merchant.LogoUrl = null;
                merchant.PasswordHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N"));
                merchant.IsDisabled = true;
                merchant.DeletionRequestedAt = null;
                await Db.SaveChangesAsync();
                TempData["AdminMessage"] = "The shop was closed. Existing products and orders are kept, so the shop record was anonymised.";
            }
            return RedirectToAction("Merchants");
        }

        return RedirectToAction("Index");
    }

    // ---------------- Categories and brands ----------------

    public async Task<IActionResult> Catalog(int? categoryIdFilter)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var allCategories = await Db.Categories.OrderBy(c => c.SortOrder).ThenBy(c => c.CategoryId).ToListAsync();
        // The list is shown as a tree: every category sits directly under its parent.
        ViewBag.Categories = CategoryTree.Flatten(allCategories);
        ViewBag.Brands = await Db.Brands.OrderBy(b => b.SortOrder).ToListAsync();

        var categoryId = categoryIdFilter ?? 0;
        if (categoryId > 0)
            ViewBag.CategoryProducts = await Db.Products.Include(p => p.Merchant)
                .Where(p => p.CategoryId == categoryId)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.ProductId).ToListAsync();
        else
            ViewBag.CategoryProducts = new List<Product>();

        ViewBag.CategoryId = categoryId;
        return View();
    }

    /// <summary>True when <paramref name="candidateId"/> is the category itself or one of its descendants.</summary>
    private static bool IsSelfOrDescendant(List<Category> all, int categoryId, int? candidateId)
    {
        if (candidateId == null || candidateId.Value == 0) return false;
        if (candidateId.Value == categoryId) return true;

        var parentOf = all.ToDictionary(c => c.CategoryId, c => c.ParentId);
        var cursor = candidateId;
        var guard = 0;
        while (cursor != null && guard++ < 50)
        {
            if (cursor.Value == categoryId) return true;
            cursor = parentOf.TryGetValue(cursor.Value, out var pp) ? pp : null;
        }
        return false;
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory(int? categoryId, string name, int? parentId, bool isActive)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        if (string.IsNullOrWhiteSpace(name)) return RedirectToAction("Catalog");

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            var c = await Db.Categories.FirstOrDefaultAsync(x => x.CategoryId == categoryId.Value);
            if (c != null)
            {
                var all = await Db.Categories.ToListAsync();
                if (IsSelfOrDescendant(all, c.CategoryId, parentId))
                    parentId = null; // a category can never become a child of itself

                c.Name = name.Trim();
                c.ParentId = parentId;
                c.IsActive = isActive;
                // SortOrder is managed only by drag and drop.
            }
        }
        else
        {
            var next = (await Db.Categories
                .Where(c => c.ParentId == parentId)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0) + 1;
            Db.Categories.Add(new Category
            {
                Name = name.Trim(),
                ParentId = parentId,
                SortOrder = next,
                IsActive = isActive
            });
        }

        await Db.SaveChangesAsync();
        TempData["AdminMessage"] = "Category saved.";
        return RedirectToAction("Catalog");
    }

    /// <summary>Persists the order produced by dragging category rows in the admin catalogue page.</summary>
    [HttpPost]
    public async Task<IActionResult> ReorderCategories(string ids)
    {
        var gate = GateAdmin();
        if (gate != null) return Unauthorized();

        var order = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var v) ? v : 0)
            .Where(v => v > 0)
            .ToList();

        if (!order.Any()) return BadRequest();

        var categories = await Db.Categories.ToListAsync();
        for (var i = 0; i < order.Count; i++)
        {
            var category = categories.FirstOrDefault(c => c.CategoryId == order[i]);
            if (category != null) category.SortOrder = i + 1;
        }

        await Db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    /// <summary>Persists the order produced by dragging brand cards in the admin catalogue page.</summary>
    /// <summary>Positions of the products inside one category, changed by drag and drop.</summary>
    [HttpPost]
    public async Task<IActionResult> ReorderProducts(string ids)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        if (string.IsNullOrWhiteSpace(ids)) return Json(new { ok = false });

        var list = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(x => x > 0).ToList();

        for (var i = 0; i < list.Count; i++)
        {
            var product = await Db.Products.FirstOrDefaultAsync(p => p.ProductId == list[i]);
            if (product != null) product.SortOrder = i + 1;
        }

        await Db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> ReorderBrands(string ids)
    {
        var gate = GateAdmin();
        if (gate != null) return Unauthorized();

        var order = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var v) ? v : 0)
            .Where(v => v > 0)
            .ToList();

        if (!order.Any()) return BadRequest();

        var brands = await Db.Brands.ToListAsync();
        for (var i = 0; i < order.Count; i++)
        {
            var brand = brands.FirstOrDefault(b => b.BrandId == order[i]);
            if (brand != null) brand.SortOrder = i + 1;
        }

        await Db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var used = await Db.Products.AnyAsync(p => p.CategoryId == categoryId);
        if (used)
        {
            TempData["AdminError"] = "This category still contains products.";
            return RedirectToAction("Catalog");
        }

        var children = await Db.Categories.AnyAsync(c => c.ParentId == categoryId);
        if (children)
        {
            TempData["AdminError"] = "Delete the subcategories first.";
            return RedirectToAction("Catalog");
        }

        var category = await Db.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category != null)
        {
            Db.Categories.Remove(category);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Catalog");
    }

    [HttpPost]
    public async Task<IActionResult> SaveBrand(int? brandId, string name,
        bool isActive, string croppedImage, IFormFile logoFile)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        if (string.IsNullOrWhiteSpace(name)) return RedirectToAction("Catalog");

        Brand brand;
        if (brandId.HasValue && brandId.Value > 0)
        {
            brand = await Db.Brands.FirstOrDefaultAsync(b => b.BrandId == brandId.Value);
            if (brand == null) return RedirectToAction("Catalog");
        }
        else
        {
            brand = new Brand
            {
                // SortOrder is managed only by drag and drop.
                SortOrder = (await Db.Brands.MaxAsync(b => (int?)b.SortOrder) ?? 0) + 1
            };
            Db.Brands.Add(brand);
        }

        brand.Name = name.Trim();
        brand.IsActive = isActive;

        var logo = _files.SaveCroppedDataUrl(croppedImage, "brands") ?? _files.SaveFile(logoFile, "brands");
        if (!string.IsNullOrEmpty(logo)) brand.LogoUrl = logo;

        await Db.SaveChangesAsync();
        TempData["AdminMessage"] = "Brand saved.";
        return RedirectToAction("Catalog");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBrand(int brandId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        if (await Db.Products.AnyAsync(p => p.BrandId == brandId))
        {
            TempData["AdminError"] = "This brand is still used by products.";
            return RedirectToAction("Catalog");
        }

        var brand = await Db.Brands.FirstOrDefaultAsync(b => b.BrandId == brandId);
        if (brand != null)
        {
            Db.Brands.Remove(brand);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Catalog");
    }

    // ---------------- Banners and notices ----------------

    public async Task<IActionResult> Content()
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        ViewBag.Banners = await Db.Banners.OrderBy(b => b.SortOrder).ToListAsync();
        ViewBag.Notices = await Db.Notices.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SaveBanner(int? bannerId, string title, string linkUrl,
        int sortOrder, bool isActive, string croppedImage, IFormFile imageFile)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        Banner banner;
        if (bannerId.HasValue && bannerId.Value > 0)
        {
            banner = await Db.Banners.FirstOrDefaultAsync(b => b.BannerId == bannerId.Value);
            if (banner == null) return RedirectToAction("Content");
        }
        else
        {
            banner = new Banner { ImageUrl = "/images/banners/.gitkeep" };
            Db.Banners.Add(banner);
        }

        banner.Title = title;
        banner.LinkUrl = linkUrl;
        banner.SortOrder = sortOrder;
        banner.IsActive = isActive;

        var url = _files.SaveCroppedDataUrl(croppedImage, "banners") ?? _files.SaveFile(imageFile, "banners");
        if (!string.IsNullOrEmpty(url)) banner.ImageUrl = url;

        await Db.SaveChangesAsync();
        TempData["AdminMessage"] = "Banner saved.";
        return RedirectToAction("Content");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBanner(int bannerId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var banner = await Db.Banners.FirstOrDefaultAsync(b => b.BannerId == bannerId);
        if (banner != null)
        {
            Db.Banners.Remove(banner);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Content");
    }

    [HttpPost]
    public async Task<IActionResult> SaveNotice(int? noticeId, string title, string content, bool isActive)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        if (string.IsNullOrWhiteSpace(title)) return RedirectToAction("Content");

        Notice notice;
        if (noticeId.HasValue && noticeId.Value > 0)
        {
            notice = await Db.Notices.FirstOrDefaultAsync(n => n.NoticeId == noticeId.Value);
            if (notice == null) return RedirectToAction("Content");
        }
        else
        {
            notice = new Notice();
            Db.Notices.Add(notice);
        }

        notice.Title = title.Trim();
        notice.Content = content;
        notice.IsActive = isActive;

        await Db.SaveChangesAsync();
        TempData["AdminMessage"] = "Notice saved.";
        return RedirectToAction("Content");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteNotice(int noticeId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var notice = await Db.Notices.FirstOrDefaultAsync(n => n.NoticeId == noticeId);
        if (notice != null)
        {
            Db.Notices.Remove(notice);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Content");
    }

    // ---------------- Review moderation ----------------

    public async Task<IActionResult> Reviews(bool? hidden)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var query = Db.Reviews.Include(r => r.Product).Include(r => r.Customer).AsQueryable();
        if (hidden.HasValue) query = query.Where(r => r.IsVisible == !hidden.Value);

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        ViewBag.Hidden = hidden;
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> ModerateReview(int reviewId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var review = await Db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
        if (review != null)
        {
            review.IsVisible = !review.IsVisible;
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Reviews");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var review = await Db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);
        if (review != null)
        {
            Db.Reviews.Remove(review);
            await Db.SaveChangesAsync();
            TempData["AdminMessage"] = "Violating review deleted.";
        }
        return RedirectToAction("Reviews");
    }

    // ---------------- Customers ----------------

    public async Task<IActionResult> Customers(string keyword, int page = 1)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        const int pageSize = 15;
        var query = Db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(c => c.Username.Contains(keyword.Trim()) || c.Phone.Contains(keyword.Trim()));

        var total = await query.CountAsync();
        var list = await query.OrderByDescending(c => c.DeletionRequestedAt != null)
            .ThenByDescending(c => c.RegisteredAt)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Keyword = keyword;
        ViewBag.Page = Math.Max(page, 1);
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleCustomer(int customerId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer != null)
        {
            customer.IsDisabled = !customer.IsDisabled;
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Customers");
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int customerId)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        if (customer != null)
        {
            customer.PasswordHash = PasswordHasher.Hash("Reset@123");
            await Db.SaveChangesAsync();
            TempData["AdminMessage"] = "The password was reset to Reset@123.";
        }
        return RedirectToAction("Customers");
    }

    // ---------------- Statistics with period filter ----------------

    public async Task<IActionResult> Stats(string from, string to)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var toDate = string.IsNullOrWhiteSpace(to)
            ? AppTime.Today
            : DateTime.Parse(to).Date;
        var fromDate = string.IsNullOrWhiteSpace(from)
            ? toDate.AddDays(-6)
            : DateTime.Parse(from).Date;

        if (fromDate > toDate) fromDate = toDate.AddDays(-6);

        var orders = await Db.Orders.Where(o => o.CreatedAt >= fromDate
                                                && o.CreatedAt < toDate.AddDays(1)).ToListAsync();
        var visits = await Db.DailyVisits.Where(v => v.StatDate >= fromDate
                                                     && v.StatDate <= toDate).ToListAsync();

        var vm = new StatsViewModel
        {
            From = fromDate.ToString("yyyy-MM-dd"),
            To = toDate.ToString("yyyy-MM-dd"),
            OrderCount = orders.Count,
            SalesAmount = orders.Where(o => o.Status != "pending_payment" && o.Status != "canceled")
                .Sum(o => o.TotalAmount),
            VisitCount = visits.Sum(v => v.Visits)
        };

        BuildCharts(vm, orders, visits, (toDate - fromDate).Days + 1, fromDate);

        var products = await Db.Products.Include(p => p.Skus)
            .OrderByDescending(p => p.SalesCount).Take(10).ToListAsync();
        vm.TopProducts = products.Select(p => new ProductCardVm
        {
            Product = p,
            MinPrice = p.Skus.Any() ? p.Skus.Min(s => s.Price) : 0m,
            TotalStock = p.Skus.Sum(s => s.Stock)
        }).ToList();

        return View(vm);
    }

    /// <summary>Builds the Chart.js series for sales amounts and daily visits.</summary>
    private void BuildCharts(AdminDashboardVm vm, List<OrderInfo> orders,
        List<DailyVisit> visits, int days, DateTime start = default)
    {
        if (start == default) start = AppTime.Today.AddDays(-(days - 1));

        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            vm.SalesLabels.Add(date.ToString("MM-dd"));
            vm.SalesValues.Add(orders
                .Where(o => o.CreatedAt.Date == date && o.Status != "pending_payment" && o.Status != "canceled")
                .Sum(o => o.TotalAmount));
            vm.VisitLabels.Add(date.ToString("MM-dd"));
            vm.VisitValues.Add(visits.Where(v => v.StatDate.Date == date).Sum(v => v.Visits));
        }
    }

    private void BuildCharts(StatsViewModel vm, List<OrderInfo> orders,
        List<DailyVisit> visits, int days, DateTime start)
    {
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            vm.Labels.Add(date.ToString("MM-dd"));
            vm.SalesValues.Add(orders
                .Where(o => o.CreatedAt.Date == date && o.Status != "pending_payment" && o.Status != "canceled")
                .Sum(o => o.TotalAmount));
            vm.VisitValues.Add(visits.Where(v => v.StatDate.Date == date).Sum(v => v.Visits));
        }
    }

    // ---------------- Support messages ----------------

    public IActionResult Messages()
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        // The old one-way contact box was replaced by the shared message inbox.
        return RedirectToAction("Index", "Message");
    }

    [HttpPost]
    public async Task<IActionResult> ReplyMessage(int messageId, string reply)
    {
        var gate = GateAdmin();
        if (gate != null) return gate;

        var message = await Db.ContactMessages.FirstOrDefaultAsync(m => m.MessageId == messageId);
        if (message != null && !string.IsNullOrWhiteSpace(reply))
        {
            message.Reply = reply.Trim();
            message.Status = "answered";
            message.RepliedAt = AppTime.Now;
            await Db.SaveChangesAsync();
            TempData["AdminMessage"] = "Reply sent.";
        }
        return RedirectToAction("Messages");
    }
}
