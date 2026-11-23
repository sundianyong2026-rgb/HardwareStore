using HardwareStore.Models.Entities;

namespace HardwareStore.Models.ViewModels;

public class CategoryNode
{
    public Category Category { get; set; }
    public List<Category> Children { get; set; } = new();
}

public class HomeRow
{
    public string Title { get; set; }
    public int CategoryId { get; set; }
    public List<ProductCardVm> Products { get; set; } = new();
}

public class HomeViewModel
{
    public List<Banner> Banners { get; set; } = new();
    public List<Notice> Notices { get; set; } = new();
    public List<CategoryNode> Categories { get; set; } = new();
    public List<HomeRow> Rows { get; set; } = new();
}

public class ProductCardVm
{
    public Product Product { get; set; }
    public decimal MinPrice { get; set; }
    public int TotalStock { get; set; }
    public double AvgRating { get; set; }
    public int ReviewCount { get; set; }
}

public class ProductListViewModel
{
    public List<ProductCardVm> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int Total { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public string Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating { get; set; }
    public string Sort { get; set; }
    public string Title { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
}

public class ProductDetailViewModel
{
    public Product Product { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public double AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public int? RatingFilter { get; set; }
    public bool IsFavorite { get; set; }
    public string Message { get; set; }
}

public class CartLineVm
{
    public CartItem Item { get; set; }
    public Product Product { get; set; }
    public ProductSku Sku { get; set; }
    public decimal Subtotal => Sku == null ? 0 : Sku.Price * Item.Quantity;
    public bool StockEnough => Sku != null && Sku.Stock >= Item.Quantity;
}

public class CartViewModel
{
    public List<CartLineVm> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Subtotal);
    public int Count => Lines.Sum(l => l.Item.Quantity);
}

public class CheckoutViewModel
{
    public List<CartLineVm> Lines { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
    public int? AddressId { get; set; }
    public decimal GoodsTotal => Lines.Sum(l => l.Subtotal);
    public decimal ShippingFee => GoodsTotal >= 99m ? 0m : 10m;
    public decimal PayTotal => GoodsTotal + ShippingFee;
    public string SelectedIds { get; set; }
}

public class MyOrdersViewModel
{
    public List<OrderInfo> Orders { get; set; } = new();
    public string Status { get; set; }
}

public class MerchantDashboardVm
{
    public int ProductCount { get; set; }
    public int SkuCount { get; set; }
    public int OrderCount { get; set; }
    public decimal SalesAmount { get; set; }
    public int PendingMessages { get; set; }
    public List<OrderInfo> RecentOrders { get; set; } = new();
}

public class AdminDashboardVm
{
    public int CustomerCount { get; set; }
    public int MerchantCount { get; set; }
    public int PendingMerchantCount { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
    public decimal SalesAmount { get; set; }
    public int PendingReviewCount { get; set; }
    public List<string> SalesLabels { get; set; } = new();
    public List<decimal> SalesValues { get; set; } = new();
    public List<string> VisitLabels { get; set; } = new();
    public List<int> VisitValues { get; set; } = new();
}

public class StatsViewModel
{
    public string From { get; set; }
    public string To { get; set; }
    public decimal SalesAmount { get; set; }
    public int OrderCount { get; set; }
    public int VisitCount { get; set; }
    public List<string> Labels { get; set; } = new();
    public List<decimal> SalesValues { get; set; } = new();
    public List<int> VisitValues { get; set; } = new();
    public List<ProductCardVm> TopProducts { get; set; } = new();
}
