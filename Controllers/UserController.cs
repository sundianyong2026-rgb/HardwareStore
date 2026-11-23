using HardwareStore.Data;
using HardwareStore.Models.Entities;
using HardwareStore.Models.ViewModels;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

/// <summary>Customer personal centre: profile, addresses, favourites, browsing history, messages.</summary>
public class UserController : BaseController
{
    private readonly FileStorage _files;

    public UserController(StoreDbContext db, CurrentUserSession session, FileStorage files)
        : base(db, session)
    {
        _files = files;
    }

    // ---------------- Profile ----------------

    public async Task<IActionResult> Profile()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == CurrentCustomerId);
        if (customer == null) return RedirectToAction("Logout", "Account");

        // Cleared orders disappear from the order centre, so they must not be counted here either.
        ViewBag.OrderCount = await Db.Orders.CountAsync(o => o.CustomerId == CurrentCustomerId && !o.HiddenByCustomer);
        ViewBag.FavoriteCount = await Db.Favorites.CountAsync(f => f.CustomerId == CurrentCustomerId);
        ViewBag.HistoryCount = await Db.ViewLogs.CountAsync(v => v.CustomerId == CurrentCustomerId);
        return View(customer);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string fullName, string phone)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == CurrentCustomerId);
        if (customer != null)
        {
            customer.FullName = fullName;
            customer.Phone = phone;
            await Db.SaveChangesAsync();
            TempData["UserMessage"] = "Profile updated.";
        }
        return RedirectToAction("Profile");
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string newPassword)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["UserError"] = "The new password cannot be empty.";
            return RedirectToAction("Profile");
        }

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == CurrentCustomerId);
        if (customer != null)
        {
            customer.PasswordHash = PasswordHasher.Hash(newPassword);
            await Db.SaveChangesAsync();
            TempData["UserMessage"] = "Password changed.";
        }
        return RedirectToAction("Profile");
    }

    // ---------------- Address book ----------------

    public async Task<IActionResult> Addresses()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var list = await Db.Addresses.Where(a => a.CustomerId == CurrentCustomerId)
            .OrderByDescending(a => a.IsDefault).ToListAsync();
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> SaveAddress(int? addressId, string receiverName, string phone,
        string addressDetail, bool isDefault, string returnUrl)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        IActionResult Back()
            => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Addresses");

        if (string.IsNullOrWhiteSpace(receiverName) || string.IsNullOrWhiteSpace(addressDetail))
        {
            TempData["UserError"] = "Receiver and address are required.";
            return Back();
        }

        if (isDefault)
        {
            foreach (var a in Db.Addresses.Where(a => a.CustomerId == CurrentCustomerId && a.IsDefault))
                a.IsDefault = false;
        }

        if (addressId.HasValue && addressId.Value > 0)
        {
            var existing = await Db.Addresses.FirstOrDefaultAsync(
                a => a.AddressId == addressId.Value && a.CustomerId == CurrentCustomerId);
            if (existing != null)
            {
                existing.ReceiverName = receiverName;
                existing.Phone = phone;
                existing.AddressDetail = addressDetail;
                existing.IsDefault = isDefault;
            }
        }
        else
        {
            Db.Addresses.Add(new Address
            {
                CustomerId = CurrentCustomerId,
                ReceiverName = receiverName,
                Phone = phone,
                AddressDetail = addressDetail,
                IsDefault = isDefault
            });
        }

        await Db.SaveChangesAsync();
        TempData["UserMessage"] = "Address saved.";
        return Back();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAddress(int addressId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var existing = await Db.Addresses.FirstOrDefaultAsync(
            a => a.AddressId == addressId && a.CustomerId == CurrentCustomerId);
        if (existing != null)
        {
            Db.Addresses.Remove(existing);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Addresses");
    }

    [HttpPost]
    public async Task<IActionResult> SetDefaultAddress(int addressId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var all = await Db.Addresses.Where(a => a.CustomerId == CurrentCustomerId).ToListAsync();
        foreach (var a in all) a.IsDefault = a.AddressId == addressId;
        await Db.SaveChangesAsync();
        return RedirectToAction("Addresses");
    }

    // ---------------- Favourites ----------------

    public async Task<IActionResult> Favorites(int page = 1)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        const int pageSize = 12;
        var query = Db.Favorites.Include(f => f.Product).ThenInclude(p => p.Skus)
            .Where(f => f.CustomerId == CurrentCustomerId);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(f => f.CreatedAt)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Page = Math.Max(page, 1);
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFavorite(int productId, string returnUrl)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var existing = await Db.Favorites.FirstOrDefaultAsync(
            f => f.CustomerId == CurrentCustomerId && f.ProductId == productId);

        if (existing == null)
            Db.Favorites.Add(new Favorite { CustomerId = CurrentCustomerId, ProductId = productId });
        else
            Db.Favorites.Remove(existing);

        await Db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Detail", "Product", new { id = productId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveFavorite(int favoriteId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var existing = await Db.Favorites.FirstOrDefaultAsync(
            f => f.FavoriteId == favoriteId && f.CustomerId == CurrentCustomerId);
        if (existing != null)
        {
            Db.Favorites.Remove(existing);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("Favorites");
    }

    // ---------------- Browsing history ----------------

    public async Task<IActionResult> History()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var items = await Db.ViewLogs.Include(v => v.Product).ThenInclude(p => p.Skus)
            .Where(v => v.CustomerId == CurrentCustomerId)
            .OrderByDescending(v => v.ViewedAt).Take(100).ToListAsync();
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> RemoveHistory(int viewId)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var log = await Db.ViewLogs.FirstOrDefaultAsync(
            v => v.ViewId == viewId && v.CustomerId == CurrentCustomerId);
        if (log != null)
        {
            Db.ViewLogs.Remove(log);
            await Db.SaveChangesAsync();
        }
        return RedirectToAction("History");
    }

    [HttpPost]
    public async Task<IActionResult> ClearHistory()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        Db.ViewLogs.RemoveRange(Db.ViewLogs.Where(v => v.CustomerId == CurrentCustomerId));
        await Db.SaveChangesAsync();
        return RedirectToAction("History");
    }

    // ---------------- Messages (shared inbox) ----------------

    public IActionResult Messages() => RedirectToAction("Index", "Message");

    /// <summary>Kept for the old support popup: forwards the text to the shared inbox.</summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage(string content, string returnUrl)
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var text = (content ?? string.Empty).Trim();
        if (text.Length > 0)
        {
            Db.Messages.Add(new HardwareStore.Models.Entities.Message
            {
                SenderRole = HardwareStore.Models.RoleConstants.Customer,
                SenderId = CurrentCustomerId,
                SenderName = Session.UserName,
                ReceiverRole = HardwareStore.Models.RoleConstants.Admin,
                ReceiverId = 0,
                Body = text,
                CreatedAt = AppTime.Now
            });
            await Db.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Chat", "Message", new { peerRole = "admin", peerId = 0 });
    }

    // ---------------- Account closure request ----------------

    /// <summary>Orders that still have to be finished block the closure request.</summary>
    private IQueryable<OrderInfo> UnfinishedOrders()
        => Db.Orders.Where(o => o.CustomerId == CurrentCustomerId
                                && o.Status != "completed" && o.Status != "canceled");

    [HttpPost]
    public async Task<IActionResult> RequestDeletion()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var open = await UnfinishedOrders().CountAsync();
        if (open > 0)
        {
            TempData["UserError"] =
                $"You still have {open} unfinished order(s). Finish or cancel them before closing the account.";
            return RedirectToAction("Profile");
        }

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == CurrentCustomerId);
        if (customer == null) return RedirectToAction("Profile");

        customer.DeletionRequestedAt = AppTime.Now;
        customer.IsDisabled = true;
        await Db.SaveChangesAsync();

        Session.SignOut();
        TempData["UserMessage"] = "Your closure request was sent. The administrator will review it.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> CancelDeletion()
    {
        var gate = GateCustomer();
        if (gate != null) return gate;

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.CustomerId == CurrentCustomerId);
        if (customer != null && customer.DeletionRequestedAt != null)
        {
            customer.DeletionRequestedAt = null;
            customer.IsDisabled = false;
            await Db.SaveChangesAsync();
            TempData["UserMessage"] = "Your closure request was withdrawn.";
        }
        return RedirectToAction("Profile");
    }
}
