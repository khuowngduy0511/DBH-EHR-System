using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Appointment.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppointmentService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "encounters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "encounters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "encounters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "appointments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "appointments");
        }
    }
}
