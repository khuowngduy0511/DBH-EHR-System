using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Organization.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    org_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    org_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    org_type = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "jsonb", nullable: true),
                    contact_info = table.Column<string>(type: "jsonb", nullable: true),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fabric_msp_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fabric_channel_peers = table.Column<string>(type: "jsonb", nullable: true),
                    fabric_ca_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.org_id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    department_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    head_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    floor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    room_numbers = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.department_id);
                    table.ForeignKey(
                        name: "FK_departments_departments_parent_department_id",
                        column: x => x.parent_department_id,
                        principalTable: "departments",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_departments_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                columns: table => new
                {
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    job_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    qualifications = table.Column<string>(type: "jsonb", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    org_permissions = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memberships", x => x.membership_id);
                    table.ForeignKey(
                        name: "FK_memberships_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_memberships_organizations_org_id",
                        column: x => x.org_id,
                        principalTable: "organizations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_departments_org_id",
                table: "departments",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_parent_department_id",
                table: "departments",
                column: "parent_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_memberships_department_id",
                table: "memberships",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_memberships_org_id",
                table: "memberships",
                column: "org_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "memberships");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
