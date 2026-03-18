using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.EHR.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ehr_records",
                columns: table => new
                {
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    org_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ehr_records", x => x.ehr_id);
                });

            migrationBuilder.CreateTable(
                name: "ehr_access_log",
                columns: table => new
                {
                    access_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    accessed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    access_action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    consent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    verify_status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    accessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ehr_access_log", x => x.access_id);
                    table.ForeignKey(
                        name: "FK_ehr_access_log_ehr_records_ehr_id",
                        column: x => x.ehr_id,
                        principalTable: "ehr_records",
                        principalColumn: "ehr_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ehr_files",
                columns: table => new
                {
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    file_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ehr_files", x => x.file_id);
                    table.ForeignKey(
                        name: "FK_ehr_files_ehr_records_ehr_id",
                        column: x => x.ehr_id,
                        principalTable: "ehr_records",
                        principalColumn: "ehr_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ehr_subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    auto_renew = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_billing_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ehr_subscriptions", x => x.subscription_id);
                    table.ForeignKey(
                        name: "FK_ehr_subscriptions_ehr_records_ehr_id",
                        column: x => x.ehr_id,
                        principalTable: "ehr_records",
                        principalColumn: "ehr_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ehr_versions",
                columns: table => new
                {
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ehr_versions", x => x.version_id);
                    table.ForeignKey(
                        name: "FK_ehr_versions_ehr_records_ehr_id",
                        column: x => x.ehr_id,
                        principalTable: "ehr_records",
                        principalColumn: "ehr_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_access_log_accessed_by_accessed_at",
                table: "ehr_access_log",
                columns: new[] { "accessed_by", "accessed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_access_log_ehr_id_accessed_at",
                table: "ehr_access_log",
                columns: new[] { "ehr_id", "accessed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_files_ehr_id",
                table: "ehr_files",
                column: "ehr_id");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_records_encounter_id",
                table: "ehr_records",
                column: "encounter_id");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_records_org_id_created_at",
                table: "ehr_records",
                columns: new[] { "org_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_records_patient_id_created_at",
                table: "ehr_records",
                columns: new[] { "patient_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_subscriptions_ehr_id_status",
                table: "ehr_subscriptions",
                columns: new[] { "ehr_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_subscriptions_patient_id_status",
                table: "ehr_subscriptions",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_versions_ehr_id_version_number",
                table: "ehr_versions",
                columns: new[] { "ehr_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ehr_access_log");

            migrationBuilder.DropTable(
                name: "ehr_files");

            migrationBuilder.DropTable(
                name: "ehr_subscriptions");

            migrationBuilder.DropTable(
                name: "ehr_versions");

            migrationBuilder.DropTable(
                name: "ehr_records");
        }
    }
}
