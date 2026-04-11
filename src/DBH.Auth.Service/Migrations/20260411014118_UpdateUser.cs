using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("448c21d1-e873-4305-a774-1b79899522ab"), null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("336118ad-a8a1-487b-a2f6-8de758cc2e36"), null, new DateOnly(1990, 1, 1), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("5789dac8-21de-495c-906a-e1471eadbf22"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" },
                    { new Guid("9f7f0e51-7c56-4c62-895f-9d7f18a3d620"), "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" },
                    { new Guid("e882ccfb-c47f-44b0-9c60-73a36406a7b2"), null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" }
                });

            migrationBuilder.InsertData(
                table: "user_credentials",
                columns: new[] { "credential_id", "created_at", "credential_value", "provider", "user_id" },
                values: new object[,]
                {
                    { new Guid("1b30a28b-2243-4745-a2ea-92c70eed2e3c"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OIsTY2UIcfQJ0xc1dL2+sSK9K+muweLhpP+vP5Zckgs1urby4VkC5HxJUo0b7KsI0fm9H3CJ3QGG1CZdKPmBAPpHZZrdRYsFaFHlVRI2S8weVTjkVw5XDUlSv94XKVRNTWSmn0Hlrs7PSAlwAEDA74ywgPbMARMBlW/aIcFLzMEZi7mGA6yZyDVEDfV4Rz5il5CRpdfUN7c811xxTDE+1274Cti5KdIDtvZmbon7M4OQrTmvFcC2oTCoyyLYevw/vxaUY1qYZYyCBYcxPpI9kh3wYBxJBWlYMn37DBzUXENVbu7z3usMnJPpFf0Q9vNjYoEjEGEvM+pf2yj9+Yg0EoJymro06moRWZcF2pLNkHQpZVQKh4Shzze9Qv7e1Th0cufantRrnvTPzF6UEywaxkJOvmX4bqteyNfuOczxEOSofcgAFUc76S4fwTmQ5YYCdiHV/ktXrCwhdoXs/tFnAcJI8iQBviR1i0gom+TAWd0w3G7vn1GIWCVAIlCJ7hy54Fpm37Q37Pe25oo6S2MAVL00/0kW4Tq4vG5ExXDMzO1UBICygjF/zk8yKGR8jOaIjNZzi63+EFUsZ0UJi/Gme2gCAaQl0t4w+mCT66mxO/GFsofpFydFa0kkc3mcnTIqYcgSghtXxmutNWPQEIhle6Mzz1Zr7C8VqfLa3bPzyvFOd5iO4/1NivqGFNRFvXCPLuWPCYdSQPXVe4cy8n1WcbkTL8MTyHI5VsGjNAKTAP1+1dp392K+JnXITLA/iN5z37+2JfSe8b7FPWH2Cz0/8MpPyB8Sl4mVI+cwmvnUuZ9JW8OJGkSTfhUOaukFFaRS7+KryLc+GrsKalbrzaQMD1CNGd095zOhw5K+JVWazGOOQ/uX810jRhkVRfDUpjfH/OiP9952sUbc3e0VkbLhClh0UXZXFdSwSIZgsVkykiDIP08LuG+92PNoTy9KV+FjfHR3Jr3A27bZgvG31IUgbPo6LCeNCss73xk1FJlHCZFu9VEQfXFnEQ/ZNfyphf2D5jPmLb9NpMsOd+VjXa0grTqIewY3O51E3zk0TeMZy9kvU4uY+Wvs+7NVIVnz8qJjNuf2hqp3RNzpiIHvX+kamtdBsYahX/ZgcrvZ3EhPnE4EzMgWN5mdhhFz5BAmyXw0KVEqnW2R6cPbG42D679mUpEMY+UmKObPdDWAN3iUzofc/f9BopscA0rmDTG1G534WNA8fVxM0q8UQxNbMZQbiKDGczBkDD/Bw/R/nhEsVCZwtLAe/Y0eHvqSFPOv9AO5TLf/zX/fg5tiEdeE+hpRl2PrUMhb61hRU8MGeK+v0g/FeBEWM5S/fmb8uNErjCAQG6sduhwQ8KRtNSpn8Pt6QxQ5MHE4+QWtjSymGHbYJ1r8AJ+Wi17fxLpqJMx0EMJX1yXYQ+GvFwI9SvHVtKsyIGjWWB0BD0mreYimPqgV8T9EugI0wLfOHdQGqwnPGLx3UrF6yIw7Ubj4UT+DV+Eoosp9rVFNh7JEzTaExndVIfJqhphLea2v4u7WltqNR81JerdMhrWu547/C/p1bbQV9q42KiyGtboAT571lT8CI5Z3iH2XR+ya4e+KoIcJ23FSC8wVwH0Wej1lo5Sm5/IFs6ahcFn/jb2HbQmIn9EpNqNCHcyIdiq6ZaML1jvpAWvbeTsn4fWag6sekao0iB7tewR7RuoFFGy0F/4GC08d3QgSBbt21qgdlij8UGhcrM21KIvpfbt85WhwfOCttHpQ7coacXn86df59zrbWdCzl9076NKYrHtzKYcur8u6Hbio2dj8YdJ3cSghXSvE2fz6SN4SstmExN98Q85Pt4zzUGueBD2an3mZzEx/3pb9RebzDi3goBfUzaj6SSc6TXwc8lhRzc+NLDoVgMlLY28MBXh6gfuwI0D+ZIq+114uxyB9I+KGYMHiCy2tyRtbiaPkvX0RAEZZSS+pyDT+icQiVBVfUYvZrQvljCUDHVKwqY6GODRFu+IgYhII4cdF5w1xiLGqic5hTkD4HuGF+WbPJtQHyaxMSRwLZZgDCkJRMQgIaDRydeRLk+YFB2wK4fyGuIkhUBSws0kCSC+1RIngMBy25IOz0SmCrPx/eJbwBa//YXBwiwHkeKDsi9j5q4WEt7PJGNsoJ4Y91WFQ24qLt7eEbHHCOO3CsnD0WOTegM3DHIEgTxQIDYcIRuHBQuxxRQ==", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { new Guid("5919a63b-24be-4240-982c-1fc1596cb279"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "CFxMsRSV7AASf61bdTBWLzxT4sxxvKg6sUSjnZR0hIOiPuCI0qbqZunx0T/usmjbpHmyHyqaDBG1elPURCfW5piKJS02OWtg/RiLEINEvjXCJi0UDUbvLwMUBcQTFcMari8iL01x6vPZNwhQWzs2orlh5OmMXx6QKpl0U7kVBn0kW3HU8pluubmqXvGn2JmB2IPN7cYTtYeVyBJAcDJ1LijL/ReeNzfG6ZHfFjz8P1QfbEG2hiFfRS8ChkFn0Pg2YcgTkw10AewvH2dYsI7nUaFMqCu2nD7T8FvGidZcXB/F4Kuss8hV1gHxvIS1qVAKuM5MVkwAqYwK3+OWo31qWLStaRuUaBUPG2+X+XaRumQoP+OyDoYrqQo8I1hr1HUuAoBOhRTqE2xzOfsQKNFNORhjONvGbE5Z3MRQnr06+kPmV9kin+bHnLwptUcLceGbPdgHjacCFUMRs+meSftsuPu/yYe4TcRAV6SQGlZHqk5ez43E3b7P0qYHzq4b9TynqIzfcXowNSHNFlaFD0RGAtem4uP+pjBUhVpVH4GIIcsvLUmq3/VyzMzztTYp+2Gj4a/eYySi5eKRdD1W3wPoI4wkHpL2lW2v8Y1jnqXAorMp2pkH72ptwY0oSalvmiSw40tiZdmTIs19wuWXPpzeyXV6JIT3qbmO1PnSvBIoD5aBDuZxq3kx8y22ckDW3QTSkzGsaU8Rg48dje3RAJm9zkFvQ+t6IpW3clmjcuDCimtMLCn6qDGkyAVGXlZlMX2bpJHtNCudwDCmnx1WVA9hCIOwqwXcqm2kJkAAlRxenpBBz4YhGPlHtKzWJyxNCbpUzr1ZSzN9x4i70AY+EN4QtkGgjmOCbm5Tad5UnO5INsFCSiL6FSIwdgQmQo8jgPhKMnrbwCyaWZxl1qMCQZ5kxWWixhYE6KjNqXc8Xw2C4EJzRPjRims251dxpg9e2ej4VNxOgvMlma8SVm0aRPdJuhbj/myeuGHCOUjmMhF5CvkncHzC5fElKv43jz7i1OCy6kr2bwwn0PkkB8iq6LNd9wbC76rdbKCLh2hY2nbynA/xi1rH11PGlHCDQQU5Dn3td14gEpuplqgFqI4w4MzxYnamBI/u01nYyDtspVEWFX8w80Z2Xbvmx+vVsaaENav5JOhQlLpypBg3GU/+swlye3/JSrpvoOA2sKxJFwVDaqbHyEqozC+dWXU/t1HAmRayeeG35vNwgAUJkiKQIHSpe8dgy4PvJqU41wOyuySuCRJBkdIqpT+EoN51a1fZzyeAqFothi98C2U2ecRKYRexZDmqjcSHHtD71fmsOnCtXmbJpGZwYTCRNf90L4rvj1RPyWs5S/3O0dapo5EOz+wmjubiwKrRaZD7ft9FxvNRpaMbUyMbpU8Y8FdESSewP1xDhPeCDI0P4MfhrG1G+L4qRWNSaDGdqfUxZWW6PcR/RdLCtpwMpumj5g5MTaUEdRLZPdrFzjbk9ZOP+AmxvPSdTwiQeTcf7H+6mypSlI+lQ0jGENUVSJWLGuaATSZnskuXeBWcT4u3Alc01tgPnrFHjUPyqI0V0+4UoeW9MtpmB0NRR22zo+NH752Ze2RO9ok/efB155DIKy5k4jC7mik7LQizwWH/bljXNeqBu7rAmh71ljA26e+Tjd/kRBCQlxbw9+w/rE7MlTL+jWFJnkDK/FHhNCRn2Il65vq9fJtNVYifL7olzHUA8T7v1vNKh+E04NIFyo71FdPfFgzz1GffaNtUKk+ZWWujfOVmxD3xqkhAUkuygDjvR/UFJKSg6eLAgf/yJq4O9At20D27OD6xPjhsSht2rOBTgJ/NlpY4sOJmW/i3zrOUi7JB+U5Oko7lIw2m7SFPOhNhqTw/UBUFdereT/rDRrO4xqaNaCQ1/F+K9YqyCd7E9bs4mqNU/8xL42DbPvaiThU/W7vL0Z+r05dCsB1feY34Aq12nbDFGYqvtHUkpHn5A7EbNixJYBDlLa76XzcDzfCZETmNNd7VRgLsoSd7y3sfZdoAt9F1gj7oxLFTCOBkJieSk+yVnjabkhomkBj47ztzZ+JP00rP4Q2iwhQP6Qp78b96WI9t5T1a8WezpQIkI4M8YE7YtA0mwAOdaqJSPr8GshdEY8HTOMF3Dh05Lz3cAHISYR39pBKUjK3Hz2Qjk8v86lCEpKKdY19mYc+rMafqBRhY2IXi9g==", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("7e740404-5c03-405d-9254-60e11699a633"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("c527a0e2-6375-4a23-a08c-dbb765065d81"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("e4e4be39-a542-4e37-84b2-7e8bab033908"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                    { new Guid("ed9060e0-9fe0-4839-be8d-88eca956f5a5"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "created_by", "password", "public_key", "updated_at", "updated_by" },
                values: new object[] { null, "$2a$11$ujWUeJZ4pr6cnHWlGvUPMuwwWVA5Y9uBpRfzHXnBf9paIseQC61mC", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzZDG6gSrB+jsOtEA3Nxf0/yJ309EapC37+KhHDEfrMYz58HXgC6lJ3sR3eiG7K0YCha8UHMrdgDGtFmdTt6F8ShZlDanfpAkkWdA5iH+HLUPPwmps7NvXW8YbdH0CuZRkEcuhph93jILc5SNoS5dtlzmRpgdp9sJ+OgE8fO6VzyjGT/vwwmZEpCgUHLjScylaMnMk3esjOo8L4CAB3o5MZdUybMTyLThJt7lKXL8I+epiQSa+EmtP1wy8s0YInCOXPb2FoMDkQc3ZKEdi0xuWvELNkIC6NtsmzoJHker04kwMWUjwfxHajWt2Hr1BXWKNtYgOOlBkf/EtOMlbcr8dQIDAQAB", null, null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "created_by", "password", "public_key", "updated_at", "updated_by" },
                values: new object[] { null, "$2a$11$rCZlBK7kEDtsJWntRlAhDO7tMKbZ1ZY6sbN6.7R10HzWyIV8uNIBe", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAyWvWHIbG/CZDjEnCch9RYNU7dAOv/hKxh/AaSZinie/n1ksEAEJ3NikaTH79x7Q78dB89YRA+2128UkjpAcdhXN3wHLO2nrjMvpaeis1713lGpRkVdPHzs80RmfoZsOKXRiB0bAxiYWX4Qsy8HJa+fS5+O5u1rQUru5J1G7dtkjhUGFRVM0aauFQXxGQMpugz3FIbk79u7aym9XAnQ1ZhZdSIBEll651v3peF41WEfGIDgcnMe90FROfrPA6k91ykD7cMSfsUslzG9bjysrUPUbHWdwoUdBCTLsmjqqbeU9nNqFX0ggX718YXViZzy1D0TNuz9vmNa5ko9yhq3eZzQIDAQAB", null, null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "created_by", "password", "public_key", "updated_at", "updated_by" },
                values: new object[] { null, "$2a$11$ZzOnuEug3XZOPvT9i65MtewRHvrm9rxrU8qRMcrjp/tugBdfzBTdq", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwHYdMIRZedpgCHLuuJTge75F95PT3dGQSzFEpsrK+LrBl782kJihBNvZ2q+9sQYpEd8fMqWYKUnhdyCtfY0Eilux8/tnS4CIRONBbo6Khqhxgk4W+2lKErIGc9e4HRmV8XFDv8mVw0o/VkcoG+tSeR/vQbihcVwK66bN5mFPQ9YT1WIFkvH2jXsXZ4cL0RUKeHC6vky9x+8t5ng9sisGzHbl4WhH3YfLNOlLSqvANXI54P+t/NgMsJLFr/6qt77GrKA9Ow0EkKPa61qX6ZOnmN2+i6g5p7MV5AXF4pl3TKZSPd/VzGj5JOGOrym63IQtoSk9yjzsbbZmpxu2RMwgeQIDAQAB", null, null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "created_by", "password", "public_key", "updated_at", "updated_by" },
                values: new object[] { null, "$2a$11$tJdZgsmSuB.Kl2CSzUS/Ze3iqoA/ghj4A3.1bOF1A58MyJx7wi9n.", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwKMtWVVhVPjjbU6xkSzcJtC1gTP+pWM3aMy/MHKE3DRVM2qtaIF0gI5fyRE2ZHcxN/AN7eLYsx/QhSQnzl3gTxfdW/WvLdoXUGeEin3KmAga/JjGMpz9F/cS0lJEZVeHZlQK3Obh/ZA8qOIO7rtdwCdrVnEAu5tK9+bFFdvdmjswkQpThmQRsXxC2+CCfWl+mt4iSD1He2P234TptCA1zciG9dApSnSIQIOWguhefLkR4te/kQcxhMj6oKFKstJG1wGkeEjmTLMRdRtAjxSn7PBoC+YwCReOT98OitorOnoOKIi2BWrNEBS9H106yMNgXGZlA4A4duC7dY6VsyLdEQIDAQAB", null, null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "created_by", "password", "public_key", "updated_at", "updated_by" },
                values: new object[] { null, "$2a$11$QSXoyMqDoAmMMPMkSyA9D.j7WXo5zhnLmZR/8KGk98hQsxEEV57YO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3EhgacxBOBe3cl5906a4BYSBICvg7D4SVwMsGzo4zqezTiJs4LLzYz7PquL1XtDQqSSpZrKGN4ohLIDb75s/Kz2XVlZ9FVdPjiFo4pnHYVEnZ7CaLZUfMij7kWIHUkXtTrTtcDYT7YopAwzvnaVE0GaItGejOeF5rkvH4xRuwcPMoC6lTPx2nZtkKaHKphwIOGmQ8lGY7aT39sJH34BeDIZZJnEAS3j+3+lzpqAWdOE/xgElO0dl18rMvGYS8oukNfmipLyVnyN/++8YYsJxD+Kh/9cDs5jDVli4zqEYXn25odFlR3jTmG0agYzuRUV0+pZ98dm+81DRlJmnq4dZNQIDAQAB", null, null });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "created_by", "password", "public_key", "updated_at", "updated_by" },
                values: new object[] { null, "$2a$11$KueAq6Drm7lda3n6CUkUB.31iK04mAKEaKYh4vl2I6ITYxW39utni", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwAP5DAdnvpM2P6PUVHQs6yPIDqG6WGoYBImWj7wGIk8a8AIbFEurhl9tsvZaUJ/ZZ/rAu+QpHT3MwsJvFoDtRs9A5rMwsu3IZH74JSLaJmE2nigl1f8fauxYRKqZKgF2jZwpUfRrwt3Hx05tLOJDU+sXpEXh6uXkd0FmEWRFSUKYnLCFPhONRHP7Ije6MHNRCXbc0QrGUjgB2c4EAQLNnplwQuO1r+/uoJT88RzKbk8H/ywnruMUtQWLVLa4Vg6PxuMKK5Rah3ao/FzYpj/2Hy6tFZjtqRodEKPzfGFyFxIJsEZlMebOLWEJZFZfWhweuTryZmzem7IgAjZ+CMXhvQIDAQAB", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("448c21d1-e873-4305-a774-1b79899522ab"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("336118ad-a8a1-487b-a2f6-8de758cc2e36"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("5789dac8-21de-495c-906a-e1471eadbf22"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("9f7f0e51-7c56-4c62-895f-9d7f18a3d620"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("e882ccfb-c47f-44b0-9c60-73a36406a7b2"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("1b30a28b-2243-4745-a2ea-92c70eed2e3c"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("5919a63b-24be-4240-982c-1fc1596cb279"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("7e740404-5c03-405d-9254-60e11699a633"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("c527a0e2-6375-4a23-a08c-dbb765065d81"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("e4e4be39-a542-4e37-84b2-7e8bab033908"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("ed9060e0-9fe0-4839-be8d-88eca956f5a5"));

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "users");

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
                    { new Guid("718cb316-1be0-4c8f-902a-865e65140f02"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "7lR7ly7qJSIIzUqrIJqQxhyKoH3s6DR9y5eK/pTvkO4MQMWQTHIhgc85U7uOHZWeQx3e5Vpne5dzm2EQtJUT8ASmQZfSIjr/LlM7ykVh4jpM+N+FaOMweUOSDwqjVjCm9QH46wG5BowMwV3o2kh1WuzH3TeTTw6httKEgw+LpuLHMGZ7jcQc7TUvy+d7ZvrgrGRlgDoJz3eECgPpmPZbprW3HSFPNHOjx939BTeCnzB3okAUAWNzKVYYaXYI0ZiYNX02E+47uKkJno7f4Q4ypY3nX2pvwLAeELiqO/k5zfSElymO/1HLUXBI2WHiedXw9BLE+P1smTaJx0FciNnip12rgmCpzzulYoV2TdIlajY2bSRpfhnE+/htjmcLtgAKIw4xurmY/6M2g3KsIkp5smPMVRH42C7BYgWVEDEtH6Iialj8SfnuVKonbFvjfsMmk4a4nAoyv+LR7KDP9aOcjRql1Cvo4qOmvJxfRdhVA9vUMUuRVqv1qSY+4p9UDbMg7OjBuJu01nzW+aNFMdtRrWZOHWwL76NJS+sA7TR+L7WS1rpfOh0AyRqz7pfytz/ZOd4cBW19jywY1JaXNlkIxQt8eh5aiyKXPWwlHbe7Z7emfs/gmPCG2oFLOliIFI3DroF3Qnr3fECcuoehMPAB6Bkvbb69gqOb5j5lMHjFnsfHMLb++aHliJ8g8KI5enJzmkN5VOf8RTmVplQ/hlO7b6g7o6wcvmZSXtfQNRtmmyuEvPacjxMo9YWDaJSCXByAo7Nc44rKwhswBY5B2aND1hBVJU0s3W39AT9pPjdqMirhQCoSlc5BL2QKcDw1cqzNX0VyyTBaHY/fl44ZUBPpiU+06zvKzm+Wcz5qKhWTUYtZXV8PvhFH/CSTWpafd5e6D/3oW+geSexo8xg/1cCmG6ND32Elye5VIov/dzWZfr+EsMAthgvLDK3Wrt7mzUdETh2TfWGqFqrBUicOXch+uvAOficPgYhQxc5Hw6NyUqyoCkXCmAKGfhEeAww8AxW+cSnNXWz9pAzsMDORNByqEpbCZNSjwzJswZTe+CLpOtStiTBQ1V3LCAbRr/EI7TTo7OkZzmiaVhjr/1TYdtIuE783Rlb4NoGTMllqlwkNrfvuNl5vMovluCdh8NUqynNBplaiNheF4nXLK/oKFySBacdv1Sm9QoeTlqB10D+/uUGmcBJu/Z0v55yF5ygOZe5sSjUGuwa1uFRP8z5QKCA5csXJ/bZqGwNoxJCxO3FBg2s2ovusplkXVODRC+y7uxQlncxbsmVfN2E+K4BV8AR9uLDmmabAABuBAYfZTfW4DH9Bwi+U+4ncj+//Zm2CnpIkZcEtqQUuoO9MGfQAeGSbJ3mnt01Tq0MQ82c8wq2bggzdUzSKf5K+c5twurc/xqPgt1M2Z/06eFlA+/li4P+NxgUJzNBxyFWbfsjwclsG21tRm2xvL4G7sc+87wBh5jVJxdVGR+ZQaMJ1ECeY8aLNiJfGSGj8aVM7jVk3Vs+0A7ty9G8xvLfnbs2CRXcRCqjRvwhrzHPsjh91IQ2eqB/hJUpmLLM6+ntUdeHFkf7MmFpwGAsn3ld3TPt6Ph4Ia3g6FGsmZlD8NHbjB9qaSyC/3kP7zpSmSA0bbtvIzlzjI0A1X37B6MuZyPLQID+wezhRPbB4I2cXqkyaC3OoqadU30p59F10QbYv0bYuBPjapykASwTh18iAV0EYGtT3xQnXpj63KEQJDbgqprC8A9+dQuOuriYBG5xhEilxLj7ZQrrLcG050R5QdrL2G9DbYgOAzJoAkVX7s6QSpHFP4EogLn0Zzr1D4z7SeQU/35qCLIXrUr7RLaaBvyozV1CN9d+LGK1Sq/A10PqojjZFzLo0C37NVA+oW9KSQqzhvtCiLHA3YgLucl4CUSBU09L6cH7vDBikbVhmu1iJY6w1Q+Zop9v6um5xCh1k9/9KPmFHDT7VQC2RBxfLvVVrg9zue7AP8T7hotHHbugMsUWyWs/2CPC/8Y3nzJohjHuC3TZmq2UauULGfkQCs5u1HeO8AIL+s160rOIXUlv9cD4YTQe91PuFvMcDCJ/HvsXeE0g5Kv3DQTdoSlaTO8/hXmc8ooC1JKqKd5nBzwsfAp8MxSKfELKCsT29f9ewtZlU1JSCPe259Nn72/E8BrYfkwNhYrlvjZr3zwWgnzZDCLsipCGLeA==", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
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
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$.zuKnhtZTPESHF/wZBasKOdUEDW1tYgAwwGSuJeCDBvyhFCJQ2.n6", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAt5I94nRqMsvZ7K6yKmVxXfGxZds87Txg1EcR0/ME5ueJzmJGiW10CyPe1JbupviHgCrn/vs0mlAxypdfdA5ajSr4ZYHK7M5ycd5pTxX9L2uaA+E/N6t1lM37CwedJjcNswWWw1rDjUM7gXf/wrv1Kw6FkToD1TRGIYljMFf7aoAmJVQOPo319PvAAvaN8IZhmKHe0C6KHcMJKRaAbQbJMnDj5xQRlrO49bzM8joB87KrpePyEWvsiRElE8WBz7EJeU23QsZQrFGaXI6sHhhuSIX58nsnIhCZqK3v3i18dPt12rWh4BrzzhsgiqhvoL3YszsoJbbwgyjbXiaswRuR2QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$ytsIwIRlkwgrEMjb7xksh.JO.RYY0SynoHj4EAELXV3.kOESnCMqO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0F2gUT66985lIT1WWZ6C7UOYSuGfaFFVyFfgwL3xFQwoswzbD6jxIvnUG8WcLqGIAkG889+CAcuWJzot1YTc4IFI1nGIwNIAwe82eXtCfTpP9jxoPCHx/FgOnui8JTB957Js60ZMLXRr07yRlEBqsOEBUcTg/3+3S3vvyhcxZ0P2EGhGMkEUkIC65UVV0zv2YeI9V9/LlgwEMfDRizykUn1foUM7/mwe1HboATmpCHskX0Nby39hfN/lkuYpXZxgMKGBnwd5fzBVZKYCSShFo+j+NsgEjdAqy0mKwIZzuaEGPGcg5qRAHm/zKAjeVUiuQ19JpWMAgpGDCwHq4eYaUQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$4co8sIu5Ymi18Bzlb3zwnucpJCwsPfCzu6.OGfy.TpQurePjJspCy", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA5dPlSPwircBsfXdhLCB6z6t2xYF+ZTrd7qBqXNXcV3MFGH8O16HbcRxYSarLlBzhDiOoaDpNQyfOpDtR/IlQqbBZRoGfY720pHvAYwnpilCurcQmIbEzdJqQ1L+/yiGQ/Bdu/0ehL2Pey8uIpaW7NF24rAePIkUw40UYfrTeWthAaJbiRhwxOpXzAZ9eP1V3G9LMUMuGclThDb1CehTaWm+NTCltfewFmTkVFgO+GwgaEjBkFuBP2NUD37gCi9rSKuM+RdBX0LjoZIynye1yPWInZbPkapvpZl84VC6ZPhstq7zHgZOlUp8lIXpsX/Nud1XdYPbZNWnvOHoaKsIdfQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$93P3hVhNU8LD7Yl1Rc7zz.b46bzqVbgE4S99w.EKYl2fUQbeVecRi", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAucv1BflNTJqyXghh3b58MhHHFINe3Hv4/jSzaaG2cVLoa8dVfdqw+dZw25fdC3Llb88bysA6PVSYy80BUirgApL8KlFgzPjc1OmtezA1eTV+6LQWaqIC9TY/LoMnXlvp5AphAdlDYFHZOEWQmAO0gJuK25WwGOgm35EKAZcfDUY0kXCIAZgJl0UrkB1nfm/y8DU+Dmyjfzq2GBiQV+5I+ZMR82DIC75QsjWzySchtG3QY3SwxyfrWohpi4FBc3td8bFzwpY7FPlc1XWQxvkvIBcDdvY4+SG8NLBPPebswggA3X7nP+VIjKnAM6U7JTk/xSeibTxKShPvTnmJWvRn6QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$zbRSQ8MZkPLuIrzNv.JpDuI5qmWgdeg80rzFtXq1O3Bp2QtRa2F3S", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqdAnNqIVv8UyLCFGELibHstq8gDm3Mhrb13Vd57Zh/LWrzWFQ0xdDE9OeOVmSxzIe8z2aBf+hJjK0liEBWyLD7SCAWrgxOqrqkY4aYZHz5lgX7x3aKnU1dIptrQ8pDz4jxJFZe1yo9yq6yC3RWQOWyXE2KI8rv0vRAF6yNWI+eyV9sEKK5N+zMDjfjh0Mh9GwRI6R2DkGA2DgGA9OjVHSp+HKBxN/vNap0zMAasakctXHP+8/gtZF3GI/9/1xeWl2ei0X9YCyDNiwu4OPQ13t7ux0kTcaaeAc7ELgZMTSyDPT70XwcrON5R4T5XPb4QZZNVC0ZpVORwuPHnQRx042QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$5pktLJCzWzyGgHFjbIoIKe9ai894atpYDkZJLq5lmRDICU/QIzbiO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAonZcc5xfn5czSCwAvM00tSaVzUR754JgZC28xfimgvsSeV6uGtrxugVaX4QTszCzw2sMXddAabf1A0m0a2BhFVZCLTsKwcJlm2bdUQriPQ1IPMrUObFAIoNd+8FQoxQzdefgDxo8OH2O2flbVv4zbsg4sVHcg6D/0mBkf2npaMTwXA+8ZweID+RbaIDPEjsTLo4rIPZrVcm7BwEqucziGYlIIJR9eVJB+z9zTT3yRD1rjE9h1CoReklRlKjzxT7HJ48RcdZ2ILHGd4Ljaqznj1lQ77K3MODLxlhzLj3EhMmDdA+qP+EFPEukeDDWSdZbH5ts4mPII4kyNLCarHD59QIDAQAB" });
        }
    }
}
