using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.EHR.Service.Migrations
{
    /// <inheritdoc />
    public partial class EhrSchemaV2 : Migration
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
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_doctor = table.Column<Guid>(type: "uuid", nullable: false),
                    current_version = table.Column<int>(type: "integer", nullable: false),
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
                    version = table.Column<int>(type: "integer", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    change_reason = table.Column<string>(type: "text", nullable: true),
                    previous_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    blockchain_tx_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tx_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.ForeignKey(
                        name: "FK_ehr_versions_ehr_versions_previous_version_id",
                        column: x => x.previous_version_id,
                        principalTable: "ehr_versions",
                        principalColumn: "version_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ehr_files",
                columns: table => new
                {
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    report_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    file_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EhrVersionVersionId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ehr_files_ehr_versions_EhrVersionVersionId",
                        column: x => x.EhrVersionVersionId,
                        principalTable: "ehr_versions",
                        principalColumn: "version_id");
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
                name: "IX_ehr_files_created_by_created_at",
                table: "ehr_files",
                columns: new[] { "created_by", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_files_ehr_id_version_report_type",
                table: "ehr_files",
                columns: new[] { "ehr_id", "version", "report_type" });

            migrationBuilder.CreateIndex(
                name: "IX_ehr_files_EhrVersionVersionId",
                table: "ehr_files",
                column: "EhrVersionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_records_encounter_id",
                table: "ehr_records",
                column: "encounter_id");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_records_hospital_id_created_at",
                table: "ehr_records",
                columns: new[] { "hospital_id", "created_at" });

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
                name: "IX_ehr_versions_blockchain_tx_hash",
                table: "ehr_versions",
                column: "blockchain_tx_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ehr_versions_ehr_id_version",
                table: "ehr_versions",
                columns: new[] { "ehr_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ehr_versions_previous_version_id",
                table: "ehr_versions",
                column: "previous_version_id");
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
