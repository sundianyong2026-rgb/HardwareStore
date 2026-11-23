using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HardwareStore.Services;

namespace HardwareStore.Models.Entities;

/// <summary>Customer delivery address.</summary>
[Table("Address")]
public class Address
{
    [Key]
    [Column("addressID")]
    public int AddressId { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Required, MaxLength(100)]
    [Column("receiverName")]
    public string ReceiverName { get; set; }

    [Required, MaxLength(20)]
    [Column("phone")]
    public string Phone { get; set; }

    [Required, MaxLength(255)]
    [Column("addressDetail")]
    public string AddressDetail { get; set; }

    [Column("isDefault")]
    public bool IsDefault { get; set; } = false;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }
}

/// <summary>Cart line. One row per customer + product + SKU.</summary>
[Table("CartItem")]
public class CartItem
{
    [Key]
    [Column("cartItemID")]
    public int CartItemId { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("skuID")]
    public int SkuId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    [ForeignKey("SkuId")]
    public virtual ProductSku Sku { get; set; }
}

/// <summary>Order header. Snapshot of receiver and amounts is kept after the order is placed.</summary>
[Table("OrderInfo")]
public class OrderInfo
{
    [Key]
    [Column("orderID")]
    public int OrderId { get; set; }

    [Required, MaxLength(32)]
    [Column("orderNo")]
    public string OrderNo { get; set; }

    [Column("customerID")]
    public int CustomerId { get; set; }

    [Column("merchantID")]
    public int MerchantId { get; set; }

    [Required, MaxLength(100)]
    [Column("receiverName")]
    public string ReceiverName { get; set; }

    [Required, MaxLength(20)]
    [Column("phone")]
    public string Phone { get; set; }

    [Required, MaxLength(255)]
    [Column("addressDetail")]
    public string AddressDetail { get; set; }

    [Column("totalAmount", TypeName = "numeric(10,2)")]
    public decimal TotalAmount { get; set; }

    [Column("shippingFee", TypeName = "numeric(10,2)")]
    public decimal ShippingFee { get; set; } = 0;

    /// <summary>pending_payment | paid | delivered | received | completed | canceled</summary>
    [Required, MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending_payment";

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    [Column("paidAt")]
    public DateTime? PaidAt { get; set; }

    /// <summary>Set when the customer removes a finished or cancelled order from their own list.</summary>
    [Column("hiddenByCustomer")]
    public bool HiddenByCustomer { get; set; } = false;

    /// <summary>Set when the merchant removes a finished or cancelled order from the shop list.</summary>
    [Column("hiddenByMerchant")]
    public bool HiddenByMerchant { get; set; } = false;

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }

    [ForeignKey("MerchantId")]
    public virtual Merchant Merchant { get; set; }

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

/// <summary>Order line. Keeps a snapshot of the SKU specification and unit price.</summary>
[Table("OrderItem")]
public class OrderItem
{
    [Key]
    [Column("orderItemID")]
    public int OrderItemId { get; set; }

    [Column("orderID")]
    public int OrderId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("skuID")]
    public int SkuId { get; set; }

    [Required, MaxLength(100)]
    [Column("skuSpecSnapshot")]
    public string SkuSpecSnapshot { get; set; }

    [Column("unitPrice", TypeName = "numeric(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("subtotal", TypeName = "numeric(10,2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey("OrderId")]
    public virtual OrderInfo Order { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    [ForeignKey("SkuId")]
    public virtual ProductSku Sku { get; set; }
}
