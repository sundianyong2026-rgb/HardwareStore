using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

/// <summary>
/// Merchant backend. Every list query is filtered by CurrentMerchantId so that a
/// merchant can only see and edit the data of its own shop.
/// </summary>
public class MerchantController : BaseController
{
    private readonly FileStorage _files;

    public MerchantController(StoreDbContext db, CurrentUserSession session, FileStorage files)
        : base(db, session)
    {
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var merchantId = CurrentMerchantId;

        var products = await Db.Products.Where(p => p.MerchantId == merchantId).ToListAsync();
        var productIds = products.Select(p => p.ProductId).ToList();

        // Exactly the orders this shop currently sees in the Orders page.
        var orders = await Db.Orders.Include(o => o.Items)
            .Where(o => o.MerchantId == merchantId && !o.HiddenByMerchant)
            .ToListAsync();

        var vm = new MerchantDashboardVm
        {
            ProductCount = products.Count,
            SkuCount = await Db.ProductSkus.Where(s => productIds.Contains(s.ProductId)).CountAsync(),
            OrderCount = orders.Count,
            SalesAmount = orders.Where(o => o.Status != "pending_payment" && o.Status != "canceled")
                .Sum(o => o.TotalAmount),
            RecentOrders = orders.OrderByDescending(o => o.CreatedAt).Take(5).ToList()
        };

        ViewBag.ShopName = Session.UserName;
        ViewBag.DeletionRequestedAt = await Db.Merchants
            .Where(m => m.MerchantId == merchantId)
            .Select(m => m.DeletionRequestedAt)
            .FirstOrDefaultAsync();
        return View(vm);
    }

    // ---------------- Shop account closure ----------------

    private IQueryable<OrderInfo> UnfinishedOrders()
        => Db.Orders.Where(o => o.MerchantId == CurrentMerchantId
                                && o.Status != "completed" && o.Status != "canceled");

    [HttpPost]
    public async Task<IActionResult> RequestDeletion()
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var open = await UnfinishedOrders().CountAsync();
        if (open > 0)
        {
            TempData["MerchantError"] =
                $"You still have {open} unfinished order(s). Ship or close them before asking for closure.";
            return RedirectToAction("Index");
        }

