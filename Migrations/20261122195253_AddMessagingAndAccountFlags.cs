using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HardwareStore.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingAndAccountFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sortOrder",
                table: "Product",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "deletionRequestedAt",
                table: "Merchant",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isDisabled",
                table: "Merchant",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deletionRequestedAt",
                table: "Customer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    messageID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    senderRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    senderID = table.Column<int>(type: "integer", nullable: false),
                    senderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    receiverRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    receiverID = table.Column<int>(type: "integer", nullable: false),
                    productID = table.Column<int>(type: "integer", nullable: true),
                    productName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deletedBySender = table.Column<bool>(type: "boolean", nullable: false),
                    deletedByReceiver = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.messageID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropColumn(
                name: "sortOrder",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "deletionRequestedAt",
                table: "Merchant");

            migrationBuilder.DropColumn(
                name: "isDisabled",
                table: "Merchant");

            migrationBuilder.DropColumn(
                name: "deletionRequestedAt",
                table: "Customer");
        }
    }
}
