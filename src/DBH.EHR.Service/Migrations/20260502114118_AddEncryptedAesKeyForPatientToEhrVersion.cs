using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.EHR.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedAesKeyForPatientToEhrVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "encrypted_aes_key_for_patient",
                table: "ehr_versions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "encrypted_aes_key_for_patient",
                table: "ehr_versions");
        }
    }
}
