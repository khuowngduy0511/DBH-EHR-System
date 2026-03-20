using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Notification.Service.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "device_tokens",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_did = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fcm_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    device_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    os_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    app_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_did = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ehr_access_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    consent_request_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ehr_update_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    appointment_reminder_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    security_alert_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    system_notification_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    push_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sms_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    quiet_hours_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    quiet_hours_start = table.Column<int>(type: "integer", nullable: false),
                    quiet_hours_end = table.Column<int>(type: "integer", nullable: false),
                    timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_did = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    action_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    external_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_fcm_token",
                schema: "notification",
                table: "device_tokens",
                column: "fcm_token");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_user_did",
                schema: "notification",
                table: "device_tokens",
                column: "user_did");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_user_did_is_active",
                schema: "notification",
                table: "device_tokens",
                columns: new[] { "user_did", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_did",
                schema: "notification",
                table: "notification_preferences",
                column: "user_did",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_created_at",
                schema: "notification",
                table: "notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_did",
                schema: "notification",
                table: "notifications",
                column: "recipient_did");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_user_id",
                schema: "notification",
                table: "notifications",
                column: "recipient_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_status",
                schema: "notification",
                table: "notifications",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_tokens",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notification");
        }
    }
}
