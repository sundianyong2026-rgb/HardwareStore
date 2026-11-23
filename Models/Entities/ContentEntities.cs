using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HardwareStore.Services;

namespace HardwareStore.Models.Entities;

/// <summary>Home page rotating banner uploaded by the administrator.</summary>
[Table("Banner")]
public class Banner
{
    [Key]
    [Column("bannerID")]
    public int BannerId { get; set; }

    [MaxLength(150)]
    [Column("title")]
    public string Title { get; set; }

    [Required, MaxLength(255)]
    [Column("imageUrl")]
    public string ImageUrl { get; set; }

    [MaxLength(255)]
    [Column("linkUrl")]
    public string LinkUrl { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Shop notice editable from the administration backend.</summary>
[Table("Notice")]
public class Notice
{
    [Key]
    [Column("noticeID")]
    public int NoticeId { get; set; }

    [Required, MaxLength(150)]
    [Column("title")]
    public string Title { get; set; }

    [Required, Column("content", TypeName = "text")]
    public string Content { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Per-day visit count aggregated per product.</summary>
[Table("DailyVisit")]
public class DailyVisit
{
    [Column("statDate")]
    public DateTime StatDate { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("visits")]
    public int Visits { get; set; } = 0;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}

/// <summary>Audit log recording every SKU price change.</summary>
[Table("PriceChangeLog")]
public class PriceChangeLog
{
    [Key]
    [Column("changeID")]
    public int ChangeId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Column("skuID")]
    public int SkuId { get; set; }

    [Column("oldPrice", TypeName = "numeric(10,2)")]
    public decimal OldPrice { get; set; }

    [Column("newPrice", TypeName = "numeric(10,2)")]
    public decimal NewPrice { get; set; }

    [Column("changedAt")]
    public DateTime ChangedAt { get; set; } = AppTime.Now;
}
