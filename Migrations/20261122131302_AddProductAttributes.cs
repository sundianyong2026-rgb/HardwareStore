using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HardwareStore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attributeValueIds",
                table: "ProductSku",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductAttribute",
                columns: table => new
                {
                    attributeID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    productID = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttribute", x => x.attributeID);
                    table.ForeignKey(
                        name: "FK_ProductAttribute_Product_productID",
                        column: x => x.productID,
                        principalTable: "Product",
                        principalColumn: "productID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValue",
                columns: table => new
                {
                    valueID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attributeID = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValue", x => x.valueID);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValue_ProductAttribute_attributeID",
                        column: x => x.attributeID,
                        principalTable: "ProductAttribute",
                        principalColumn: "attributeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttribute_productID",
                table: "ProductAttribute",
                column: "productID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValue_attributeID",
                table: "ProductAttributeValue",
                column: "attributeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductAttributeValue");

            migrationBuilder.DropTable(
                name: "ProductAttribute");

            migrationBuilder.DropColumn(
                name: "attributeValueIds",
                table: "ProductSku");
        }
    }
}
