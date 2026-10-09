using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialExpenseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EH_EXPENSE_CATEGORIES",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_EXPENSE_CATEGORIES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EH_EXPENSES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    OwnerId = table.Column<string>(type: "NVARCHAR2(450)", maxLength: 450, nullable: false),
                    CategoryId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "DATE", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DecidedById = table.Column<string>(type: "NVARCHAR2(450)", maxLength: 450, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    RejectionReason = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_EXPENSES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EH_EXPENSES_EH_EXPENSE_CATEGORIES_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EH_EXPENSE_CATEGORIES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EH_EXPENSE_HISTORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Action = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    ActorId = table.Column<string>(type: "NVARCHAR2(450)", maxLength: 450, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FromStatus = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Justification = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Changes = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_EXPENSE_HISTORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EH_EXPENSE_HISTORY_EH_EXPENSES_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "EH_EXPENSES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EH_PAYMENT_RECORDS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ExpenseId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    PaidById = table.Column<string>(type: "NVARCHAR2(450)", maxLength: 450, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_PAYMENT_RECORDS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EH_PAYMENT_RECORDS_EH_EXPENSES_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "EH_EXPENSES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EH_EXPENSE_CATEGORIES",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Alimentação" },
                    { 2, "Transporte" },
                    { 3, "Hospedagem" },
                    { 4, "Material de escritório" },
                    { 5, "Outros" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSE_CATEGORIES_Name",
                table: "EH_EXPENSE_CATEGORIES",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSE_HISTORY_ExpenseId_OccurredAtUtc",
                table: "EH_EXPENSE_HISTORY",
                columns: new[] { "ExpenseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSES_CategoryId",
                table: "EH_EXPENSES",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSES_OwnerId",
                table: "EH_EXPENSES",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSES_Status",
                table: "EH_EXPENSES",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EH_PAYMENT_RECORDS_ExpenseId",
                table: "EH_PAYMENT_RECORDS",
                column: "ExpenseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EH_EXPENSE_HISTORY");

            migrationBuilder.DropTable(
                name: "EH_PAYMENT_RECORDS");

            migrationBuilder.DropTable(
                name: "EH_EXPENSES");

            migrationBuilder.DropTable(
                name: "EH_EXPENSE_CATEGORIES");
        }
    }
}
