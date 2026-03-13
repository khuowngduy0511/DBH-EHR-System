using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class updateUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.permission_id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_adrress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    public_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "permission_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "doctors",
                columns: table => new
                {
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_id = table.Column<Guid>(type: "uuid", nullable: true),
                    specialty = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    license_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    license_image = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctors", x => x.doctor_id);
                    table.ForeignKey(
                        name: "FK_doctors_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dob = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BloodType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "user_credentials",
                columns: table => new
                {
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    credential_value = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    EncryptedPrivateKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_credentials", x => x.credential_id);
                    table.ForeignKey(
                        name: "FK_user_credentials_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    mfa_method = table.Column<string>(type: "text", nullable: true),
                    last_password_change = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_mfa_enroll_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_security", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_security_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "role_name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Doctor" },
                    { 3, "Pharmacist" },
                    { 4, "Nurse" },
                    { 5, "Patient" },
                    { 6, "Receptionist" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "user_id", "created_at", "email", "full_name", "ip_adrress", "password", "phone", "public_key", "status" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@dbh.com", "Admin User", null, "$2a$11$HzZ0Sv6r5FKblQFEwdg6B.U5pxGNfiZ6WP5CZvgf9kXKA3T8apxH6", "1234567890", null, "Active" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "doctor@dbh.com", "Dr. House", null, "$2a$11$CL0afOW/xRSTQva5BXAUT.I8AfosvswxfRpBYAODAbhs7kIjE.j6q", "1234567891", null, "Active" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "pharmacist@dbh.com", "Pharma Joe", null, "$2a$11$/72MIX/PqeKqerfiN4TU4u1y9EB84Weh4AxaI32CXRbBxkpDG4VG6", "1234567892", null, "Active" },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "nurse@dbh.com", "Nurse Joy", null, "$2a$11$zMKDcVB7yXWx6..qxdYHm.6XE8TtpkQfuSXutHK1wzqIe2bwg8HYe", "1234567893", null, "Active" },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "patient@dbh.com", "John Doe", null, "$2a$11$aR3.eEY6uDdsI3swbuFjDuUsC46DEwtf.78PbftP3ndKyxhI4F1Gm", "1234567894", null, "Active" },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "receptionist@dbh.com", "Pam Beesly", null, "$2a$11$hrYXX1jDexwhh1fiN3xOW.bhPk7JsDaowa/6O87NkpkLgSb4mWv1u", "1234567895", null, "Active" }
                });

            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "PatientId", "BloodType", "CreatedAt", "Dob", "Gender", "UserId" },
                values: new object[] { new Guid("61a09701-05bd-43aa-a136-8d422770d653"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 1, 1), "Male", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "created_at", "hospital_id", "license_image", "license_number", "specialty", "user_id" },
                values: new object[] { new Guid("4e792870-771c-48ea-abee-a24b7f3b468e"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "created_at", "hospital_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("057ef0df-d75f-4a91-b67c-d3eda0b56546"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" },
                    { new Guid("2db9ae06-e0f4-4cbc-90e8-c91e64e0108f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" },
                    { new Guid("7c444361-a31a-40d5-9117-29ff2f4b8a96"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" }
                });

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
                name: "IX_doctors_user_id",
                table: "doctors",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_role_name",
                table: "roles",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_user_id",
                table: "staff",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_credentials_user_id_provider",
                table: "user_credentials",
                columns: new[] { "user_id", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "user_credentials");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_security");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
