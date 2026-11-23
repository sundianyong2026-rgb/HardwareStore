using HardwareStore.Data;
using HardwareStore.Models;
using HardwareStore.Models.Entities;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

public class AccountController : BaseController
{
    public AccountController(StoreDbContext db, CurrentUserSession session) : base(db, session) { }

    // ---------------- Unified registration (customer / merchant) ----------------

    [HttpGet]
    public IActionResult Register(string type = null)
    {
        ViewBag.AccountType = type == "merchant" ? "merchant" : "customer";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(string accountType, string username, string password,
        string phone, string address, string shopName, string contactName, string merchantPhone,
        string email, string merchantPassword, string description)
    {
        // Merchant onboarding branch: submitted as pending and reviewed by the administrator.
        if (accountType == "merchant")
        {
            ViewBag.AccountType = "merchant";

            if (string.IsNullOrWhiteSpace(shopName) || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(merchantPassword))
            {
                ViewBag.Error = "Shop name, e-mail and password are required.";
                return View();
            }

            if (await Db.Merchants.AnyAsync(m => m.Email == email.Trim()))
            {
                ViewBag.Error = "This e-mail has already been used for an application.";
                return View();
            }

            Db.Merchants.Add(new Merchant
            {
                ShopName = shopName.Trim(),
                ContactName = contactName,
                Phone = merchantPhone,
                Email = email.Trim(),
                PasswordHash = PasswordHasher.Hash(merchantPassword),
                Description = description,
                Status = "pending"
            });
            await Db.SaveChangesAsync();

            ViewBag.Success = "Your shop application has been submitted. The administrator will review it before you can sign in.";
            return View();
        }

        // Customer branch.
        ViewBag.AccountType = "customer";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Username and password are required.";
            return View();
        }

        if (await Db.Customers.AnyAsync(c => c.Username == username.Trim()))
        {
            ViewBag.Error = "This username is already taken.";
            return View();
        }

        var customer = new Customer
        {
            Username = username.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            Phone = phone
        };
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();

        // The optional address entered at registration becomes the default shipping address.
        if (!string.IsNullOrWhiteSpace(address))
        {
            Db.Addresses.Add(new Address
            {
                CustomerId = customer.CustomerId,
                ReceiverName = customer.Username,
                Phone = phone ?? string.Empty,
                AddressDetail = address.Trim(),
                IsDefault = true
            });
            await Db.SaveChangesAsync();
        }

        Session.SignIn(customer.CustomerId, RoleConstants.Customer, customer.Username);
        return RedirectToAction("Index", "Home");
    }

    // ---------------- Customer login ----------------

    [HttpGet]
    public IActionResult Login(string role = "customer")
    {
        ViewBag.Role = role;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string role, string username, string password)
    {
        ViewBag.Role = role;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Username and password are required.";
            return View();
        }

        if (role == "merchant")
        {
            // Merchants may sign in with either their e-mail address or their phone number.
            var key = username.Trim();
            var merchant = await Db.Merchants.FirstOrDefaultAsync(m => m.Email == key || m.Phone == key);
            if (merchant == null || !PasswordHasher.Verify(password, merchant.PasswordHash))
            {
                ViewBag.Error = "Invalid merchant credentials.";
                return View();
            }
            if (merchant.Status != "approved")
            {
                ViewBag.Error = "Your shop application is " + merchant.Status + ". Please wait for the administrator.";
                return View();
            }
            if (merchant.IsDisabled && merchant.DeletionRequestedAt == null)
            {
                ViewBag.Error = "This shop account has been disabled by the administrator.";
                return View();
            }
            Session.SignIn(merchant.MerchantId, RoleConstants.Merchant, merchant.ShopName);
            if (merchant.DeletionRequestedAt != null)
                TempData["MerchantMessage"] = "Your closure request is still waiting for the administrator. You can cancel it below.";
            return RedirectToAction("Index", "Merchant");
        }

        if (role == "admin")
        {
            var admin = await Db.Admins.FirstOrDefaultAsync(a => a.Username == username.Trim());
            if (admin == null || !PasswordHasher.Verify(password, admin.PasswordHash))
            {
                ViewBag.Error = "Invalid administrator credentials.";
                return View();
            }
            Session.SignIn(admin.AdminId, RoleConstants.Admin, admin.Username);
            return RedirectToAction("Index", "Admin");
        }

        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.Username == username.Trim());
        if (customer == null || !PasswordHasher.Verify(password, customer.PasswordHash))
        {
            ViewBag.Error = "Invalid username or password.";
            return View();
        }
        if (customer.IsDisabled && customer.DeletionRequestedAt == null)
        {
            ViewBag.Error = "This account has been disabled by the administrator.";
            return View();
        }

        Session.SignIn(customer.CustomerId, RoleConstants.Customer, customer.Username);
        if (customer.DeletionRequestedAt != null)
            TempData["UserMessage"] = "Your closure request is still waiting for the administrator. You can cancel it from your profile.";
        return RedirectToAction("Index", "Home");
    }

    // ---------------- Legacy merchant onboarding entry ----------------

    // The old standalone application page is merged into the unified Register page.
    // Keep this route only as a redirect so old bookmarks still work.
    [HttpGet]
    public IActionResult Apply() => RedirectToAction("Register", new { type = "merchant" });

    [HttpPost]
    public IActionResult Apply(string shopName, string contactName, string phone,
        string email, string password, string description)
        => RedirectToAction("Register", new { type = "merchant" });

    public IActionResult Logout()
    {
        Session.SignOut();
        return RedirectToAction("Index", "Home");
    }
}
