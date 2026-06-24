using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUser_Users_Id",
                table: "ApplicationUser");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Users_CreatedByUserId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Users_DeletedById",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Users_LastModifiedByUserId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSubscriptions_Users_CreatedByUserId",
                table: "OrganizationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSubscriptions_Users_DeletedById",
                table: "OrganizationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSubscriptions_Users_LastModifiedByUserId",
                table: "OrganizationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_Users_CreatedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_Users_DeletedById",
                table: "SubscriptionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_Users_LastModifiedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_Users_CreatedByUserId",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_Users_DeletedById",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_Users_LastModifiedByUserId",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_Users_UserId",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedByUserId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_DeletedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_LastModifiedByUserId",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationUser",
                table: "ApplicationUser");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "DomainUsers");

            migrationBuilder.RenameTable(
                name: "ApplicationUser",
                newName: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "IX_Users_OrganizationId",
                table: "DomainUsers",
                newName: "IX_DomainUsers_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_LastModifiedByUserId",
                table: "DomainUsers",
                newName: "IX_DomainUsers_LastModifiedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_DeletedById",
                table: "DomainUsers",
                newName: "IX_DomainUsers_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_Users_CreatedByUserId",
                table: "DomainUsers",
                newName: "IX_DomainUsers_CreatedByUserId");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DomainUsers",
                table: "DomainUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_DomainUsers_Id",
                table: "AspNetUsers",
                column: "Id",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DomainUsers_DomainUsers_CreatedByUserId",
                table: "DomainUsers",
                column: "CreatedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DomainUsers_DomainUsers_DeletedById",
                table: "DomainUsers",
                column: "DeletedById",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DomainUsers_DomainUsers_LastModifiedByUserId",
                table: "DomainUsers",
                column: "LastModifiedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DomainUsers_Organizations_OrganizationId",
                table: "DomainUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_DomainUsers_CreatedByUserId",
                table: "Organizations",
                column: "CreatedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_DomainUsers_DeletedById",
                table: "Organizations",
                column: "DeletedById",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_DomainUsers_LastModifiedByUserId",
                table: "Organizations",
                column: "LastModifiedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSubscriptions_DomainUsers_CreatedByUserId",
                table: "OrganizationSubscriptions",
                column: "CreatedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSubscriptions_DomainUsers_DeletedById",
                table: "OrganizationSubscriptions",
                column: "DeletedById",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSubscriptions_DomainUsers_LastModifiedByUserId",
                table: "OrganizationSubscriptions",
                column: "LastModifiedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_DomainUsers_CreatedByUserId",
                table: "SubscriptionPlans",
                column: "CreatedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_DomainUsers_DeletedById",
                table: "SubscriptionPlans",
                column: "DeletedById",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_DomainUsers_LastModifiedByUserId",
                table: "SubscriptionPlans",
                column: "LastModifiedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_CreatedByUserId",
                table: "TranscriptionJobs",
                column: "CreatedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_DeletedById",
                table: "TranscriptionJobs",
                column: "DeletedById",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_LastModifiedByUserId",
                table: "TranscriptionJobs",
                column: "LastModifiedByUserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_UserId",
                table: "TranscriptionJobs",
                column: "UserId",
                principalTable: "DomainUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_DomainUsers_Id",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_DomainUsers_DomainUsers_CreatedByUserId",
                table: "DomainUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_DomainUsers_DomainUsers_DeletedById",
                table: "DomainUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_DomainUsers_DomainUsers_LastModifiedByUserId",
                table: "DomainUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_DomainUsers_Organizations_OrganizationId",
                table: "DomainUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_DomainUsers_CreatedByUserId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_DomainUsers_DeletedById",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_DomainUsers_LastModifiedByUserId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSubscriptions_DomainUsers_CreatedByUserId",
                table: "OrganizationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSubscriptions_DomainUsers_DeletedById",
                table: "OrganizationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganizationSubscriptions_DomainUsers_LastModifiedByUserId",
                table: "OrganizationSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_DomainUsers_CreatedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_DomainUsers_DeletedById",
                table: "SubscriptionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_DomainUsers_LastModifiedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_CreatedByUserId",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_DeletedById",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_LastModifiedByUserId",
                table: "TranscriptionJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_TranscriptionJobs_DomainUsers_UserId",
                table: "TranscriptionJobs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DomainUsers",
                table: "DomainUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "DomainUsers",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "ApplicationUser");

            migrationBuilder.RenameIndex(
                name: "IX_DomainUsers_OrganizationId",
                table: "Users",
                newName: "IX_Users_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_DomainUsers_LastModifiedByUserId",
                table: "Users",
                newName: "IX_Users_LastModifiedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DomainUsers_DeletedById",
                table: "Users",
                newName: "IX_Users_DeletedById");

            migrationBuilder.RenameIndex(
                name: "IX_DomainUsers_CreatedByUserId",
                table: "Users",
                newName: "IX_Users_CreatedByUserId");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "ApplicationUser",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedUserName",
                table: "ApplicationUser",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedEmail",
                table: "ApplicationUser",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ApplicationUser",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationUser",
                table: "ApplicationUser",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUser_Users_Id",
                table: "ApplicationUser",
                column: "Id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Users_CreatedByUserId",
                table: "Organizations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Users_DeletedById",
                table: "Organizations",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Users_LastModifiedByUserId",
                table: "Organizations",
                column: "LastModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSubscriptions_Users_CreatedByUserId",
                table: "OrganizationSubscriptions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSubscriptions_Users_DeletedById",
                table: "OrganizationSubscriptions",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganizationSubscriptions_Users_LastModifiedByUserId",
                table: "OrganizationSubscriptions",
                column: "LastModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_Users_CreatedByUserId",
                table: "SubscriptionPlans",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_Users_DeletedById",
                table: "SubscriptionPlans",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_Users_LastModifiedByUserId",
                table: "SubscriptionPlans",
                column: "LastModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_Users_CreatedByUserId",
                table: "TranscriptionJobs",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_Users_DeletedById",
                table: "TranscriptionJobs",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_Users_LastModifiedByUserId",
                table: "TranscriptionJobs",
                column: "LastModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TranscriptionJobs_Users_UserId",
                table: "TranscriptionJobs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedByUserId",
                table: "Users",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_DeletedById",
                table: "Users",
                column: "DeletedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_LastModifiedByUserId",
                table: "Users",
                column: "LastModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
