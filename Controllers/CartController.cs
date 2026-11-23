using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

public class CartController : BaseController
{
    public CartController(StoreDbContext db, CurrentUserSession session) : base(db, session) { }

    public async Task<IActionResult> Index()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        return View(await BuildCart());
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int skuId, int quantity = 1)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var sku = await Db.ProductSkus.FirstOrDefaultAsync(s => s.SkuId == skuId && s.ProductId == productId);
        if (sku == null)
        {
            TempData["CartMessage"] = "Please choose a valid specification.";
            return RedirectToAction("Detail", "Product", new { id = productId });
        }

        quantity = Math.Max(1, quantity);
        if (sku.Stock < quantity)
        {
            TempData["CartMessage"] = $"Only {sku.Stock} item(s) left in stock.";
            return RedirectToAction("Detail", "Product", new { id = productId });
        }

        var existing = await Db.CartItems.FirstOrDefaultAsync(c =>
            c.CustomerId == CurrentCustomerId && c.ProductId == productId && c.SkuId == skuId);

        if (existing == null)
        {
            Db.CartItems.Add(new CartItem
            {
                CustomerId = CurrentCustomerId,
                ProductId = productId,
                SkuId = skuId,
                Quantity = quantity
            });
        }
        else
        {
            if (existing.Quantity + quantity > sku.Stock)
            {
                TempData["CartMessage"] = $"Only {sku.Stock} item(s) left in stock.";
                return RedirectToAction("Index");
            }
            existing.Quantity += quantity;
        }

        await Db.SaveChangesAsync();
        TempData["CartMessage"] = "Added to cart.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var item = await Db.CartItems.Include(c => c.Sku)
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.CustomerId == CurrentCustomerId);
        if (item == null) return RedirectToAction("Index");

        quantity = Math.Max(1, quantity);
        if (item.Sku != null && quantity > item.Sku.Stock)
        {
            TempData["CartMessage"] = $"Only {item.Sku.Stock} item(s) left in stock.";
            return RedirectToAction("Index");
        }

        item.Quantity = quantity;
        await Db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var item = await Db.CartItems
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.CustomerId == CurrentCustomerId);
        if (item != null)
        {
            Db.CartItems.Remove(item);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    private async Task<CartViewModel> BuildCart()
    {
        var items = await Db.CartItems
            .Include(c => c.Product)
            .Include(c => c.Sku)
            .Where(c => c.CustomerId == CurrentCustomerId)
            .OrderByDescending(c => c.CartItemId)
            .ToListAsync();

        return new CartViewModel
        {
            Lines = items.Select(i => new CartLineVm
            {
                Item = i,
                Product = i.Product,
                Sku = i.Sku
            }).ToList()
        };
    }
}
