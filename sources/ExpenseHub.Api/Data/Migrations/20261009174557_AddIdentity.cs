using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EH_ROLES",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_ROLES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EH_USERS",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    UserName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    PasswordHash = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_USERS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EH_ROLE_CLAIMS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    RoleId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ClaimValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_ROLE_CLAIMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EH_ROLE_CLAIMS_EH_ROLES_RoleId",
                        column: x => x.RoleId,
                        principalTable: "EH_ROLES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EH_USER_CLAIMS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    ClaimValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_USER_CLAIMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EH_USER_CLAIMS_EH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "EH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EH_USER_LOGINS",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_USER_LOGINS", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_EH_USER_LOGINS_EH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "EH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EH_USER_ROLES",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    RoleId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_USER_ROLES", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_EH_USER_ROLES_EH_ROLES_RoleId",
                        column: x => x.RoleId,
                        principalTable: "EH_ROLES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EH_USER_ROLES_EH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "EH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EH_USER_TOKENS",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Value = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EH_USER_TOKENS", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_EH_USER_TOKENS_EH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "EH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EH_PAYMENT_RECORDS_PaidById",
                table: "EH_PAYMENT_RECORDS",
                column: "PaidById");

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSES_DecidedById",
                table: "EH_EXPENSES",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_EH_EXPENSE_HISTORY_ActorId",
                table: "EH_EXPENSE_HISTORY",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_EH_ROLE_CLAIMS_RoleId",
                table: "EH_ROLE_CLAIMS",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "EH_ROLES",
                column: "NormalizedName",
                unique: true,
                filter: "\"NormalizedName\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EH_USER_CLAIMS_UserId",
                table: "EH_USER_CLAIMS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EH_USER_LOGINS_UserId",
                table: "EH_USER_LOGINS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EH_USER_ROLES_RoleId",
                table: "EH_USER_ROLES",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "EH_USERS",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "EH_USERS",
                column: "NormalizedUserName",
                unique: true,
                filter: "\"NormalizedUserName\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EH_EXPENSE_HISTORY_EH_USERS_ActorId",
                table: "EH_EXPENSE_HISTORY",
                column: "ActorId",
                principalTable: "EH_USERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EH_EXPENSES_EH_USERS_DecidedById",
                table: "EH_EXPENSES",
                column: "DecidedById",
                principalTable: "EH_USERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EH_EXPENSES_EH_USERS_OwnerId",
                table: "EH_EXPENSES",
                column: "OwnerId",
                principalTable: "EH_USERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EH_PAYMENT_RECORDS_EH_USERS_PaidById",
                table: "EH_PAYMENT_RECORDS",
                column: "PaidById",
                principalTable: "EH_USERS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EH_EXPENSE_HISTORY_EH_USERS_ActorId",
                table: "EH_EXPENSE_HISTORY");

            migrationBuilder.DropForeignKey(
                name: "FK_EH_EXPENSES_EH_USERS_DecidedById",
                table: "EH_EXPENSES");

            migrationBuilder.DropForeignKey(
                name: "FK_EH_EXPENSES_EH_USERS_OwnerId",
                table: "EH_EXPENSES");

            migrationBuilder.DropForeignKey(
                name: "FK_EH_PAYMENT_RECORDS_EH_USERS_PaidById",
                table: "EH_PAYMENT_RECORDS");

            migrationBuilder.DropTable(
                name: "EH_ROLE_CLAIMS");

            migrationBuilder.DropTable(
                name: "EH_USER_CLAIMS");

            migrationBuilder.DropTable(
                name: "EH_USER_LOGINS");

            migrationBuilder.DropTable(
                name: "EH_USER_ROLES");

            migrationBuilder.DropTable(
                name: "EH_USER_TOKENS");

            migrationBuilder.DropTable(
                name: "EH_ROLES");

            migrationBuilder.DropTable(
                name: "EH_USERS");

            migrationBuilder.DropIndex(
                name: "IX_EH_PAYMENT_RECORDS_PaidById",
                table: "EH_PAYMENT_RECORDS");

            migrationBuilder.DropIndex(
                name: "IX_EH_EXPENSES_DecidedById",
                table: "EH_EXPENSES");

            migrationBuilder.DropIndex(
                name: "IX_EH_EXPENSE_HISTORY_ActorId",
                table: "EH_EXPENSE_HISTORY");
        }
    }
}
