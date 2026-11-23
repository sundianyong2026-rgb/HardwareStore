using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HardwareStore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    adminID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    passwordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    fullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admin", x => x.adminID);
                });

            migrationBuilder.CreateTable(
                name: "Banner",
                columns: table => new
                {
                    bannerID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    imageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    linkUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sortOrder = table.Column<int>(type: "integer", nullable: false),
                    isActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banner", x => x.bannerID);
                });

            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    brandID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    logoUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sortOrder = table.Column<int>(type: "integer", nullable: false),
                    isActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.brandID);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    categoryID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parentID = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sortOrder = table.Column<int>(type: "integer", nullable: false),
                    isActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.categoryID);
                    table.ForeignKey(
                        name: "FK_Category_Category_parentID",
                        column: x => x.parentID,
                        principalTable: "Category",
                        principalColumn: "categoryID");
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    customerID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    passwordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    fullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    registeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isDisabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.customerID);
                });

            migrationBuilder.CreateTable(
                name: "Merchant",
                columns: table => new
                {
                    merchantID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shopName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    contactName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    passwordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    logoUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approvedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merchant", x => x.merchantID);
                    table.CheckConstraint("ck_merchant_status", "status in ('pending','approved','rejected')");
                });

            migrationBuilder.CreateTable(
                name: "Notice",
                columns: table => new
                {
                    noticeID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notice", x => x.noticeID);
                });

            migrationBuilder.CreateTable(
                name: "PriceChangeLog",
                columns: table => new
                {
                    changeID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    skuID = table.Column<int>(type: "integer", nullable: false),
                    oldPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    newPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    changedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceChangeLog", x => x.changeID);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    addressID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    receiverName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    addressDetail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    isDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.addressID);
                    table.ForeignKey(
                        name: "FK_Address_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactMessage",
                columns: table => new
                {
                    messageID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reply = table.Column<string>(type: "text", nullable: true),
                    repliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessage", x => x.messageID);
                    table.CheckConstraint("ck_message_status", "status in ('new','answered')");
                    table.ForeignKey(
                        name: "FK_ContactMessage_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderInfo",
                columns: table => new
                {
                    orderID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    merchantID = table.Column<int>(type: "integer", nullable: false),
                    receiverName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    addressDetail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    totalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    shippingFee = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    paidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInfo", x => x.orderID);
                    table.CheckConstraint("ck_order_status", "status in ('pending_payment','paid','delivered','received','completed','canceled')");
                    table.ForeignKey(
                        name: "FK_OrderInfo_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderInfo_Merchant_merchantID",
                        column: x => x.merchantID,
                        principalTable: "Merchant",
                        principalColumn: "merchantID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    productID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    merchantID = table.Column<int>(type: "integer", nullable: false),
                    categoryID = table.Column<int>(type: "integer", nullable: false),
                    brandID = table.Column<int>(type: "integer", nullable: true),
                    productName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    mainImage = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    salesCount = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.productID);
                    table.CheckConstraint("ck_product_status", "status in ('on','off')");
                    table.ForeignKey(
                        name: "FK_Product_Brand_brandID",
                        column: x => x.brandID,
                        principalTable: "Brand",
                        principalColumn: "brandID");
                    table.ForeignKey(
                        name: "FK_Product_Category_categoryID",
                        column: x => x.categoryID,
                        principalTable: "Category",
                        principalColumn: "categoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_Merchant_merchantID",
                        column: x => x.merchantID,
                        principalTable: "Merchant",
                        principalColumn: "merchantID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyVisit",
                columns: table => new
                {
                    statDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    visits = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyVisit", x => new { x.statDate, x.productID });
                    table.ForeignKey(
                        name: "FK_DailyVisit_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Favorite",
                columns: table => new
                {
                    favoriteID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorite", x => x.favoriteID);
                    table.ForeignKey(
                        name: "FK_Favorite_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Favorite_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImage",
                columns: table => new
                {
                    imageID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    imageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImage", x => x.imageID);
                    table.ForeignKey(
                        name: "FK_ProductImage_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSku",
                columns: table => new
                {
                    skuID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    skuSpec = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    stock = table.Column<int>(type: "integer", nullable: false),
                    imageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSku", x => x.skuID);
                    table.CheckConstraint("ck_sku_stock", "stock >= 0");
                    table.ForeignKey(
                        name: "FK_ProductSku_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewLog",
                columns: table => new
                {
                    viewID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    viewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewLog", x => x.viewID);
                    table.ForeignKey(
                        name: "FK_ViewLog_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ViewLog_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItem",
                columns: table => new
                {
                    cartItemID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    skuID = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItem", x => x.cartItemID);
                    table.CheckConstraint("ck_cart_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_CartItem_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItem_ProductSku_skuID",
                        column: x => x.skuID,
                        principalTable: "ProductSku",
                        principalColumn: "skuID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItem_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    orderItemID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orderID = table.Column<int>(type: "integer", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    skuID = table.Column<int>(type: "integer", nullable: false),
                    skuSpecSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.orderItemID);
                    table.ForeignKey(
                        name: "FK_OrderItem_OrderInfo_orderID",
                        column: x => x.orderID,
                        principalTable: "OrderInfo",
                        principalColumn: "orderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_ProductSku_skuID",
                        column: x => x.skuID,
                        principalTable: "ProductSku",
                        principalColumn: "skuID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Review",
                columns: table => new
                {
                    reviewID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customerID = table.Column<int>(type: "integer", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    orderItemID = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<short>(type: "smallint", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    imageUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isVisible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Review", x => x.reviewID);
                    table.CheckConstraint("ck_review_rating", "rating between 1 and 5");
                    table.ForeignKey(
                        name: "FK_Review_Customer_customerID",
                        column: x => x.customerID,
                        principalTable: "Customer",
                        principalColumn: "customerID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Review_OrderItem_orderItemID",
                        column: x => x.orderItemID,
                        principalTable: "OrderItem",
                        principalColumn: "orderItemID");
                    table.ForeignKey(
                        name: "FK_Review_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Address_customerID",
                table: "Address",
                column: "customerID");

            migrationBuilder.CreateIndex(
                name: "IX_Admin_username",
                table: "Admin",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brand_name",
                table: "Brand",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_customerID_productID_skuID",
                table: "CartItem",
                columns: new[] { "customerID", "productID", "skuID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_productID",
                table: "CartItem",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_skuID",
                table: "CartItem",
                column: "skuID");

            migrationBuilder.CreateIndex(
                name: "IX_Category_parentID",
                table: "Category",
                column: "parentID");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessage_customerID",
                table: "ContactMessage",
                column: "customerID");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_username",
                table: "Customer",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyVisit_productID",
                table: "DailyVisit",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_Favorite_customerID_productID",
                table: "Favorite",
                columns: new[] { "customerID", "productID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorite_productID",
                table: "Favorite",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_Merchant_email",
                table: "Merchant",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInfo_customerID",
                table: "OrderInfo",
                column: "customerID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInfo_merchantID",
                table: "OrderInfo",
                column: "merchantID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInfo_orderNo",
                table: "OrderInfo",
                column: "orderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_orderID",
                table: "OrderItem",
                column: "orderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_productID",
                table: "OrderItem",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_skuID",
                table: "OrderItem",
                column: "skuID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_brandID",
                table: "Product",
                column: "brandID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_categoryID",
                table: "Product",
                column: "categoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_merchantID",
                table: "Product",
                column: "merchantID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_productID",
                table: "ProductImage",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSku_productID",
                table: "ProductSku",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_customerID",
                table: "Review",
                column: "customerID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_orderItemID",
                table: "Review",
                column: "orderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Review_productID",
                table: "Review",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_ViewLog_customerID",
                table: "ViewLog",
                column: "customerID");

            migrationBuilder.CreateIndex(
                name: "IX_ViewLog_productID",
                table: "ViewLog",
                column: "productID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "Banner");

            migrationBuilder.DropTable(
                name: "CartItem");

            migrationBuilder.DropTable(
                name: "ContactMessage");

            migrationBuilder.DropTable(
                name: "DailyVisit");

            migrationBuilder.DropTable(
                name: "Favorite");

            migrationBuilder.DropTable(
                name: "Notice");

            migrationBuilder.DropTable(
                name: "PriceChangeLog");

            migrationBuilder.DropTable(
                name: "ProductImage");

            migrationBuilder.DropTable(
                name: "Review");

            migrationBuilder.DropTable(
                name: "ViewLog");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "OrderInfo");

            migrationBuilder.DropTable(
                name: "ProductSku");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Merchant");
        }
    }
}
