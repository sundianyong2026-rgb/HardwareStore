using HardwareStore.Data;
using HardwareStore.Models;
using HardwareStore.Models.Entities;
using HardwareStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Controllers;

/// <summary>
/// Platform messaging shared by the three roles. A conversation is always a pair
/// (customer to merchant, customer to support, merchant to support), and each side
/// can delete a single message or clear the whole conversation for itself only.
/// </summary>
public class MessageController : BaseController
{
    public MessageController(StoreDbContext db, CurrentUserSession session) : base(db, session) { }

    // ---------------- identity helpers ----------------

    private bool LoggedIn => Session.UserId.HasValue && !string.IsNullOrEmpty(Session.Role);

    private string MyRole => Session.Role ?? string.Empty;

    private int MyId => MyRole == RoleConstants.Admin ? 0 : (Session.UserId ?? 0);

    private string MyName => MyRole == RoleConstants.Admin ? "Platform support" : (Session.UserName ?? "Me");

    private static bool IsKnownRole(string role)
        => role == RoleConstants.Customer || role == RoleConstants.Merchant || role == RoleConstants.Admin;

    /// <summary>Who is allowed to talk to whom.</summary>
    private bool CanTalkTo(string role)
    {
        if (MyRole == RoleConstants.Customer) return role == RoleConstants.Merchant || role == RoleConstants.Admin;
        if (MyRole == RoleConstants.Merchant) return role == RoleConstants.Customer || role == RoleConstants.Admin;
        if (MyRole == RoleConstants.Admin) return role == RoleConstants.Customer || role == RoleConstants.Merchant;
        return false;
    }

    private async Task<string> PeerName(string role, int id)
    {
        switch (role)
        {
            case RoleConstants.Merchant:
                var shop = await Db.Merchants.Where(m => m.MerchantId == id).Select(m => m.ShopName).FirstOrDefaultAsync();
                return shop ?? "Shop";
            case RoleConstants.Customer:
                var name = await Db.Customers.Where(c => c.CustomerId == id).Select(c => c.Username).FirstOrDefaultAsync();
                return name ?? "Customer";
            default:
                return "Platform support";
        }
    }

    private IQueryable<Message> Between(string myRole, int myId, string peerRole, int peerId)
        => Db.Messages.Where(m =>
            (m.SenderRole == myRole && m.SenderId == myId
             && m.ReceiverRole == peerRole && m.ReceiverId == peerId) ||
            (m.SenderRole == peerRole && m.SenderId == peerId
             && m.ReceiverRole == myRole && m.ReceiverId == myId));

    private IQueryable<Message> VisibleToMe(string myRole, int myId, IQueryable<Message> source)
        => source.Where(m =>
            (m.SenderRole == myRole && m.SenderId == myId && !m.DeletedBySender) ||
            (m.ReceiverRole == myRole && m.ReceiverId == myId && !m.DeletedByReceiver));

    // ---------------- inbox ----------------

    public async Task<IActionResult> Index()
    {
        if (!LoggedIn) return RedirectToAction("Login", "Account");

        var role = MyRole;
        var id = MyId;

        var all = await VisibleToMe(role, id, Db.Messages).OrderByDescending(m => m.CreatedAt).ToListAsync();

        var threads = new Dictionary<string, (string role, int id, Message last, int count)>();

        foreach (var m in all)
        {
            var fromMe = m.SenderRole == role && m.SenderId == id;
            var peerRole = fromMe ? m.ReceiverRole : m.SenderRole;
            var peerId = fromMe ? m.ReceiverId : m.SenderId;
            var key = peerRole + "|" + peerId;

            if (!threads.ContainsKey(key))
                threads[key] = (peerRole, peerId, m, 1);
            else
            {
                var t = threads[key];
                threads[key] = (t.role, t.id, t.last, t.count + 1);
            }
        }

        var rows = new List<(string Role, int Id, string Name, Message Last, int Count)>();
        foreach (var t in threads.Values.OrderByDescending(x => x.last.CreatedAt))
            rows.Add((t.role, t.id, await PeerName(t.role, t.id), t.last, t.count));

        // Support is always reachable, even before the first message.
        var supportRole = role == RoleConstants.Admin ? string.Empty : RoleConstants.Admin;
        ViewBag.SupportRole = supportRole;
        ViewBag.Role = role;
        return View(rows);
    }

