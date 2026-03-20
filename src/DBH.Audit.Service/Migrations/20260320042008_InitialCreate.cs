using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Audit.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    blockchain_audit_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_type = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    action = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    target_type = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    patient_did = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    consent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result = table.Column<int>(type: "integer", maxLength: 30, nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    session_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    blockchain_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    blockchain_tx_hash = table.Column<string>(type: "character varying(66)", maxLength: 66, nullable: true),
                    blockchain_block_num = table.Column<long>(type: "bigint", nullable: true),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.audit_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");
        }
    }
}
