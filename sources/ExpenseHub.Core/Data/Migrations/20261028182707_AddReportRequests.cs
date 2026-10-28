using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseHub.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EH_REPORT_REQUESTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    RequesterId = table.Column<string>(type: "NVARCHAR2(450)", maxLength: 450, nullable: false),
                    Scope = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "DATE", nullable: false),
                    ToDate = table.Column<DateTime>(type: "DATE", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    ResultJson = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true),
                    ErrorCode = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    Attempts = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_REPORT_REQUESTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EH_REPORT_REQUESTS_EH_USERS_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "EH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EH_REPORT_REQUESTS_RequesterId_CreatedAtUtc",
                table: "EH_REPORT_REQUESTS",
                columns: new[] { "RequesterId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EH_REPORT_REQUESTS");
        }
    }
}
