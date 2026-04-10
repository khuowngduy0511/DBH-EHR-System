using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("2ce8c1fc-a192-4433-b9a5-461685ef6909"), null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("1b8d4450-8c66-4554-8a7f-22a49a7af66b"), null, new DateOnly(1990, 1, 1), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("bd902e5b-b639-46b4-bf16-93183eceeb36"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" },
                    { new Guid("ebbf4de9-49a4-4fc2-b9eb-efcd3be633ea"), "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" },
                    { new Guid("f50064cd-80d1-48b3-8601-6671b142b108"), null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" }
                });

            migrationBuilder.InsertData(
                table: "user_credentials",
                columns: new[] { "credential_id", "created_at", "credential_value", "provider", "user_id" },
                values: new object[,]
                {
                    { new Guid("718cb316-1be0-4c8f-902a-865e65140f02"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                    { new Guid("89c3caf6-d490-4a00-8ed7-b534dadbd023"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { new Guid("a1621d10-0a54-4683-b888-3181d91f4565"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("aa02c377-41bd-4778-a24a-73b841c6757f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                    { new Guid("cd8b2213-933e-452e-ad83-7252c96d896c"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("e7961983-6bf5-4037-8574-24f44bc59435"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { "100 Nguyen Du, Quan 1, TP.HCM", new DateTime(1985, 6, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Male", "11111111-1111-1111-1111-111111111101", "$2a$11$.zuKnhtZTPESHF/wZBasKOdUEDW1tYgAwwGSuJeCDBvyhFCJQ2.n6", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAt5I94nRqMsvZ7K6yKmVxXfGxZds87Txg1EcR0/ME5ueJzmJGiW10CyPe1JbupviHgCrn/vs0mlAxypdfdA5ajSr4ZYHK7M5ycd5pTxX9L2uaA+E/N6t1lM37CwedJjcNswWWw1rDjUM7gXf/wrv1Kw6FkToD1TRGIYljMFf7aoAmJVQOPo319PvAAvaN8IZhmKHe0C6KHcMJKRaAbQbJMnDj5xQRlrO49bzM8joB87KrpePyEWvsiRElE8WBz7EJeU23QsZQrFGaXI6sHhhuSIX58nsnIhCZqK3v3i18dPt12rWh4BrzzhsgiqhvoL3YszsoJbbwgyjbXiaswRuR2QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { "50 Pasteur, Quan 1, TP.HCM", new DateTime(1980, 3, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Male", "11111111-1111-1111-1111-111111111101", "$2a$11$ytsIwIRlkwgrEMjb7xksh.JO.RYY0SynoHj4EAELXV3.kOESnCMqO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0F2gUT66985lIT1WWZ6C7UOYSuGfaFFVyFfgwL3xFQwoswzbD6jxIvnUG8WcLqGIAkG889+CAcuWJzot1YTc4IFI1nGIwNIAwe82eXtCfTpP9jxoPCHx/FgOnui8JTB957Js60ZMLXRr07yRlEBqsOEBUcTg/3+3S3vvyhcxZ0P2EGhGMkEUkIC65UVV0zv2YeI9V9/LlgwEMfDRizykUn1foUM7/mwe1HboATmpCHskX0Nby39hfN/lkuYpXZxgMKGBnwd5fzBVZKYCSShFo+j+NsgEjdAqy0mKwIZzuaEGPGcg5qRAHm/zKAjeVUiuQ19JpWMAgpGDCwHq4eYaUQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { "56 Dien Bien Phu, Binh Thanh", new DateTime(1991, 2, 8, 0, 0, 0, 0, DateTimeKind.Utc), "Male", "11111111-1111-1111-1111-111111111101", "$2a$11$4co8sIu5Ymi18Bzlb3zwnucpJCwsPfCzu6.OGfy.TpQurePjJspCy", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA5dPlSPwircBsfXdhLCB6z6t2xYF+ZTrd7qBqXNXcV3MFGH8O16HbcRxYSarLlBzhDiOoaDpNQyfOpDtR/IlQqbBZRoGfY720pHvAYwnpilCurcQmIbEzdJqQ1L+/yiGQ/Bdu/0ehL2Pey8uIpaW7NF24rAePIkUw40UYfrTeWthAaJbiRhwxOpXzAZ9eP1V3G9LMUMuGclThDb1CehTaWm+NTCltfewFmTkVFgO+GwgaEjBkFuBP2NUD37gCi9rSKuM+RdBX0LjoZIynye1yPWInZbPkapvpZl84VC6ZPhstq7zHgZOlUp8lIXpsX/Nud1XdYPbZNWnvOHoaKsIdfQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { "22 Le Van Sy, Quan 3, TP.HCM", new DateTime(1992, 4, 18, 0, 0, 0, 0, DateTimeKind.Utc), "Female", "11111111-1111-1111-1111-111111111102", "$2a$11$93P3hVhNU8LD7Yl1Rc7zz.b46bzqVbgE4S99w.EKYl2fUQbeVecRi", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAucv1BflNTJqyXghh3b58MhHHFINe3Hv4/jSzaaG2cVLoa8dVfdqw+dZw25fdC3Llb88bysA6PVSYy80BUirgApL8KlFgzPjc1OmtezA1eTV+6LQWaqIC9TY/LoMnXlvp5AphAdlDYFHZOEWQmAO0gJuK25WwGOgm35EKAZcfDUY0kXCIAZgJl0UrkB1nfm/y8DU+Dmyjfzq2GBiQV+5I+ZMR82DIC75QsjWzySchtG3QY3SwxyfrWohpi4FBc3td8bFzwpY7FPlc1XWQxvkvIBcDdvY4+SG8NLBPPebswggA3X7nP+VIjKnAM6U7JTk/xSeibTxKShPvTnmJWvRn6QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { "12 Le Lai, Quan 1, TP.HCM", new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Male", "11111111-1111-1111-1111-111111111103", "$2a$11$zbRSQ8MZkPLuIrzNv.JpDuI5qmWgdeg80rzFtXq1O3Bp2QtRa2F3S", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqdAnNqIVv8UyLCFGELibHstq8gDm3Mhrb13Vd57Zh/LWrzWFQ0xdDE9OeOVmSxzIe8z2aBf+hJjK0liEBWyLD7SCAWrgxOqrqkY4aYZHz5lgX7x3aKnU1dIptrQ8pDz4jxJFZe1yo9yq6yC3RWQOWyXE2KI8rv0vRAF6yNWI+eyV9sEKK5N+zMDjfjh0Mh9GwRI6R2DkGA2DgGA9OjVHSp+HKBxN/vNap0zMAasakctXHP+8/gtZF3GI/9/1xeWl2ei0X9YCyDNiwu4OPQ13t7ux0kTcaaeAc7ELgZMTSyDPT70XwcrON5R4T5XPb4QZZNVC0ZpVORwuPHnQRx042QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { "34 Pham Ngoc Thach, Quan 3, TP.HCM", new DateTime(1994, 6, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Female", "11111111-1111-1111-1111-111111111101", "$2a$11$5pktLJCzWzyGgHFjbIoIKe9ai894atpYDkZJLq5lmRDICU/QIzbiO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAonZcc5xfn5czSCwAvM00tSaVzUR754JgZC28xfimgvsSeV6uGtrxugVaX4QTszCzw2sMXddAabf1A0m0a2BhFVZCLTsKwcJlm2bdUQriPQ1IPMrUObFAIoNd+8FQoxQzdefgDxo8OH2O2flbVv4zbsg4sVHcg6D/0mBkf2npaMTwXA+8ZweID+RbaIDPEjsTLo4rIPZrVcm7BwEqucziGYlIIJR9eVJB+z9zTT3yRD1rjE9h1CoReklRlKjzxT7HJ48RcdZ2ILHGd4Ljaqznj1lQ77K3MODLxlhzLj3EhMmDdA+qP+EFPEukeDDWSdZbH5ts4mPII4kyNLCarHD59QIDAQAB" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("2ce8c1fc-a192-4433-b9a5-461685ef6909"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("1b8d4450-8c66-4554-8a7f-22a49a7af66b"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("bd902e5b-b639-46b4-bf16-93183eceeb36"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("ebbf4de9-49a4-4fc2-b9eb-efcd3be633ea"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("f50064cd-80d1-48b3-8601-6671b142b108"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("718cb316-1be0-4c8f-902a-865e65140f02"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("89c3caf6-d490-4a00-8ed7-b534dadbd023"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("a1621d10-0a54-4683-b888-3181d91f4565"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("aa02c377-41bd-4778-a24a-73b841c6757f"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("cd8b2213-933e-452e-ad83-7252c96d896c"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("e7961983-6bf5-4037-8574-24f44bc59435"));

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
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { null, null, null, null, "$2a$11$FNTZE2XDBWXAft918MWrxetlt8iwbi7hl9u.JEB3kfLIaYRZOY.RK", null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { null, null, null, null, "$2a$11$aq0RZY5PdCDe/r9rOxCD6.IpCYmXHYt2ilyBxXjGeRRJxS9ecERjq", null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { null, null, null, null, "$2a$11$IMsXglcEkKU6tZxL21GG1eemv8M2QLkFgOzPslXsJ1dH0..F0DL8W", null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { null, null, null, null, "$2a$11$kNu.A6.aIA/.Df8HfvOs5evR/HeDUN5kHyE3Ahu0mtehSwI.ZehP2", null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { null, null, null, null, "$2a$11$L5bsYzPqNmHizTEGjGB23.8ayIYa9HEaa4ZOLK8OLu.yoNj1LFG5K", null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "address", "date_of_birth", "gender", "organization_id", "password", "public_key" },
                values: new object[] { null, null, null, null, "$2a$11$DG21Wo6Pe33FF41e2hB9aezNXjuUsAzOYTzJXAK6xAYxhApgnQd.S", null });
        }
    }
}
