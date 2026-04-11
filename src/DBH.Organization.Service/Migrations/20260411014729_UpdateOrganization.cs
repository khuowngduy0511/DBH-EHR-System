using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBH.Organization.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "memberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "memberships",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"),
                columns: new[] { "created_by", "updated_by" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"),
                columns: new[] { "created_by", "updated_by" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333303"),
                columns: new[] { "created_by", "updated_by" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333304"),
                columns: new[] { "created_by", "updated_by" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333305"),
                columns: new[] { "created_by", "updated_by" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333306"),
                columns: new[] { "created_by", "updated_by" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "memberships");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "memberships");
        }
    }
}