    // ---------------- one conversation ----------------

    public async Task<IActionResult> Chat(string peerRole, int peerId = 0, int? productId = null)
    {
        if (!LoggedIn) return RedirectToAction("Login", "Account");
        if (!IsKnownRole(peerRole) || !CanTalkTo(peerRole))
            return RedirectToAction("Index");

        if (peerRole == RoleConstants.Admin) peerId = 0;

        var list = await VisibleToMe(MyRole, MyId, Between(MyRole, MyId, peerRole, peerId))
            .OrderBy(m => m.CreatedAt).ToListAsync();

        ViewBag.PeerRole = peerRole;
        ViewBag.PeerId = peerId;
        ViewBag.PeerName = await PeerName(peerRole, peerId);
        ViewBag.MyRole = MyRole;
        ViewBag.Product = productId.HasValue
            ? await Db.Products.Where(p => p.ProductId == productId.Value)
                .Select(p => new { p.ProductId, p.ProductName }).FirstOrDefaultAsync()
            : null;
        ViewBag.Conversation = await Db.Messages.CountAsync(m =>
            (m.SenderRole == MyRole && m.SenderId == MyId && m.ReceiverRole == peerRole && m.ReceiverId == peerId)
            || (m.SenderRole == peerRole && m.SenderId == peerId && m.ReceiverRole == MyRole && m.ReceiverId == MyId));

        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> Send(string peerRole, int peerId, string body, int? productId)
    {
        if (!LoggedIn) return RedirectToAction("Login", "Account");
        if (!IsKnownRole(peerRole) || !CanTalkTo(peerRole)) return RedirectToAction("Index");

        if (peerRole == RoleConstants.Admin) peerId = 0;

        var text = (body ?? string.Empty).Trim();
        if (text.Length == 0) return RedirectToAction("Chat", new { peerRole, peerId, productId });

        string productName = null;
        int? pid = null;
        if (productId.HasValue)
        {
            var product = await Db.Products.Where(p => p.ProductId == productId.Value)
                .Select(p => new { p.ProductId, p.ProductName }).FirstOrDefaultAsync();
            if (product != null)
            {
                pid = product.ProductId;
                productName = product.ProductName;
            }
        }

        Db.Messages.Add(new Message
        {
            SenderRole = MyRole,
            SenderId = MyId,
            SenderName = MyName,
            ReceiverRole = peerRole,
            ReceiverId = peerId,
            ProductId = pid,
            ProductName = productName,
            Body = text,
            CreatedAt = AppTime.Now
        });
        await Db.SaveChangesAsync();

        return RedirectToAction("Chat", new { peerRole, peerId, productId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int messageId, string peerRole, int peerId)
    {
        if (!LoggedIn) return RedirectToAction("Login", "Account");

        var message = await Db.Messages.FirstOrDefaultAsync(m => m.MessageId == messageId);
        if (message != null)
        {
            if (message.SenderRole == MyRole && message.SenderId == MyId) message.DeletedBySender = true;
            if (message.ReceiverRole == MyRole && message.ReceiverId == MyId) message.DeletedByReceiver = true;
            await Db.SaveChangesAsync();
        }

        return RedirectToAction("Chat", new { peerRole, peerId });
    }

    /// <summary>Clears this conversation for the current user only; the other side keeps its copy.</summary>
    [HttpPost]
    public async Task<IActionResult> Clear(string peerRole, int peerId)
    {
        if (!LoggedIn) return RedirectToAction("Login", "Account");

        var messages = await Between(MyRole, MyId, peerRole, peerId == 0 ? 0 : peerId).ToListAsync();

        foreach (var m in messages)
        {
            if (m.SenderRole == MyRole && m.SenderId == MyId) m.DeletedBySender = true;
            if (m.ReceiverRole == MyRole && m.ReceiverId == MyId) m.DeletedByReceiver = true;
        }

        await Db.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
