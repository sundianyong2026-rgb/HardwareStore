using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareStore.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderHiddenFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "IF NOT EXISTS" keeps this migration safe on databases where the two
            // columns were already created by hand.
            migrationBuilder.Sql(
                "ALTER TABLE \"OrderInfo\" ADD COLUMN IF NOT EXISTS \"hiddenByCustomer\" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql(
                "ALTER TABLE \"OrderInfo\" ADD COLUMN IF NOT EXISTS \"hiddenByMerchant\" boolean NOT NULL DEFAULT false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"OrderInfo\" DROP COLUMN IF EXISTS \"hiddenByCustomer\";");
            migrationBuilder.Sql("ALTER TABLE \"OrderInfo\" DROP COLUMN IF EXISTS \"hiddenByMerchant\";");
        }
    }
}
