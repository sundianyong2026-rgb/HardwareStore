using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HardwareStore.Services;

namespace HardwareStore.Models.Entities;

/// <summary>Product category, supports one parent category (self-reference).</summary>
[Table("Category")]
public class Category
{
    [Key]
    [Column("categoryID")]
    public int CategoryId { get; set; }

    [Column("parentID")]
    public int? ParentId { get; set; }

    [Required, MaxLength(100)]
    [Column("name")]
    public string Name { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;

    [ForeignKey("ParentId")]
    public virtual Category Parent { get; set; }
}

/// <summary>Product brand with its own logo.</summary>
[Table("Brand")]
public class Brand
{
    [Key]
    [Column("brandID")]
    public int BrandId { get; set; }

    [Required, MaxLength(100)]
    [Column("name")]
    public string Name { get; set; }

    [MaxLength(255)]
    [Column("logoUrl")]
    public string LogoUrl { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>Product listing owned by one merchant. Stock lives only on the SKU level.</summary>
[Table("Product")]
public class Product
{
    [Key]
    [Column("productID")]
    public int ProductId { get; set; }

    [Column("merchantID")]
    public int MerchantId { get; set; }

    [Column("categoryID")]
    public int CategoryId { get; set; }

    [Column("brandID")]
    public int? BrandId { get; set; }

    [Required, MaxLength(150)]
    [Column("productName")]
    public string ProductName { get; set; }

    [Column("description", TypeName = "text")]
    public string Description { get; set; }

    [MaxLength(255)]
    [Column("mainImage")]
    public string MainImage { get; set; }

    /// <summary>on | off</summary>
    [Required, MaxLength(10)]
    [Column("status")]
    public string Status { get; set; } = "on";

    [Column("salesCount")]
    public int SalesCount { get; set; } = 0;

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = AppTime.Now;

    /// <summary>Position of this product inside its own category, maintained by drag and drop.</summary>
    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [ForeignKey("MerchantId")]
    public virtual Merchant Merchant { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; }

    [ForeignKey("BrandId")]
    public virtual Brand Brand { get; set; }

    public virtual ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();

    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
}

/// <summary>Concrete SKU: one combination of specifications with its own price and stock.</summary>
[Table("ProductSku")]
public class ProductSku
{
    [Key]
    [Column("skuID")]
    public int SkuId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Required, MaxLength(100)]
    [Column("skuSpec")]
    public string SkuSpec { get; set; }

    [Column("price", TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [Column("stock")]
    public int Stock { get; set; } = 0;

    [MaxLength(255)]
    [Column("imageUrl")]
    public string ImageUrl { get; set; }

    /// <summary>
    /// Comma separated attribute value ids that build this combination, ordered like
    /// the attributes of the product (e.g. "3,7" = Color=Black + Size=512GB).
    /// </summary>
    [MaxLength(255)]
    [Column("attributeValueIds")]
    public string AttributeValueIds { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}

/// <summary>
/// One attribute dimension of a product, e.g. Color or Size. A product can have any
/// number of dimensions, so specifications are not limited to a single field.
/// </summary>
[Table("ProductAttribute")]
public class ProductAttribute
{
    [Key]
    [Column("attributeID")]
    public int AttributeId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Required, MaxLength(50)]
    [Column("name")]
    public string Name { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    public virtual ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}

/// <summary>One selectable value of an attribute, e.g. Black, Blue or 512GB.</summary>
[Table("ProductAttributeValue")]
public class ProductAttributeValue
{
    [Key]
    [Column("valueID")]
    public int ValueId { get; set; }

    [Column("attributeID")]
    public int AttributeId { get; set; }

    [Required, MaxLength(100)]
    [Column("value")]
    public string Value { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [ForeignKey("AttributeId")]
    public virtual ProductAttribute Attribute { get; set; }
}

/// <summary>Additional gallery image of a product.</summary>
[Table("ProductImage")]
public class ProductImage
{
    [Key]
    [Column("imageID")]
    public int ImageId { get; set; }

    [Column("productID")]
    public int ProductId { get; set; }

    [Required, MaxLength(255)]
    [Column("imageUrl")]
    public string ImageUrl { get; set; }

    [Column("sortOrder")]
    public int SortOrder { get; set; } = 0;

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }
}
