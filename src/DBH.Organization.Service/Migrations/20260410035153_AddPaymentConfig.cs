using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Organization.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_configs",
                columns: table => new
                {
                    config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    encrypted_api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    encrypted_checksum_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_configs", x => x.config_id);
                    table.ForeignKey(
                        name: "FK_payment_configs_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_configs_org_id",
                table: "payment_configs",
                column: "org_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_configs");
        }
    }
}
