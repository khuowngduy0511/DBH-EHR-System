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
                name: "change_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requested_scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ttl_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approvals = table.Column<string>(type: "jsonb", nullable: false),
                    offchain_doc_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ehr_index",
                columns: table => new
                {
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    owner_org = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    offchain_doc_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    record_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ehr_index", x => x.record_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_created_at",
                table: "change_requests",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_patient_id",
                table: "change_requests",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_status",
                table: "change_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_index_offchain_doc_id",
                table: "ehr_index",
                column: "offchain_doc_id");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_index_owner_org",
                table: "ehr_index",
                column: "owner_org");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_index_patient_id",
                table: "ehr_index",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "IX_ehr_index_status",
                table: "ehr_index",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "change_requests");

            migrationBuilder.DropTable(
                name: "ehr_index");
        }
    }
}
