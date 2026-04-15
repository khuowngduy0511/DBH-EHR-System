using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Consent.Service.Migrations
{
    /// <inheritdoc />
    public partial class RemoveConditionsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "conditions",
                table: "consents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "conditions",
                table: "consents",
                type: "jsonb",
                nullable: true);
        }
    }
}
