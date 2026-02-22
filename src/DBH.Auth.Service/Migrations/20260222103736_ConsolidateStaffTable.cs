using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateStaffTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_security_users_user_id",
                table: "user_security");

            migrationBuilder.DropTable(
                name: "nurses");

            migrationBuilder.DropTable(
                name: "pharmacist");

            migrationBuilder.DropTable(
                name: "receptionist");

            migrationBuilder.DropIndex(
                name: "IX_user_credentials_user_id_provider",
                table: "user_credentials");

            migrationBuilder.DropIndex(
                name: "IX_roles_role_name",
                table: "roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_security",
                table: "user_security");

            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("c55ac732-b666-4c0d-87d0-422bbab63e7f"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("16910d5a-96d5-43bc-aa56-c412cda912c0"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 1, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 2, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 3, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 4, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 5, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 6, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") });

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            migrationBuilder.RenameTable(
                name: "user_security",
                newName: "user_securities");

            migrationBuilder.RenameColumn(
                name: "ip_adrress",
                table: "users",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "verified_at",
                table: "user_credentials",
                newName: "VerifiedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_securities",
                table: "user_securities",
                column: "user_id");

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    staff_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: true),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    specialty = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verified_status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.staff_id);
                    table.ForeignKey(
                        name: "FK_staff_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: 3,
                column: "role_name",
                value: "Nurse");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: 4,
                column: "role_name",
                value: "Pharmacist");

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "role_name" },
                values: new object[] { 7, "LabTech" });

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "IX_user_credentials_user_id",
                table: "user_credentials",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_role_verified_status",
                table: "staff",
                columns: new[] { "role", "verified_status" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_user_id",
                table: "staff",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_securities_users_user_id",
                table: "user_securities",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_securities_users_user_id",
                table: "user_securities");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropIndex(
                name: "IX_users_phone",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_credentials_user_id",
                table: "user_credentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_securities",
                table: "user_securities");

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: 7);

            migrationBuilder.RenameTable(
                name: "user_securities",
                newName: "user_security");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "users",
                newName: "ip_adrress");

            migrationBuilder.RenameColumn(
                name: "VerifiedAt",
                table: "user_credentials",
                newName: "verified_at");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_security",
                table: "user_security",
                column: "user_id");

            migrationBuilder.CreateTable(
                name: "nurses",
                columns: table => new
                {
                    nurse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: true),
                    specialty = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nurses", x => x.nurse_id);
                    table.ForeignKey(
                        name: "FK_nurses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacist",
                columns: table => new
                {
                    pharmacist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: true),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacist", x => x.pharmacist_id);
                    table.ForeignKey(
                        name: "FK_pharmacist_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receptionist",
                columns: table => new
                {
                    receptionist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receptionist", x => x.receptionist_id);
                    table.ForeignKey(
                        name: "FK_receptionist_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: 3,
                column: "role_name",
                value: "Pharmacist");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: 4,
                column: "role_name",
                value: "Nurse");

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "user_id", "created_at", "email", "full_name", "ip_adrress", "password", "phone", "public_key", "status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@dbh.com", "Admin User", null, "$2a$11$XLuCUzIUQfjc0b.0NfM7cu8QMyKGnpVcD9WrPVZR/E8aVPHr166ai", "1234567890", null, "Active" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "doctor@dbh.com", "Dr. House", null, "$2a$11$qoWD3OoiLWUzlBrmFmsS4u23pdk6bCnOes5fZU70Uk8EjrfI6EKXO", "1234567891", null, "Active" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "pharmacist@dbh.com", "Pharma Joe", null, "$2a$11$rbf/E0oHQBxTOWSuEUJW/OmRGtkhuKUuUGmR1I1tc.oV/r5gdeSTa", "1234567892", null, "Active" },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "nurse@dbh.com", "Nurse Joy", null, "$2a$11$C/bYK069AX.Su/sDwAhwUejc8pDsLkYMADIsx6nnqBZTwnX9VL2EK", "1234567893", null, "Active" },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "patient@dbh.com", "John Doe", null, "$2a$11$Vpcxg/pmrVGdsDIukDnrYOGaU4WsVNpEdmng3ewAan4lUXCYz.2ee", "1234567894", null, "Active" },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "receptionist@dbh.com", "Pam Beesly", null, "$2a$11$nqB694vO7GMu0jaDHKbK0.uJc.dhqOWC3WIvV52kCW40ZRNwuSmBq", "1234567895", null, "Active" }
                });

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "created_at", "hospital_id", "license_image", "license_number", "specialty", "user_id" },
                values: new object[] { new Guid("c55ac732-b666-4c0d-87d0-422bbab63e7f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") });

            migrationBuilder.InsertData(
                table: "nurses",
                columns: new[] { "nurse_id", "created_at", "hospital_id", "specialty", "user_id" },
                values: new object[] { new Guid("28c0c388-ae29-44a1-a63a-003cefa26412"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "created_at", "dob", "gender", "user_id" },
                values: new object[] { new Guid("16910d5a-96d5-43bc-aa56-c412cda912c0"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 1, 1), "Male", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "pharmacist",
                columns: new[] { "pharmacist_id", "created_at", "hospital_id", "license_number", "user_id" },
                values: new object[] { new Guid("57cf0ada-f586-4149-bcdb-73ec342e1fb9"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "PHARM123", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") });

            migrationBuilder.InsertData(
                table: "receptionist",
                columns: new[] { "receptionist_id", "created_at", "hospital_id", "user_id" },
                values: new object[] { new Guid("7be60b7a-3771-4f7b-a0fe-8caf44102a97"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "role_id", "user_id" },
                values: new object[,]
                {
                    { 1, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { 2, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                    { 3, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { 4, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { 5, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { 6, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") }
                });

            migrationBuilder.InsertData(
                table: "user_security",
                columns: new[] { "user_id", "last_mfa_enroll_at", "last_password_change", "mfa_enabled", "mfa_method" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, null, false, null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, null, false, null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), null, null, false, null },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), null, null, false, null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), null, null, false, null },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), null, null, false, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_credentials_user_id_provider",
                table: "user_credentials",
                columns: new[] { "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_name",
                table: "roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nurses_user_id",
                table: "nurses",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pharmacist_user_id",
                table: "pharmacist",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receptionist_user_id",
                table: "receptionist",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_security_users_user_id",
                table: "user_security",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
