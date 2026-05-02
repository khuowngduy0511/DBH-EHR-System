using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Consent.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentModifyAcceptMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Audit metadata for Modify-and-Accept consent flow

            migrationBuilder.AddColumn<Guid>(
                name: "requested_by",
                table: "consents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                table: "consents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "original_access_request_id",
                table: "consents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_patient_modified",
                table: "consents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requested_by",
                table: "consents");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "consents");

            migrationBuilder.DropColumn(
                name: "original_access_request_id",
                table: "consents");

            migrationBuilder.DropColumn(
                name: "is_patient_modified",
                table: "consents");
        }
    }
}
