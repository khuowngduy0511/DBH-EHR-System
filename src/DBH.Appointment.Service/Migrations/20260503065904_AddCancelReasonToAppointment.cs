using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Appointment.Service.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelReasonToAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "appointments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "appointments");
        }
    }
}
