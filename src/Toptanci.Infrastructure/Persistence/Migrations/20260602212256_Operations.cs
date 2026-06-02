using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Toptanci.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Operations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokenProductRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokenProductRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrokenProductRecords_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BrokenProductRecords_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCounts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransfers_Warehouses_TargetWarehouseId",
                        column: x => x.TargetWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCountLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountedQuantity = table.Column<int>(type: "int", nullable: false),
                    Difference = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCountLines_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCountLines_StockCounts_StockCountId",
                        column: x => x.StockCountId,
                        principalTable: "StockCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransferLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransferLines_WarehouseTransfers_WarehouseTransferId",
                        column: x => x.WarehouseTransferId,
                        principalTable: "WarehouseTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokenProductRecords_VariantId",
                table: "BrokenProductRecords",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_BrokenProductRecords_WarehouseId",
                table: "BrokenProductRecords",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_StockCountId_VariantId",
                table: "StockCountLines",
                columns: new[] { "StockCountId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockCountLines_VariantId",
                table: "StockCountLines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCounts_WarehouseId",
                table: "StockCounts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_VariantId",
                table: "WarehouseTransferLines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransferLines_WarehouseTransferId",
                table: "WarehouseTransferLines",
                column: "WarehouseTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_IdempotencyKey",
                table: "WarehouseTransfers",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_SourceWarehouseId",
                table: "WarehouseTransfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransfers_TargetWarehouseId",
                table: "WarehouseTransfers",
                column: "TargetWarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokenProductRecords");

            migrationBuilder.DropTable(
                name: "StockCountLines");

            migrationBuilder.DropTable(
                name: "WarehouseTransferLines");

            migrationBuilder.DropTable(
                name: "StockCounts");

            migrationBuilder.DropTable(
                name: "WarehouseTransfers");
        }
    }
}
