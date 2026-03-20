using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Consent.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_requests",
                columns: table => new
                {
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    requester_type = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: true),
                    permission = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    purpose = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    requested_duration_days = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    consent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_requests", x => x.request_id);
                });

            migrationBuilder.CreateTable(
                name: "consents",
                columns: table => new
                {
                    consent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blockchain_consent_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    grantee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grantee_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    grantee_type = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: true),
                    permission = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    purpose = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: true),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    grant_tx_hash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true),
                    revoke_tx_hash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true),
                    blockchain_block_num = table.Column<long>(type: "bigint", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consents", x => x.consent_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_requests");

            migrationBuilder.DropTable(
                name: "consents");
        }
    }
}
