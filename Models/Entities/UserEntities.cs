using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HardwareStore.Services;

namespace HardwareStore.Models.Entities;

/// <summary>Platform administrator (super-admin).</summary>
[Table("Admin")]
public class Admin
{
    [Key]
    [Column("adminID")]
    public int AdminId { get; set; }

    [Required, MaxLength(50)]
    [Column("username")]
    public string Username { get; set; }

    [Required, MaxLength(255)]
    [Column("passwordHash")]
    public string PasswordHash { get; set; }

    [MaxLength(100)]
    [Column("fullName")]
    public string FullName { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;
}

/// <summary>Registered customer.</summary>
[Table("Customer")]
public class Customer
{
    [Key]
    [Column("customerID")]
    public int CustomerId { get; set; }

    [Required, MaxLength(50)]
    [Column("username")]
    public string Username { get; set; }

    [Required, MaxLength(255)]
    [Column("passwordHash")]
    public string PasswordHash { get; set; }

    [MaxLength(100)]
    [Column("fullName")]
    public string FullName { get; set; }

    [MaxLength(20)]
    [Column("phone")]
    public string Phone { get; set; }

    [Column("registeredAt")]
    public DateTime RegisteredAt { get; set; } = AppTime.Now;

    [Column("isDisabled")]
    public bool IsDisabled { get; set; } = false;

    /// <summary>Set when the customer asks the administrator to close the account.</summary>
    [Column("deletionRequestedAt")]
    public DateTime? DeletionRequestedAt { get; set; }
}

/// <summary>Merchant store that applies for onboarding and manages its own shop.</summary>
[Table("Merchant")]
public class Merchant
{
    [Key]
    [Column("merchantID")]
    public int MerchantId { get; set; }

    [Required, MaxLength(100)]
    [Column("shopName")]
    public string ShopName { get; set; }

    [MaxLength(100)]
    [Column("contactName")]
    public string ContactName { get; set; }

    [MaxLength(20)]
    [Column("phone")]
    public string Phone { get; set; }

    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; }

    [Required, MaxLength(255)]
    [Column("passwordHash")]
    public string PasswordHash { get; set; }

    [Column("description", TypeName = "text")]
    public string Description { get; set; }

    [MaxLength(255)]
    [Column("logoUrl")]
    public string LogoUrl { get; set; }

    /// <summary>pending | approved | rejected</summary>
    [Required, MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("submittedAt")]
    public DateTime SubmittedAt { get; set; } = AppTime.Now;

    [Column("approvedAt")]
    public DateTime? ApprovedAt { get; set; }

    [Column("approvedBy")]
    public int? ApprovedBy { get; set; }

    /// <summary>Banned by the administrator: signing in is refused until it is lifted.</summary>
    [Column("isDisabled")]
    public bool IsDisabled { get; set; } = false;

    /// <summary>Set when the shop asks the administrator to close the account.</summary>
    [Column("deletionRequestedAt")]
    public DateTime? DeletionRequestedAt { get; set; }
}
