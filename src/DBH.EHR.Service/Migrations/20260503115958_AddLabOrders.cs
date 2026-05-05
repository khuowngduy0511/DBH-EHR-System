using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.EHR.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddLabOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "encrypted_aes_key",
                table: "ehr_files",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lab_orders",
                columns: table => new
                {
                    lab_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ehr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    org_id = table.Column<Guid>(type: "uuid", nullable: true),
                    test_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    clinical_note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    result_note = table.Column<string>(type: "text", nullable: true),
                    result_values = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_orders", x => x.lab_order_id);
                    table.ForeignKey(
                        name: "FK_lab_orders_ehr_records_ehr_id",
                        column: x => x.ehr_id,
                        principalTable: "ehr_records",
                        principalColumn: "ehr_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_ehr_id",
                table: "lab_orders",
                column: "ehr_id");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_org_id_status",
                table: "lab_orders",
                columns: new[] { "org_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_patient_id_status",
                table: "lab_orders",
                columns: new[] { "patient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_requested_by",
                table: "lab_orders",
                column: "requested_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lab_orders");

            migrationBuilder.DropColumn(
                name: "encrypted_aes_key",
                table: "ehr_files");
        }
    }
}