        var shop = await Db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == CurrentMerchantId);
        if (shop != null)
        {
            shop.DeletionRequestedAt = AppTime.Now;
            shop.IsDisabled = true;
            await Db.SaveChangesAsync();
        }

        Session.SignOut();
        TempData["MerchantMessage"] = "Your closure request was sent to the administrator.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> CancelDeletion()
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var shop = await Db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == CurrentMerchantId);
        if (shop != null && shop.DeletionRequestedAt != null)
        {
            shop.DeletionRequestedAt = null;
            shop.IsDisabled = false;
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "Your closure request was withdrawn.";
        }
        return RedirectToAction("Index");
    }

    // ---------------- Own products ----------------

    public async Task<IActionResult> Products(string keyword)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var query = Db.Products.Include(p => p.Skus).Include(p => p.Category)
            .Where(p => p.MerchantId == CurrentMerchantId);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p => p.ProductName.Contains(keyword.Trim()));

        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        ViewBag.Keyword = keyword;
        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int? id)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var activeCategories = await Db.Categories.Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ToListAsync();
        // Parent categories first, then their subcategories (nothing jumps above its parent).
        ViewBag.Categories = CategoryTree.Flatten(activeCategories);
        ViewBag.CategoryDepths = CategoryTree.Depths(activeCategories);
        ViewBag.Brands = await Db.Brands.Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ToListAsync();

        if (!id.HasValue || id.Value == 0)
            return View(new Product { Status = "on" });

        var product = await Db.Products.Include(p => p.Skus).Include(p => p.Images)
            .Include(p => p.Attributes).ThenInclude(a => a.Values)
            .FirstOrDefaultAsync(p => p.ProductId == id.Value && p.MerchantId == CurrentMerchantId);

        if (product == null)
        {
            TempData["MerchantError"] = "Product not found in your shop.";
            return RedirectToAction("Products");
        }

        return View(product);
    }

    /// <summary>Total number of pictures a product may have, main image included.</summary>
    private const int MaxProductImages = 8;

    [HttpPost]
    [RequestSizeLimit(64 * 1024 * 1024)]
    public async Task<IActionResult> EditProduct(int productId, string productName, int categoryId,
        int? brandId, string description, string status,
        List<string> attrName, List<string> attrValues,
        List<string> comboKey, List<string> comboPrice, List<string> comboStock,
        string mainImage, string clearPictures, List<string> keepImageIds, List<string> newImages)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        if (string.IsNullOrWhiteSpace(productName))
        {
            TempData["MerchantError"] = "The product name is required.";
            return RedirectToAction("EditProduct", new { id = productId });
        }

        Product product;
        if (productId > 0)
        {
            product = await Db.Products.Include(p => p.Skus).Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.MerchantId == CurrentMerchantId);

            if (product == null)
            {
                TempData["MerchantError"] = "Product not found in your shop.";
                return RedirectToAction("Products");
            }
        }
        else
        {
            product = new Product { MerchantId = CurrentMerchantId, CreatedAt = AppTime.Now };
            Db.Products.Add(product);
        }

        var categoryChanged = product.CategoryId != categoryId;

        product.ProductName = productName.Trim();
        product.CategoryId = categoryId;
        product.BrandId = brandId;
        product.Description = description;
        product.Status = status == "off" ? "off" : "on";

        // A product always lands at the end of its (possibly new) category.
        if (product.ProductId == 0 || categoryChanged)
            product.SortOrder = (await Db.Products
                .Where(p => p.CategoryId == categoryId && p.ProductId != product.ProductId)
                .MaxAsync(p => (int?)p.SortOrder) ?? 0) + 1;

        // ---- Main image: either a newly cropped data URL or the URL that was already stored ----
        if (clearPictures == "1")
            product.MainImage = null;
        else if (!string.IsNullOrWhiteSpace(mainImage))
        {
            if (mainImage.StartsWith("/"))
            {
                product.MainImage = mainImage;
            }
            else
            {
                var saved = _files.SaveCroppedDataUrl(mainImage, "products");
                if (saved != null) product.MainImage = saved;
                else TempData["MerchantError"] = "The main image could not be saved. Please try again.";
            }
        }

        await Db.SaveChangesAsync();

        // ---- Gallery: keep the pictures that survived, then append the new ones ----
        var keptIds = (keepImageIds ?? new List<string>())
            .Select(x => int.TryParse(x, out var n) ? n : 0).Where(x => x > 0).ToList();
        var pending = (newImages ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var maxExtras = MaxProductImages - 1;
        if (keptIds.Count + pending.Count > maxExtras)
        {
            pending = pending.Take(Math.Max(0, maxExtras - keptIds.Count)).ToList();
            TempData["MerchantError"] = $"A product can hold at most {MaxProductImages} pictures.";
        }

        var gallery = await Db.ProductImages
            .Where(i => i.ProductId == product.ProductId).ToListAsync();

        foreach (var img in gallery.Where(i => !keptIds.Contains(i.ImageId)))
            Db.ProductImages.Remove(img);

        var order = 1;
        foreach (var id in keptIds)
        {
            var img = gallery.FirstOrDefault(i => i.ImageId == id);
            if (img != null) img.SortOrder = order++;
        }

        var failed = 0;
        foreach (var data in pending)
        {
            var url = _files.SaveCroppedDataUrl(data, "products");
            if (url == null) { failed++; continue; }

            Db.ProductImages.Add(new ProductImage
            {
                ProductId = product.ProductId,
                ImageUrl = url,
                SortOrder = order++
            });
        }

        if (failed > 0)
            TempData["MerchantError"] = $"{failed} picture(s) could not be saved. Please try again.";

        // ---- Attribute dimensions (Color, Size, ... any number of them) ----
        var names = attrName ?? new List<string>();
        var rawValues = attrValues ?? new List<string>();

        var wanted = new List<(string name, List<string> values)>();
        for (var i = 0; i < names.Count; i++)
        {
            var n = (names[i] ?? string.Empty).Trim();
            if (n.Length == 0) continue;

            var vals = (i < rawValues.Count ? rawValues[i] ?? string.Empty : string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => v.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (vals.Count == 0) continue;
            wanted.Add((n.Length > 50 ? n.Substring(0, 50) : n, vals));
        }

        if (wanted.Count == 0)
        {
            TempData["MerchantError"] = "Add at least one attribute with one value.";
            return RedirectToAction("EditProduct", new { id = product.ProductId });
        }

        var existingAttrs = await Db.ProductAttributes.Include(a => a.Values)
            .Where(a => a.ProductId == product.ProductId).ToListAsync();

        // ordered lists keep the same order the merchant submitted, so combo keys stay valid
        var keptAttrs = new List<ProductAttribute>();
        var keptValues = new List<List<ProductAttributeValue>>();

        for (var i = 0; i < wanted.Count; i++)
        {
            var (name, values) = wanted[i];

            var attr = existingAttrs.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (attr == null)
            {
                attr = new ProductAttribute { ProductId = product.ProductId, Name = name };
                Db.ProductAttributes.Add(attr);
            }

            attr.Name = name;
            attr.SortOrder = i + 1;
            keptAttrs.Add(attr);

            var row = new List<ProductAttributeValue>();
            for (var j = 0; j < values.Count; j++)
            {
                var text = values[j].Length > 100 ? values[j].Substring(0, 100) : values[j];
                var value = attr.Values.FirstOrDefault(x => x.Value.Equals(text, StringComparison.OrdinalIgnoreCase));
                if (value == null)
                {
                    value = new ProductAttributeValue { Value = text };
                    attr.Values.Add(value);
                }
                value.Value = text;
                value.SortOrder = j + 1;
                row.Add(value);
            }
            keptValues.Add(row);

            foreach (var old in attr.Values.Where(x => !row.Contains(x)).ToList())
            {
                attr.Values.Remove(old);
                Db.ProductAttributeValues.Remove(old);
            }
        }

        foreach (var old in existingAttrs.Where(a => !keptAttrs.Contains(a)))
        {
            Db.ProductAttributeValues.RemoveRange(old.Values);
            Db.ProductAttributes.Remove(old);
        }

        // Gives new attributes / values their ids before the combinations are written.
        await Db.SaveChangesAsync();

        // ---- One SKU per combination of values ("0:1|1:2" = attribute 0 value 1 + attribute 1 value 2) ----
        var keys = comboKey ?? new List<string>();
        var wantedSkus = new List<(string spec, string valueIds, decimal price, int stock)>();

        for (var i = 0; i < keys.Count; i++)
        {
            var key = (keys[i] ?? string.Empty).Trim();
            if (key.Length == 0) continue;

            var ids = new List<int>();
            var texts = new List<string>();
            var broken = false;

            foreach (var part in key.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split(':');
                if (pair.Length != 2) { broken = true; break; }

                var ai = int.TryParse(pair[0], out var a) ? a : -1;
                var vi = int.TryParse(pair[1], out var v) ? v : -1;
                if (ai < 0 || ai >= keptValues.Count || vi < 0 || vi >= keptValues[ai].Count) { broken = true; break; }

                var value = keptValues[ai][vi];
                ids.Add(value.ValueId);
                texts.Add(value.Value);
            }

            if (broken || ids.Count != keptValues.Count) continue;

            wantedSkus.Add((
                string.Join(" / ", texts),
                string.Join(",", ids),
                decimal.TryParse(i < (comboPrice?.Count ?? 0) ? comboPrice[i] : "0",
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0m,
                int.TryParse(i < (comboStock?.Count ?? 0) ? comboStock[i] : "0", out var st) ? st : 0));
        }

        if (wantedSkus.Count == 0)
        {
            TempData["MerchantError"] = "The product needs at least one specification combination.";
            return RedirectToAction("EditProduct", new { id = product.ProductId });
        }

        var existingSkus = product.Skus.ToList();
        var usedIds = new List<int>();

        foreach (var item in wantedSkus)
        {
            var sku = existingSkus.FirstOrDefault(s => s.AttributeValueIds == item.valueIds)
                      ?? existingSkus.FirstOrDefault(s => s.AttributeValueIds == null && s.SkuSpec == item.spec);

            if (sku == null)
            {
                sku = new ProductSku { ProductId = product.ProductId };
                Db.ProductSkus.Add(sku);
            }

            sku.SkuSpec = item.spec;
            sku.AttributeValueIds = item.valueIds;
            sku.Price = item.price;
            sku.Stock = Math.Max(0, item.stock);
            usedIds.Add(sku.SkuId);
        }

        // Combinations that no longer exist are dropped, unless a cart or an order still points at them.
        foreach (var sku in existingSkus.Where(s => !usedIds.Contains(s.SkuId)))
        {
            var inCart = await Db.CartItems.AnyAsync(c => c.SkuId == sku.SkuId);
            var inOrder = await Db.OrderItems.AnyAsync(o => o.SkuId == sku.SkuId);
            if (!inCart && !inOrder) Db.ProductSkus.Remove(sku);
        }

        await Db.SaveChangesAsync();
        TempData["MerchantMessage"] = "Product saved.";
        return RedirectToAction("Products");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSku(int skuId, decimal price, int stock)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var sku = await Db.ProductSkus.Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.SkuId == skuId && s.Product.MerchantId == CurrentMerchantId);

        if (sku != null)
        {
            sku.Price = price;
            sku.Stock = Math.Max(0, stock);
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "Specification updated.";
        }
        return RedirectToAction("Products");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSku(int skuId)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var sku = await Db.ProductSkus.Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.SkuId == skuId && s.Product.MerchantId == CurrentMerchantId);

        if (sku != null)
        {
            Db.ProductSkus.Remove(sku);
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "Specification deleted.";
        }
        return RedirectToAction("Products");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleProduct(int productId)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var product = await Db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.MerchantId == CurrentMerchantId);
        if (product != null)
        {
            product.Status = product.Status == "on" ? "off" : "on";
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Products");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var product = await Db.Products.Include(p => p.Skus).Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.MerchantId == CurrentMerchantId);
        if (product != null)
        {
            Db.Products.Remove(product);
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "Product deleted.";
        }
        return RedirectToAction("Products");
    }

    // ---------------- Product images (upload / replace / clear) ----------------

    [HttpPost]
    public async Task<IActionResult> AddImage(int productId, string croppedImage, IFormFile imageFile)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var product = await Db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.MerchantId == CurrentMerchantId);
        if (product == null) return RedirectToAction("Products");

        var url = _files.SaveCroppedDataUrl(croppedImage, "products")
                  ?? _files.SaveFile(imageFile, "products");
        if (!string.IsNullOrEmpty(url))
        {
            Db.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                ImageUrl = url,
                SortOrder = product.Images.Count + 1
            });
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "Image uploaded.";
        }
        return RedirectToAction("EditProduct", new { id = productId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int imageId)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var image = await Db.ProductImages.Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ImageId == imageId && i.Product.MerchantId == CurrentMerchantId);

        if (image != null)
        {
            var productId = image.ProductId;
            Db.ProductImages.Remove(image);
            await Db.SaveChangesAsync();
            return RedirectToAction("EditProduct", new { id = productId });
        }
        return RedirectToAction("Products");
    }

    [HttpPost]
    public async Task<IActionResult> ClearImages(int productId)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var product = await Db.Products.Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.MerchantId == CurrentMerchantId);
        if (product != null)
        {
            Db.ProductImages.RemoveRange(product.Images);
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "All gallery images were cleared.";
        }
        return RedirectToAction("EditProduct", new { id = productId });
    }

    // ---------------- Own orders ----------------

    public async Task<IActionResult> Orders(string status)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var query = Db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.MerchantId == CurrentMerchantId && !o.HiddenByMerchant);

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        ViewBag.Status = status;
        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Ship(int orderId)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var order = await Db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MerchantId == CurrentMerchantId);
        if (order != null && order.Status == "paid")
        {
            order.Status = "delivered";
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "The order was marked as delivered.";
        }
        return RedirectToAction("Orders");
    }

    /// <summary>Removes a finished or cancelled order from the shop list (the record itself is kept).</summary>
    [HttpPost]
    public async Task<IActionResult> HideOrder(int orderId, string status)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var order = await Db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MerchantId == CurrentMerchantId);

        if (order != null && (order.Status == "completed" || order.Status == "canceled"))
        {
            order.HiddenByMerchant = true;
            await Db.SaveChangesAsync();
            TempData["MerchantMessage"] = "The order was removed from your list.";
        }
        else
        {
            TempData["MerchantError"] = "Only completed or cancelled orders can be removed.";
        }
        return RedirectToAction("Orders", new { status });
    }

    // ---------------- Reviews of own products ----------------

    public async Task<IActionResult> Reviews(int? rating)
    {
        var gate = GateMerchant();
        if (gate != null) return gate;

        var query = Db.Reviews.Include(r => r.Product).Include(r => r.Customer)
            .Where(r => r.Product.MerchantId == CurrentMerchantId);

        if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        ViewBag.Rating = rating;
        return View(reviews);
    }
}
