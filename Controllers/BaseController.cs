using HardwareStore.Data;
using HardwareStore.Models;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace HardwareStore.Controllers;

/// <summary>
/// Shared base controller: database context, session helper and role guards.
/// Merchant data isolation is enforced by always filtering on <see cref="CurrentMerchantId"/>.
/// </summary>
public abstract class BaseController : Controller
{
    protected readonly StoreDbContext Db;
    protected readonly CurrentUserSession Session;

    protected BaseController(StoreDbContext db, CurrentUserSession session)
    {
        Db = db;
        Session = session;
    }

    protected bool IsCustomer => Session.IsInRole(RoleConstants.Customer);
    protected bool IsMerchant => Session.IsInRole(RoleConstants.Merchant);
    protected bool IsAdmin => Session.IsInRole(RoleConstants.Admin);

    protected int CurrentCustomerId => IsCustomer ? (Session.UserId ?? 0) : 0;
    protected int CurrentMerchantId => IsMerchant ? (Session.UserId ?? 0) : 0;

    /// <summary>Returns a redirect result when the visitor is not a customer, otherwise null.</summary>
    protected IActionResult GateCustomer()
        => IsCustomer ? null : RedirectToAction("Login", "Account", new { role = "customer" });

    protected IActionResult GateMerchant()
        => IsMerchant ? null : RedirectToAction("Login", "Account", new { role = "merchant" });

    protected IActionResult GateAdmin()
        => IsAdmin ? null : RedirectToAction("Login", "Account", new { role = "admin" });
}
