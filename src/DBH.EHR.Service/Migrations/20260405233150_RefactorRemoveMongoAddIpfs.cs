using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.EHR.Service.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRemoveMongoAddIpfs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data",
                table: "ehr_versions");

            migrationBuilder.DropColumn(
                name: "data",
                table: "ehr_records");

            migrationBuilder.AddColumn<string>(
                name: "data_hash",
                table: "ehr_versions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "encrypted_fallback_data",
                table: "ehr_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ipfs_cid",
                table: "ehr_versions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "encrypted_fallback_data",
                table: "ehr_files",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ipfs_cid",
                table: "ehr_files",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_hash",
                table: "ehr_versions");

            migrationBuilder.DropColumn(
                name: "encrypted_fallback_data",
                table: "ehr_versions");

            migrationBuilder.DropColumn(
                name: "ipfs_cid",
                table: "ehr_versions");

            migrationBuilder.DropColumn(
                name: "encrypted_fallback_data",
                table: "ehr_files");

            migrationBuilder.DropColumn(
                name: "ipfs_cid",
                table: "ehr_files");

            migrationBuilder.AddColumn<string>(
                name: "data",
                table: "ehr_versions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data",
                table: "ehr_records",
                type: "jsonb",
                nullable: true);
        }
    }
}
