using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HardwareStore.Services;

namespace HardwareStore.Models.Entities;

/// <summary>Product review tied to the purchased order item.</summary>
[Table("Review")]
public class Review
{
    [Key]
    [Column("reviewID")]
    public int ReviewId { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("orderItemID")]
    public int? OrderItemId { get; set; }

    [Column("rating")]
    public short Rating { get; set; }

    [Column("content", TypeName = "text")]
    public string Content { get; set; }

    [MaxLength(255)]
    [Column("imageUrl")]
    public string ImageUrl { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    [Column("isVisible")]
    public bool IsVisible { get; set; } = true;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    [ForeignKey("OrderItemId")]
    public virtual OrderItem OrderItem { get; set; }
}

/// <summary>Customer favourite product.</summary>
[Table("Favorite")]
public class Favorite
{
    [Key]
    [Column("favoriteID")]
    public int FavoriteId { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}

/// <summary>Browsing history record.</summary>
[Table("ViewLog")]
public class ViewLog
{
    [Key]
    [Column("viewID")]
    public int ViewId { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("viewedAt")]
    public DateTime ViewedAt { get; set; } = AppTime.Now;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}

/// <summary>Customer support message shown in the popup contact box.</summary>
[Table("ContactMessage")]
public class ContactMessage
{
    [Key]
    [Column("messageID")]
    public int MessageId { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Required, Column("content", TypeName = "text")]
    public string Content { get; set; }

    /// <summary>new | answered</summary>
    [Required, MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "new";

    [Column("reply", TypeName = "text")]
    public string Reply { get; set; }

    [Column("repliedAt")]
    public DateTime? RepliedAt { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }
}

/// <summary>
/// Platform message. Every conversation is a pair of roles (customer / merchant / admin),
/// so the same table serves customer to merchant, customer to support and merchant to support chats.
/// Each side can delete a single message or clear the whole conversation independently.
/// </summary>
[Table("Message")]
public class Message
{
    [Key]
    [Column("messageID")]
    public int MessageId { get; set; }

    /// <summary>customer | merchant | admin</summary>
    [Required, MaxLength(20)]
    [Column("senderRole")]
    public string SenderRole { get; set; }

    [Column("senderID")]
    public int SenderId { get; set; }

    [MaxLength(100)]
    [Column("senderName")]
    public string SenderName { get; set; }

    /// <summary>customer | merchant | admin (admin always uses receiverID = 0)</summary>
    [Required, MaxLength(20)]
    [Column("receiverRole")]
    public string ReceiverRole { get; set; }

    [Column("receiverID")]
    public int ReceiverId { get; set; }

    /// <summary>Product the message is about, when the chat started from a product page.</summary>
    [Column("productID")]
    public int? ProductId { get; set; }

    [MaxLength(150)]
    [Column("productName")]
    public string ProductName { get; set; }

    [Required, Column("body", TypeName = "text")]
    public string Body { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    [Column("deletedBySender")]
    public bool DeletedBySender { get; set; } = false;

    [Column("deletedByReceiver")]
    public bool DeletedByReceiver { get; set; } = false;
}
