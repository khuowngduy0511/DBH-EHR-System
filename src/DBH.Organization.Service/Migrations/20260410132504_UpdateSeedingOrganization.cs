using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Organization.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedingOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "organizations",
                columns: new[] { "org_id", "address", "contact_info", "created_at", "fabric_ca_url", "fabric_channel_peers", "fabric_msp_id", "license_number", "logo_url", "org_code", "org_did", "org_name", "org_type", "settings", "status", "tax_id", "timezone", "updated_at", "verified_at", "verified_by", "website" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "{\"line\":[\"215 Hong Bang\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 5\",\"country\":\"VN\",\"postalCode\":\"700000\"}", "{\"phone\":\"028-3855-4269\",\"fax\":\"028-3855-4270\",\"email\":\"contact@bvdktu.vn\",\"hotline\":\"1900-1234\"}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "http://ca_hospital1:7054", "[\"peer0.hospital1.ehr.com\"]", "Hospital1MSP", "BV-HCM-001", null, "BVDKTU", "did:dbh:org:hospital-a", "Benh vien Da khoa Trung uong", 0, null, 0, "0301234567", "Asia/Ho_Chi_Minh", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "https://bvdktu.vn" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "{\"line\":[\"341 Su Van Hanh\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 10\",\"country\":\"VN\",\"postalCode\":\"700000\"}", "{\"phone\":\"028-3927-1119\",\"fax\":\"028-3927-1120\",\"email\":\"contact@bvnd1.vn\",\"hotline\":\"1900-5678\"}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "http://ca_hospital2:8054", "[\"peer0.hospital2.ehr.com\"]", "Hospital2MSP", "BV-HCM-002", null, "BVND1", "did:dbh:org:hospital-b", "Benh vien Nhi Dong 1", 0, null, 0, "0301234568", "Asia/Ho_Chi_Minh", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "https://bvnd1.vn" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "{\"line\":[\"123 Nguyen Hue\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 1\",\"country\":\"VN\",\"postalCode\":\"700000\"}", "{\"phone\":\"028-3821-0000\",\"fax\":\"028-3821-0001\",\"email\":\"contact@pkdlsg.vn\",\"hotline\":\"1900-9012\"}", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "http://ca_clinic:10054", "[\"peer0.clinic.ehr.com\"]", "ClinicMSP", "PK-HCM-001", null, "PKDLSG", "did:dbh:org:clinic-a", "Phong kham Da lieu Sai Gon", 1, null, 0, "0301234569", "Asia/Ho_Chi_Minh", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "https://pkdlsg.vn" }
                });

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "department_id", "created_at", "department_code", "department_name", "description", "floor", "head_user_id", "org_id", "parent_department_id", "phone_extension", "room_numbers", "status", "updated_at" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TM", "Khoa Tim mach", "Khoa Tim mach chuyen sau chan doan va dieu tri benh tim", "3", null, new Guid("11111111-1111-1111-1111-111111111101"), null, "3001", "301-308", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222202"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CC", "Phong cap cuu", "Tiep nhan benh nhan cap cuu 24/7", "1", null, new Guid("11111111-1111-1111-1111-111111111102"), null, "1101", "101-106", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222203"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "NHI", "Khoa Nhi", "Khoa Nhi tong hop va cham soc tre em", "2", null, new Guid("11111111-1111-1111-1111-111111111102"), null, "2001", "201-208", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222204"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "DUOC", "Khoa Duoc", "Cap phat thuoc va tu van duoc lam sang", "1", null, new Guid("11111111-1111-1111-1111-111111111101"), null, "1002", "101-104", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222205"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "TN", "Quay tiep nhan", "Tiep nhan, dang ky va phan luong benh nhan", "1", null, new Guid("11111111-1111-1111-1111-111111111101"), null, "1001", "L1-01", 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "memberships",
                columns: new[] { "membership_id", "created_at", "department_id", "employee_id", "end_date", "job_title", "license_number", "notes", "org_id", "org_permissions", "qualifications", "specialty", "start_date", "status", "updated_at", "user_id" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333301"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "EMP-ADM-001", null, "System Admin", null, "Tai khoan quan tri he thong", new Guid("11111111-1111-1111-1111-111111111101"), "[\"ALL\"]", null, null, new DateOnly(2024, 1, 1), 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("33333333-3333-3333-3333-333333333305"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Patient", null, "Benh nhan dang ky tai phong kham", new Guid("11111111-1111-1111-1111-111111111103"), "[\"READ_OWN_RECORDS\"]", null, null, new DateOnly(2024, 1, 15), 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("33333333-3333-3333-3333-333333333302"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222201"), "EMP-DOC-001", null, "Bac si chuyen khoa Tim mach", "VN-DOC-001", "Bac si chinh Khoa Tim mach", new Guid("11111111-1111-1111-1111-111111111101"), "[\"VIEW_PATIENTS\",\"CREATE_RECORDS\"]", "[\"Dai hoc Y Duoc TP.HCM\",\"Thac si Tim mach\"]", "Tim mach", new DateOnly(2024, 1, 10), 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                    { new Guid("33333333-3333-3333-3333-333333333303"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222204"), "EMP-STF-001", null, "Duoc si lam sang", "DS-001", "Phu trach cap phat thuoc", new Guid("11111111-1111-1111-1111-111111111101"), "[\"VIEW_PRESCRIPTIONS\",\"DISPENSE_DRUGS\"]", "[\"Dai hoc Duoc\",\"Chung chi Duoc lam sang\"]", "Duoc lam sang", new DateOnly(2024, 1, 12), 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("33333333-3333-3333-3333-333333333304"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222203"), "EMP-STF-002", null, "Dieu duong Nhi khoa", "DD-001", "Dieu duong khoa Nhi", new Guid("11111111-1111-1111-1111-111111111102"), "[\"VIEW_PATIENTS\",\"UPDATE_VITALS\"]", "[\"Cu nhan Dieu duong\",\"Chung chi Nhi khoa\"]", "Dieu duong Nhi", new DateOnly(2024, 2, 1), 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { new Guid("33333333-3333-3333-3333-333333333306"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-2222-2222-2222-222222222205"), "EMP-STF-003", null, "Nhan vien tiep nhan", "LT-001", "Le tan khu tiep nhan", new Guid("11111111-1111-1111-1111-111111111101"), "[\"VIEW_PATIENTS\",\"CREATE_APPOINTMENTS\"]", "[\"Trung cap Y\",\"Chung chi tiep nhan benh nhan\"]", "Tiep nhan", new DateOnly(2024, 1, 20), 0, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "department_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"));

            migrationBuilder.DeleteData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"));

            migrationBuilder.DeleteData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"));

            migrationBuilder.DeleteData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333303"));

            migrationBuilder.DeleteData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333304"));

            migrationBuilder.DeleteData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333305"));

            migrationBuilder.DeleteData(
                table: "memberships",
                keyColumn: "membership_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333306"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "department_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "department_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "department_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"));

            migrationBuilder.DeleteData(
                table: "departments",
                keyColumn: "department_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222205"));

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "org_id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "org_id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.DeleteData(
                table: "organizations",
                keyColumn: "org_id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));
        }
    }
}
