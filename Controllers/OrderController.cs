using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

public class OrderController : BaseController
{
    public OrderController(StoreDbContext db, CurrentUserSession session) : base(db, session) { }

    // ---------------- Checkout page ----------------

    public async Task<IActionResult> Checkout(string ids)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var lines = await LoadLines(ids);
        if (!lines.Any())
        {
            TempData["CartMessage"] = "Please select at least one product.";
            return RedirectToAction("Index", "Cart");
        }

        var addresses = await Db.Addresses
            .Where(a => a.CustomerId == CurrentCustomerId)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync();

        var vm = new CheckoutViewModel
        {
            Lines = lines,
            Addresses = addresses,
            AddressId = addresses.FirstOrDefault(a => a.IsDefault)?.AddressId
                       ?? addresses.FirstOrDefault()?.AddressId,
            SelectedIds = string.Join(",", lines.Select(l => l.Item.CartItemId))
        };

        return View(vm);
    }

    // ---------------- Place order (split per merchant) ----------------

    [HttpPost]
    public async Task<IActionResult> Place(string selectedIds, int addressId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var address = await Db.Addresses
            .FirstOrDefaultAsync(a => a.AddressId == addressId && a.CustomerId == CurrentCustomerId);
        if (address == null)
        {
            TempData["CartMessage"] = "Please choose a delivery address.";
            return RedirectToAction("Checkout", new { ids = selectedIds });
        }

        var lines = await LoadLines(selectedIds);
        if (!lines.Any()) return RedirectToAction("Index", "Cart");

        // Second stock validation before creating the order.
        foreach (var line in lines)
        {
            var sku = await Db.ProductSkus.FirstOrDefaultAsync(s => s.SkuId == line.Item.SkuId);
            if (sku == null || sku.Stock < line.Item.Quantity)
            {
                TempData["CartMessage"] = $"Insufficient stock for {line.Product.ProductName}.";
                return RedirectToAction("Index", "Cart");
            }
        }

        await using var tx = await Db.Database.BeginTransactionAsync();
        try
        {
            var createdOrders = new List<int>();

            foreach (var group in lines.GroupBy(l => l.Product.MerchantId))
            {
                var goodsTotal = group.Sum(l => l.Sku.Price * l.Item.Quantity);
                var shipping = goodsTotal >= 99m ? 0m : 10m;

                var order = new OrderInfo
                {
                    OrderNo = "HS" + AppTime.Now.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..4],
                    CustomerId = CurrentCustomerId,
                    MerchantId = group.Key,
                    ReceiverName = address.ReceiverName,
                    Phone = address.Phone,
                    AddressDetail = address.AddressDetail,
                    TotalAmount = goodsTotal + shipping,
                    ShippingFee = shipping,
                    Status = "pending_payment",
                    CreatedAt = AppTime.Now
                };
                Db.Orders.Add(order);
                await Db.SaveChangesAsync();

                foreach (var line in group)
                {
                    var skuEntity = await Db.ProductSkus.FirstAsync(s => s.SkuId == line.Item.SkuId);
                    Db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = line.Product.ProductId,
                        SkuId = skuEntity.SkuId,
                        SkuSpecSnapshot = skuEntity.SkuSpec,
                        UnitPrice = skuEntity.Price,
                        Quantity = line.Item.Quantity,
                        Subtotal = skuEntity.Price * line.Item.Quantity
                    });

                    // Stock is kept only on the SKU level.
                    skuEntity.Stock -= line.Item.Quantity;

                    var product = await Db.Products.FirstAsync(p => p.ProductId == line.Product.ProductId);
                    product.SalesCount += line.Item.Quantity;
                }

                await Db.SaveChangesAsync();
                createdOrders.Add(order.OrderId);
            }

            foreach (var line in lines)
            {
                Db.CartItems.Remove(line.Item);
            }
            await Db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["OrderMessage"] = "Order placed. Please complete the payment.";
            return RedirectToAction("MyOrders");
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["CartMessage"] = "The order could not be created. Please try again.";
            return RedirectToAction("Index", "Cart");
        }
    }

    // ---------------- Order centre ----------------

    public async Task<IActionResult> MyOrders(string status)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var query = Db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Merchant)
            .Where(o => o.CustomerId == CurrentCustomerId && !o.HiddenByCustomer);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        return View(new MyOrdersViewModel { Orders = orders, Status = status });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var order = await Db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Merchant)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == CurrentCustomerId);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> Pay(int orderId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var order = await Db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == CurrentCustomerId);
        if (order != null && order.Status == "pending_payment")
        {
            order.Status = "paid";
            order.PaidAt = AppTime.Now;
            await Db.SaveChangesAsync();
            TempData["OrderMessage"] = "Payment completed (simulated).";
        }
        return RedirectToAction("MyOrders");
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var order = await Db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == CurrentCustomerId);

        if (order == null || order.Status == "canceled" || order.Status == "completed")
            return RedirectToAction("MyOrders");

        await using var tx = await Db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in order.Items)
            {
                var sku = await Db.ProductSkus.FirstOrDefaultAsync(s => s.SkuId == item.SkuId);
                if (sku != null) sku.Stock += item.Quantity;

                var product = await Db.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product != null) product.SalesCount = Math.Max(0, product.SalesCount - item.Quantity);
            }

            order.Status = "canceled";
            await Db.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["OrderMessage"] = "The order was cancelled and the stock was restored.";
        }
        catch
        {
            await tx.RollbackAsync();
        }

        return RedirectToAction("MyOrders");
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmReceive(int orderId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var order = await Db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == CurrentCustomerId);
        if (order != null && order.Status == "delivered")
        {
            order.Status = "received";
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("MyOrders");
    }

    [HttpPost]
    public async Task<IActionResult> Complete(int orderId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var order = await Db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == CurrentCustomerId);
        if (order != null && order.Status == "received")
        {
            order.Status = "completed";
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("MyOrders");
    }

    /// <summary>Removes a finished or cancelled order from the customer list (the record itself is kept).</summary>
    [HttpPost]
    public async Task<IActionResult> HideOrder(int orderId, string status)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var order = await Db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == CurrentCustomerId);

        if (order != null && (order.Status == "completed" || order.Status == "canceled"))
        {
            order.HiddenByCustomer = true;
            await Db.SaveChangesAsync();
            TempData["OrderMessage"] = "The order was removed from your list.";
        }
        else
        {
            TempData["CartMessage"] = "Only completed or cancelled orders can be removed.";
        }
        return RedirectToAction("MyOrders", new { status });
    }

    private async Task<List<CartLineVm>> LoadLines(string ids)
    {
        List<int> idList = null;
        if (!string.IsNullOrWhiteSpace(ids))
        {
            idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var v) ? v : 0)
                .Where(v => v > 0).ToList();
        }

        var items = await Db.CartItems.Include(c => c.Product).Include(c => c.Sku)
            .Where(c => c.CustomerId == CurrentCustomerId)
            .ToListAsync();

        if (idList != null && idList.Any()) items = items.Where(i => idList.Contains(i.CartItemId)).ToList();

        return items.Select(i => new CartLineVm
        {
            Item = i,
            Product = i.Product,
            Sku = i.Sku
        }).ToList();
    }
}
