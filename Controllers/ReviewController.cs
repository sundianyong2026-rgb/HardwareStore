using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

public class ReviewController : BaseController
{
    private readonly FileStorage _files;

    public ReviewController(StoreDbContext db, CurrentUserSession session, FileStorage files)
        : base(db, session)
    {
        _files = files;
    }

    /// <summary>
    /// Only a purchased order item of a completed order may be reviewed.
    /// </summary>
    private async Task<OrderItem> LoadEligibleItem(int orderItemId)
    {
        return await Db.OrderItems
            .Include(i => i.Order)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.OrderItemId == orderItemId
                                      && i.Order.CustomerId == CurrentCustomerId
                                      && i.Order.Status == "completed");
    }

    [HttpGet]
    public async Task<IActionResult> Create(int orderItemId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var item = await LoadEligibleItem(orderItemId);
        if (item == null)
        {
            TempData["OrderMessage"] = "Only completed orders can be reviewed.";
            return RedirectToAction("MyOrders", "Order");
        }

        var existing = await Db.Reviews.FirstOrDefaultAsync(r => r.OrderItemId == orderItemId);
        if (existing != null)
        {
            TempData["OrderMessage"] = "This item has already been reviewed.";
            return RedirectToAction("MyOrders", "Order");
        }

        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int orderItemId, int rating, string content, string croppedImage)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var item = await LoadEligibleItem(orderItemId);
        if (item == null)
        {
            TempData["OrderMessage"] = "Only completed orders can be reviewed.";
            return RedirectToAction("MyOrders", "Order");
        }

        if (rating < 1 || rating > 5) rating = 5;

        var imageUrl = _files.SaveCroppedDataUrl(croppedImage, "reviews");

        Db.Reviews.Add(new Review
        {
            CustomerId = CurrentCustomerId,
            ProductId = item.ProductId,
            OrderItemId = item.OrderItemId,
            Rating = (short)rating,
            Content = content,
            ImageUrl = imageUrl,
            CreatedAt = AppTime.Now,
            IsVisible = true
        });
        await Db.SaveChangesAsync();

        TempData["OrderMessage"] = "Thank you, your review has been published.";
        return RedirectToAction("Detail", "Product", new { id = item.ProductId });
    }
}
