using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserDepartmentIdToOrganizationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("7aff0606-4b5b-4250-a4c1-328a247b7a44"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("9ffadc0b-e1ff-4ab2-aad6-7bf9cb4210cf"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("605ae037-284a-499f-8e4c-728839d28d88"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("778ba4f1-7150-41ae-b1d2-cd91a4c6e51f"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("adfec156-fa70-4546-b2ce-6900b5659ed3"));

            migrationBuilder.RenameColumn(
                name: "department_id",
                table: "users",
                newName: "organization_id");

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("bae7fc92-70d8-44de-a290-872b162b4149"), null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("5cfd42fd-cf29-43a6-9279-413574bd8a98"), null, new DateOnly(1990, 1, 1), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("8520fcbe-ea9e-427f-be8a-fcb220a8269d"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" },
                    { new Guid("d322dbb4-a935-4a47-9622-d02f899aca5d"), null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" },
                    { new Guid("d3b17b77-f3de-4142-81ac-7dd9bdbdce96"), "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "password",
                value: "$2a$11$FNTZE2XDBWXAft918MWrxetlt8iwbi7hl9u.JEB3kfLIaYRZOY.RK");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "password",
                value: "$2a$11$aq0RZY5PdCDe/r9rOxCD6.IpCYmXHYt2ilyBxXjGeRRJxS9ecERjq");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "password",
                value: "$2a$11$IMsXglcEkKU6tZxL21GG1eemv8M2QLkFgOzPslXsJ1dH0..F0DL8W");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                column: "password",
                value: "$2a$11$kNu.A6.aIA/.Df8HfvOs5evR/HeDUN5kHyE3Ahu0mtehSwI.ZehP2");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                column: "password",
                value: "$2a$11$L5bsYzPqNmHizTEGjGB23.8ayIYa9HEaa4ZOLK8OLu.yoNj1LFG5K");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                column: "password",
                value: "$2a$11$DG21Wo6Pe33FF41e2hB9aezNXjuUsAzOYTzJXAK6xAYxhApgnQd.S");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("bae7fc92-70d8-44de-a290-872b162b4149"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("5cfd42fd-cf29-43a6-9279-413574bd8a98"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("8520fcbe-ea9e-427f-be8a-fcb220a8269d"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("d322dbb4-a935-4a47-9622-d02f899aca5d"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("d3b17b77-f3de-4142-81ac-7dd9bdbdce96"));

            migrationBuilder.RenameColumn(
                name: "organization_id",
                table: "users",
                newName: "department_id");

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("7aff0606-4b5b-4250-a4c1-328a247b7a44"), null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("9ffadc0b-e1ff-4ab2-aad6-7bf9cb4210cf"), null, new DateOnly(1990, 1, 1), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("605ae037-284a-499f-8e4c-728839d28d88"), null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" },
                    { new Guid("778ba4f1-7150-41ae-b1d2-cd91a4c6e51f"), "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" },
                    { new Guid("adfec156-fa70-4546-b2ce-6900b5659ed3"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "password",
                value: "$2a$11$ocTsCaCPgh.8rqgEIPOPCed2b5ZrCnE.g2moUq4dVfrgTN6AJgnRu");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "password",
                value: "$2a$11$lHwzf6S3mdiZTZEaOOdmuuIKjE0CtrkhzY0qaIr6Uj2cUju/SNdbK");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "password",
                value: "$2a$11$d73zj94eE3YeHBhlDYZ.FeYfwpRUbKlgHf/d59a9F.SuCe6.zI2a.");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                column: "password",
                value: "$2a$11$NIxM7qpA5wNMCX04lLZV5uExInp93V3xIBHHws4z5x3sl3nfNSG9O");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                column: "password",
                value: "$2a$11$S3AblAwIUvZq.BIV7zNVrO0/oDzPLSsBlPy1WiNPTRv.wrXNmuSfG");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                column: "password",
                value: "$2a$11$lx8vIgcr2qLvRON1LcaHROlFVkhYi1v1uoHdG5wLf1gybfJziXkGe");
        }
    }
}
