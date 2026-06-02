using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserOrganization2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("df7f006f-4fca-4d61-b7d3-e2941b0c8fd7"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("041bafa4-efab-46c4-8b90-72a98504234a"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("146e2c74-1bb4-44a2-8337-859181ce84e1"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("8ddfffa6-6ac9-4ea1-90dd-e6bba5926530"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("91e3b0e7-e369-450a-8625-6ee978b4a408"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("0db4d31a-a811-41e4-97e8-aba1b24cb786"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("40f530e1-4c21-4ecf-a959-ed693e843c2f"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("81f453b4-f99d-439d-b340-08791c97c5a4"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("897793fc-7f2e-4c06-a9a0-68da812d069f"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("a3538554-deef-491a-87de-29a7fad52c15"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("d44bbb4b-7436-4032-aef3-cb2d2a8d94f5"));

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), null, new DateOnly(1990, 1, 1), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "role_name" },
                values: new object[] { 7, "LabTech" });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" },
                    { new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" }
                });

            migrationBuilder.InsertData(
                table: "user_credentials",
                columns: new[] { "credential_id", "created_at", "credential_value", "provider", "user_id" },
                values: new object[,]
                {
                    { new Guid("421460a1-e379-4135-8156-818488502c08"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("5653495e-0397-44e9-9587-13f3b159d1b3"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { new Guid("56b1f409-1d30-43be-add3-1df0cda73568"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "eBCM7wCg/Iv58vpR7gBMwCzz4FXC4Ms1Z3cVmADqW35D91DsZ60dWZ5JAYaIBWAHrvuC3y9gZAOyWSoqABQ7eCS4yhpuxTssky0ZSGMuZRZiDX23OylaImPz91mQ6QjBQ1ME2VNn9Dsfrt4hc28SboVE9c2DYLYxnBDdG69amuZOhAzv3n6Eb9YI9tSj1zOjnoXDSFBsJuzk3cLXGQHKlZOfWgegM/e8CZV8csWfyDZB1sJuI0IrSY+n/bTaNNoYzXM2jipT+RP9aR/+EgF3dDU099ncXFAafjWxVGrvS1ZBb4fGyHBKoFTInCBJ1c7K4PS1VcKgI8MpM0EXmAeR7a1L/OGTIcCBoXwWk7gI9hqFq4xgOi3XdI/F8hSPEKW4bRb2GSFKOnkX6t8eo8NSQk5IhbMTOIb2iQmerDlmMa6xl5gtQwHBCroiKTKgux7mndKAg0rXt/F3WFJg+XkX063xVsRdcf/waMKcVz6sG7rM63rSq8hmEV/2iqjndWdOlsUDmzSaBSZYybu2pqEH3s0JHLaWsjwygV3hd5rawPLxEVVRSLUs22FLmo5pqwzzkenFtyKTOSDXpUbLSQA8cOZIXGHiI62e6h5Ll2UPGHvN0LooZaFtx8ND/0DHyeQvh2OUrIuC4P6KOjnB+bCRBcYD2i+vJeYukt2pWe0s9Tx7U0EMgdraMAvq4s8C8fIyukmHjujUZxKNQAPQhDbCpW8PjUAeXpPR85Vx7L9O3yoNQd8B5+Ep/FeFFURGE8KdbpDNMns7Ltui5efVLODQ3hx9yg7leqJCSExLmvseUx5dtOV0eD8ZlozmXyb61ZEK5KYsH23L7FYxgT9KqxewFWzUXnzqRADQcIoWHFn2zNjXjiHqtPcY96hQIHZeA+Rj/cas/UH1P+V3GsvWG38IBRMb0pu0VmvO82/Wa/0UF2dql3OobdSIyRaKtOQ9Xyb4ERJBKK67eRykM9gDKTEkbrQ0XfwdHNfo0+Gh0Bdzocf9yn5OEb+N9n8UJvF9t2zBxyyp1jnF7u/vmwqxcVpQRYf2Up4OBw5YXEp0H72habgg+IWy7cmIKPktDDtHR3yzMsufNb2UP39/3J5DGo5i+5NCDHf55bnLkvgNAK+t7+kdkXcKUxW8FsurF0ADS2wdFkE1mgMqQTL7NPVU+AYoT5Wv5QDQ1EgFJozBN7N/i3PKDpEWqrYbcrHzHUwRCPP14PWr6YQIKkysfIjBINQaUMCvXoQ2SegIp1h8fTmrU5pQSwqjQnjd6G7QMOZ42iHLJy8EdABMxHl5hWWXePw0B5Uz2F93VcOxKaL/tnQhDNvm4SyrEFnDy2X1GQ4YVkrNGb987C7O/zCrOIybzIab1TblLrhy/eNvMUBX41Y8FzRuqzUT8WEpB2RXUQ6B8NvRUypg0eEbyX6UJuu/wACupNeZkzBrI1aqTR38qPQuGP0dblTKPyJST7PhWUFWv2CQF0sV58XC2LiM2vAM4wuoaFD6tSrKhe/LOlS9Bb3HnV3OCdkEDtugcCDdf4aMuThQSykWMJOvHxklCOvrHw1iWwCwwxyg5ix7IjvnyQGwheBcoHhIgpSUR0OzJj4wz6FwzFXijjxG2Fdh5+Vc75eeikDNDO1xtKWweHYVDHPJO3yIPBGkMZZsuvWoDgh+cyjsvkU0NzHFq2QJyctCYxrLqBxjn0B4stiC74j3LU0NyLfRgySin1r0/0LvRsuZ2CPiVhVhussABxVZq6H7zOg+fQKK+xvcbL8LHhqP6mv8JRye6fah7P75Y1rXnGmkHqTuT39XxiLW1V1l07FXCPB7NvHxx5zrhL50ANw707SrcXnGXdumZFyTCPKlS2h6ylkFKy4ouEa2sG56E/rGfHRAACtY1dSMNDBIMuOFdTyXOApl3iOLK23Pl6qzmcy49gflLUhAoQu6wuy//useJODX/kKMlGETYBq5I4l1tiNxW4vJgOzpUJYwuuk8k7CTK2ddWIMPO0GBSb6w3ukN/ooIAN2YrVTODi4qs290kwBJ1OODjXAFmXOPJZvCgqd/0vACg9RV/tHJn0HwNIHTdwq9E0VnRRtztg201Rc66/+Fkuv1aoM/pMsHHtnIl1WPlN5jgmI4/ObtnPG9e+GNXFUM1LJvfVGBlW5//0LsIegv6muGbzCIMC7M4AdAdidOMrc6mGu1Ql4o2YmwcRQrwFB5Ww==", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                    { new Guid("b4f065a5-6c38-4bf8-92de-5690896f2b08"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "rQW5SAKXrZTfrY8j0bGmIS+dbHqonRjy9nsk+mWUxYo5b3x3y15lpaF9661xRniz2FL4184Jh0+73orNqKXrY0PK6MbexOCcKTfKKSPMkaHzpWk8oU6XgxQ7cDwwoEPRFUdRYwa2VCGn2zKbu3YCkzAZjtaf58PCoxDq2VdsSn/x7yO2iNvyLlndxSqBtxIvsDwlzzGhrXehMHg4/2neqdaiROkMSKE1O+2iXKjKeAi/7gF17Hwr5SW3r9nNXf9M+ExK2YSyRHhi4+hegHs9L6QxIcQ1RiE/eQd1COX6+Rhd+CYV93UJ7Gjqlkb86lKAklQDjFz7nnMkR7H8OF5YNG1UgFgxyx/Wnof/CEPwMmhz6HmQyv6RWXrOK+VfROBkz7SSakMCbCXx8nyKjcHglzsqHZvWr725l4qeAadnCTdTWtzuoE7eHCGEt11xzs4nygOYCBxP/ITiABA6ZDXlbvhC29EEi2JS+F/8+dO0olnqxMisSW+BACRRJ05ZDBbF+QOC+pJ7ZqsHyqmYfmiAGaRIBYJaIyATc5JQ5hQDNQ+fdY861/SQ926S4MxtYJQdX2jACFmCAI2i8mO9kvjB0CRtYgNlpYzmGRBRjvDbN61me3LbiX3BuSN+d+f1UCG6q4ac7bNlaO6eE6uWrfRERjmvDWhfBh3oNjdaXRqc4Z0X4XHtVk360FaFlqLOQqUVe6Vw5hntARIFZZWaY82XoBJeGPBDWPknWoEqr2WWMANL4/M+C5nxLsOr9chfZFvELkUk5ji9nA50g2qXbZrJIMt+ZijbHLaciIfhzaBgXTX/cNcoPNyQfA4zTu7TL8RB1/vxgbsroipMsVwA07aXeQEShvtmWwGuEzOX8r3doI8RXp2TFxQ7EFdlngC8GybMsYM/9vmalyVVe1b0+7RPanwuhx9UaiuE4RxlLOZgEczw5vY39uhm8K8youHeEKXHUqiZO2MUFUn6K9paZalNp4Rd/z4gu1pDCazQj5yZE9C4NpNnv8P05PGeTr7HhKV3C9hFI3qdVLgC2lvTRlb3xwENzunLA8LdfO9MdGFExpj9guFqXyfn848evf5e9/UVtd08uHmUq2yc/IfNPOa/6aEyA+9Pp4MuXaKevVyuYMSPM+bFpIVbHAkWEX4KjfwuKlV2yupVyzg8OXRiEvgVzHSb1k8lWJhB9fUP3UN1tEmDl44xnhCHv4YoQrNty/Sonji0vTr120UnpbQtl1ULyr1vtlvQU4K2MgnmNfZG01WApyR5WpQQpbPTJ/cmxonZUPRBVF/LfmsxlNTWGUVQdUX2DTOz94RmangMuZejV5GKF0v3baYl3HIN88imPTvpbI4jtJ6vP6fwQXQhNSYrACKSuwogNr15B8xv9cPVdOs/qgFjLuSXnTLusQLPFkzaY0dOEg+PJXFp2IdFWBH30ueFayVXiXLzfUYmksE81DSUoi496yrkLFzZSZDj5q1TTqaIjN7kvpPvw2k8UXFwTTjJ5wjVUjg/T+hPDDu1nYTlMYG5Zg0Ml2xtvKZxUMOqR/pP5LY1ZPKYFZNxf2R6AK133pq4ZUZ9dnPBXZOCHatH3G41nQ7Zm27rQvOkJSC4mK/ANr8R50kURSrXkcGZNsJkiTfUFcZcdqjt9fdIYsddQwkLO+t7zKWmofzXvRRyHgpLXKffOoIBTqFxd2kdZ73valrneMpcKfikp1BiLk1kcjae9wXsvMkMeOFeKFTbSOaGWXWfvi6IICbCt/fdej4HguQ14PxknJwGS4U2suIYMwrenSVAz4vY72IKAt9VeR3RjcrwNNx2RCSEGBBgwFjae80AdHSiOjLOZRKv3HSiJ02Bm+/zyNqB5prbmP3RHiPD09pYqNRKeM8iY5cwgqhFFb+FzUt9aK5HC/FVqqvdoQJvk3WyAB2BmSg48P2d4KazGhCrxK8UkRFuz8+mMBRvOoZu1AS+rCAGLylj7Ozj1ZuOUz12sWznOMYPXdpsBJFn0Dzq26TU52aQ808bchF9sOK4mx2zPA4cEMNfuTS0y+XKe0bHhTtvRc/P9MKXzX2k+tOm1DZwDBjiXeT3V5twdr3ekiKlPN+rup5vWVHSaPCT+w9uqZ9MqXyd1ckhALyBNl4dXWc7piNyB8Mq+hrmEme5x8pIAef2L+GJqVz+pKg1pm37d0ucSOuElCg3H8yQYkZksMIKqUk3Js44Qg==", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                    { new Guid("cd328de4-479f-4114-9dd7-940959b9b680"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("dda27863-28df-4159-9488-f8c6dba23f99"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$17TljQOBPjxwmbHlmEaCk.cUaGnroyDTtKedfZn.4fGDHqJG6bqd.", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvchhpT+FNl+2cXdxZeiM8/icGXaQFnTkwi71Q5vQyTfM0Ecf1JGwHuDaE7uoFi2D0W9alu18pFuLK75q63dfCmX0PPUEeTpw9HjD8ljCEYEfS8FQJBNvw9dW4zuGABubOyuvbUF8lWvSHw/gMZ7PEQfa4Hd9Z6rmOmmXF4YtCbyZLAXoNxwEKuXwydZMFmHNYwmAwdpcK/6s2wZqxuaaeqFz7LtBqlULjmDFkBkDOhiOKaON+GTYJFkZRrNp6r5C7Pqkm7zR6BFEvX4ud3su+oKm+CmGJU9193HODWshS5FuZ4KtUEELDE8uGmnjzKyRWoP3oKF2P/I4C9HbCr2wCQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$y/rqXYbiqljR5Pjw8XdBXuWaX3FfprRU3A9XRGbPjp.N9bZlxIU3a", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwlFoq+DrdhB2FfH9HLpo5Z0WxpvUwMZNhx+zaDjUesaFUI557gwoYBx0d2GAoxNSpwYvOongzIwtyv/uA3GLVB3wDfUdaZw8A7bJd+7Sgs36pw+4TMKJhiDan1R3T2MQd0kQKC6FqCZpeGVvQYXcb24hF4Gw8oaO74dUqOUaIN0bMyOLqnoIrMg6i1gq1dVjldsIBwnpzRYnam0O+DutKgOj+jDUMfv7+nmSH2pBqjEzVPeLN8RTn63KIMMBSyMMkfdd+fiHUae2rkrkoL43ZVZ7gSs8RqyTs4n2qPiUanJ+q7YQTEHQaAwvTS6aVxUhK+tZrZ1MTPV6UsB7NadN4QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$91R./Rzp1jTAQnDTtWD9C.qmW60UADm8HfY8Sn5EEGNfuFeEpGSwi", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0kvq5Oy3vhD+mFbI63LeRhp6FooBbogu4jgRBZp/zXMoAs0F33tBqH0HagpAhEk5xx0FI9tq0z6WBZiu4I0y7yZflAgfNkTKi/eC/Bd4a5UTCmuEbjvLFU+/xZMmVMr21JH8DuLxp6KsLLokpbNThJdeH/l2Y2LPQoV4Po3XXQtxTdempikLJu4DSo4hD23uc9/AHOEw19ICFF1odGn+GWIa+pSutla5PiMSkEgbk042fpBDaE4v/JLf8ZmPJSUcNUWDuCH5cFYWf99zSS/q0lqRE1z7alAo3qDMWMUxyADnDpFMaRZvtmZy6rS0Ol16YrIQHgcZ5bhtDNbFQmvaRQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$BNWWidmwAjsnUlnPHPRHveP1LvLHKHjoFLmu79xoWH4ziXbX26R3q", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA1cccjKRhvQx7wB1gCJ1VMkDiwHtFLBy+QMteL78qu4eBwjKFupZlmNLo8lZBwCnd4pe8+4rEgNztp7WGOnGZU6itIr6J2rqwnxL6GWKSb/IflLPODgaRJEfAO/pbRBuLNOWHx6XofmXzHreUfXdY4qpifr/G2aD/KirQCGEJ3MfdfXkJYN60JMJ6v1Wq15f30YXtEwEGFgStxzNyBJEUXnayHd/+JQacCT9ESz8OjP0UkGVzYpkMASQo9ToSylV0H3KU8lZ2TN02jQETBzrQSAZDAKQzy0iextiR75OyY3MWaUyZokOsmVWWzLi1vjrShbTMd8A21gR277pdpjGD+QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$lUK24iTMt.Ig79h0.vC73.NBpRI/VQRJyhGDc9SXoIjXlbkaG7YcC", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2H0kxSoGs12hZ0i8vMBqxV9yH+JkCS91P4JWneQmzdO9wF9VKwUEJujIyPel5JISnho7EGM9ZsP/kN7N7QQEC+nC2DOj/SYn25EAyg2qYzN+wYKCY8yi6qqRP55y5gFyq+oahmNh0mmrWuhphFsUGU1tvhBnkTQh4i6jTJQ/Yd3ZdHBGpp+V7b7n2WI1sgL/fNU4Ol2ZUzyVoTC+cZZgNgNxJ99tSgwNtOuqpMZIWcGPDaKSkUUe+WSXhUhqOhHJbM7YW5El6dV7eFIeBPcdql5sJxZyPGJm7yGFHctqUaja0u3ihL0kFbqNX4aZQhGu6hvsnT16A9VxBIIoat7FcQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$GCMBJIjPnBxgmGl//NxUnevxz2OvqUfqKKt6YByEi6s.LV5LuD2qa", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwqQtCYm3sNFB/P1ig+t1DMar1fQ3lyuL9hYOUl2u+3J+jcu4Q8wWiCwrE+y94iknvcVtNJHfUtgSo5v8yfEu8mhkNqkQ3HidKo/bEnr4w9dStu6gbgo+UdK6PA84WImCUxOWQxz1Mt6aT5zg3ZujbhF7pHOYR76oqEx2k5Ix9/+gYSGaGCnFFmNXNM5m3+cprB9Lj7UOf/K6OlNHOC4CfUk/vE9DBPxGm18SSQk4hIzL19RZ07AwGaYUJYTzOtSdetytrTYMjAgqU6Oeuz5Bl6/Dxzy+z6cK1FFUcXuuL8xTOLWk+4zxvH2ns/sURAxFT8IR23v9XMrqpSCyZpHKqQIDAQAB" });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "user_id", "address", "created_at", "created_by", "date_of_birth", "email", "full_name", "gender", "organization_id", "password", "phone", "public_key", "status", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "341 Su Van Hanh, Quan 10, TP.HCM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1986, 7, 16, 0, 0, 0, 0, DateTimeKind.Utc), "admin2@dbh.com", "Admin User 2", "Male", "11111111-1111-1111-1111-111111111102", "$2a$11$WBmYttne34YowQgBDAqSLu8LjzhKnJZXa4UFoI4eZkwoKqie2Cqhi", "1234567896", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA6mwuR0kR/P4qHGqC3dADcGEHJ1c9HbUXuhbPrSlOzASpq3XEBrKvxa4JF5HIayw8OtqRiavztJezutj39PtxcIW+ywZwwoDDvts31K7WojwO3WjSnQvAbMz82Dh42Sj6S1fPFaCH+ps/DJ20ln69R4ABQPCAbVZg0mlWTc7lP60Ij2IIYm68BqCEKLLFfi0UrSA8UfH97OkGUgyTaWi/gwdvxaNTf9WEqlCUS3I6NaKt0/fLAixfRXBPY1lrFuiMs1rDsegi8oWLs9nRDL7gstE04wp3LgP98I87cb96m5DmmGljUF7gxIG9ZM3Iv0vGiq+h+dMEfIZBldCEbxT5AQIDAQAB", "Active", null, null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), "341 Su Van Hanh, Quan 10, TP.HCM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1982, 5, 10, 0, 0, 0, 0, DateTimeKind.Utc), "doctor2@dbh.com", "Dr. Watson", "Male", "11111111-1111-1111-1111-111111111102", "$2a$11$B8GUiW0L7uRVzvx.uiXXD.u8geEiSPFpvWFR5i8XN3NX3aCr1B//O", "1234567801", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAojBvbpvwiUpVpR4aJS8IHMIL6JSWhbbkwNcZDF6xcQIuOpS/fUFPO3+2bSVisn0qdnpVBsfGnQeWCG+dCZrNNDpVPngFVz4qC2cAATkTESR97w0NehQ4YhdlQBIEyGWlNl7wrmKc+UvPdQyqwzjIrRS63fBNMABAXgh06k48i69ecLXZWhVewMlG3Q1Yjq0wBlEG2OOkimXIBLWO4B+mHqL31/NhyuraDCan/YgrmnpNKVQ8pMRyPlgWGXoe8hx60MW2rkm0mHTs6irZGbELActjWKvadx0nail+HdqVJ0SuQeN+BLVy5pF2PF4kkzpweOln2oBBYhSlHMBRHpS62QIDAQAB", "Active", null, null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"), "341 Su Van Hanh, Quan 10, TP.HCM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1990, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "pharmacist2@dbh.com", "Pharma Linh", "Female", "11111111-1111-1111-1111-111111111102", "$2a$11$yhJcBnCFXRqBNP8Se9LJe.pkm1O99shM868MyO2.SaoX8zjpadb7e", "1234567802", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvZawuy0aYYBYem4TW9HLfCYL4RsqOANx/RfZABfvutjl88I84dP3lK+MrjZUTjG/Tgpe3/Z+HFSh5WJG/PhNsJ37zg5sfHWaNzfj5r16d6+UI+8pK4CBnMsgvLUei6nyYGeMNBvg173YqJQkm/XTIw7NEqVRpjCYjXjkq2FLJ57nYg1kR2ZSG+jIHgc4PLDF0gDjyUkkpSWY3rSOOuIMclonUvT0VT0rzfmdGJ1BevsO7SGNAueHqQcr4jBebBtBGcRVjk5nfdomfIer1oo4a3mJK2ija4ewjGpOyQvOpnnsWu8I6q2Br8pRqvagVg474JBefq+DNn3ZaApaETdN9QIDAQAB", "Active", null, null },
                    { new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"), "341 Su Van Hanh, Quan 10, TP.HCM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1993, 11, 25, 0, 0, 0, 0, DateTimeKind.Utc), "nurse2@dbh.com", "Nurse Minh", "Female", "11111111-1111-1111-1111-111111111102", "$2a$11$4DsxO2v3E8zzHV5gvj8FW.ciPS8QnrlyzP/.PO12N2wKRxnqLVUmG", "1234567803", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoJT77y0o892jst97p/i6gZoRka3yHSkjtd6gcgRg0R7trrZDGNTj1QViWa9ZDbQ64KvpricRnQf5bNCDr1eR+8ajcwG17DY3urtgfdIO1t1TxzUDnTskqSO90LqQSXX2yrrlwzeE5NrxESMNS9GF3YaitlvsdRoENn8p2oxC0+USurb33c4BGkZ+b44P3vD9cCD5gZm2iyQR896VuQOkggbByJR4ye7DV/D/xvu3VGMjRl9HJyKoDc1sVTkESC3wTdc69PABeLPtx7zOJK9RIjfZ81aWgKTbFDlhuUJh9DkTwiPiUda6Crb9SEKdCRoe7FP9B1sozcnXg4e/AijB4QIDAQAB", "Active", null, null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef"), "341 Su Van Hanh, Quan 10, TP.HCM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1995, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), "patient2@dbh.com", "Jane Nguyen", "Female", "11111111-1111-1111-1111-111111111102", "$2a$11$oMEjKdGtWxyIHs8tXsGqwuGmH7hyrUUR3MnFpM5vLhNhXkCixrb2K", "1234567804", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwlmtOzzxSHrvxC6LQiI8PxW5A5eXRFDNOlvcJB+6jFjwMWXaWBM62Pl72xlSRNH+mBE3d/SmD8DeIBHPLQa6t9F7fdl6MOlETvP1j1pjuFn/WPNRVuxrQzCcYeiRtBfQcdQ+ljK75r8k7oYJ16Lk3cgtPanMmNnu2PvcZDQeb7WER/m3ddnDDO8D+WS5SIS8NAcCtozofd0i9BgOHNcySSXSRHjwgTXDBj0WlL74j6mCmsDaCuK9szg+UjeNZwwtsF7mKTCtjnNjOG6TdVwrCVN7WSgoZeruyvs5euH51nSeRNrA2nIA+rdCbqFee+ZmapBsTIKhooRxsSihZCPH0QIDAQAB", "Active", null, null },
                    { new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"), "341 Su Van Hanh, Quan 10, TP.HCM", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(1996, 8, 22, 0, 0, 0, 0, DateTimeKind.Utc), "receptionist2@dbh.com", "Pam Tran", "Female", "11111111-1111-1111-1111-111111111102", "$2a$11$1535twYMqw3Z4GxbFol2Zutsbcp7mfeyrbQY6QT5hy.xY2wuTJaHS", "1234567805", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2IJi9TJ0XEFx31k8IAZw28vk5NGx7ZCt8rrIBp7RN3QT/IxZ322OZdW5FAlKw7eJIpD65o9fJ4Cj5yvvXSj7oseT1x+bckSq6kUdBJL6MzypuBqD9GUNMgo5xMec7nG1G77U0CINkIiDM0W2rvfEyQrQW+wt3WFg3PUN1O0klXVl5HjxFTWVJNk6mxXCz9taVt1OS3kXXIN8520NpK7BlX9bnIh82qVPz7Tlylif7Z0JVy8MmpEi0XWQH3T109oVGSQ/ZQgHIfA6ZnPYKwMJLmVo8ZW5PlpeTPBvi5GNpSWPmlcCopZvhLjKC1Q6QjqsfrlU7mbrvStvvOHJVCEvyQIDAQAB", "Active", null, null }
                });

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), null, "DOC456", "Pediatrics", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef"), null, new DateOnly(1995, 3, 5), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"), "PHARM456", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"), "Verified" },
                    { new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"), null, "Nurse", "Emergency", new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"), "Verified" },
                    { new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"), "Verified" }
                });

            migrationBuilder.InsertData(
                table: "user_credentials",
                columns: new[] { "credential_id", "created_at", "credential_value", "provider", "user_id" },
                values: new object[,]
                {
                    { new Guid("32c03445-2cbc-4495-87a0-d41074648b93"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef") },
                    { new Guid("80e3da1c-c451-412a-8336-9b7faddfc9d1"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd") },
                    { new Guid("9a7e5c13-bfc5-4190-b981-3ee07be8be39"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde") },
                    { new Guid("bf37eb59-ac04-4e4c-ac90-71821a720922"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2") },
                    { new Guid("e6dadeb6-eabd-4662-928d-800073ad1e0a"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("fd8500d5-8360-4f77-ba6c-07b1cb20bf05"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1") }
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "role_id", "user_id" },
                values: new object[,]
                {
                    { 1, new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { 2, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2") },
                    { 3, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd") },
                    { 4, new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde") },
                    { 5, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef") },
                    { 6, new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1") }
                });

            migrationBuilder.InsertData(
                table: "user_security",
                columns: new[] { "user_id", "last_mfa_enroll_at", "last_password_change", "mfa_enabled", "mfa_method" },
                values: new object[,]
                {
                    { new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, null, false, null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), null, null, false, null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"), null, null, false, null },
                    { new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"), null, null, false, null },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef"), null, null, false, null },
                    { new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"), null, null, false, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"));

            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "doctor_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DeleteData(
                table: "patients",
                keyColumn: "patient_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef"));

            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "role_id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"));

            migrationBuilder.DeleteData(
                table: "staff",
                keyColumn: "staff_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("32c03445-2cbc-4495-87a0-d41074648b93"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("421460a1-e379-4135-8156-818488502c08"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("5653495e-0397-44e9-9587-13f3b159d1b3"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("56b1f409-1d30-43be-add3-1df0cda73568"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("80e3da1c-c451-412a-8336-9b7faddfc9d1"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("9a7e5c13-bfc5-4190-b981-3ee07be8be39"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("b4f065a5-6c38-4bf8-92de-5690896f2b08"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("bf37eb59-ac04-4e4c-ac90-71821a720922"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("cd328de4-479f-4114-9dd7-940959b9b680"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("dda27863-28df-4159-9488-f8c6dba23f99"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("e6dadeb6-eabd-4662-928d-800073ad1e0a"));

            migrationBuilder.DeleteData(
                table: "user_credentials",
                keyColumn: "credential_id",
                keyValue: new Guid("fd8500d5-8360-4f77-ba6c-07b1cb20bf05"));

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 1, new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 2, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 3, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 4, new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 5, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef") });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "role_id", "user_id" },
                keyValues: new object[] { 6, new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1") });

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef"));

            migrationBuilder.DeleteData(
                table: "user_security",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaa2-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccd"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-ddddddddddde"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeef"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-fffffffffff1"));

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "doctor_id", "license_image", "license_number", "specialty", "user_id", "verified_status" },
                values: new object[] { new Guid("df7f006f-4fca-4d61-b7d3-e2941b0c8fd7"), null, "DOC123", "General", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Verified" });

            migrationBuilder.InsertData(
                table: "patients",
                columns: new[] { "patient_id", "blood_type", "dob", "user_id" },
                values: new object[] { new Guid("041bafa4-efab-46c4-8b90-72a98504234a"), null, new DateOnly(1990, 1, 1), new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") });

            migrationBuilder.InsertData(
                table: "staff",
                columns: new[] { "staff_id", "license_number", "role", "specialty", "user_id", "verified_status" },
                values: new object[,]
                {
                    { new Guid("146e2c74-1bb4-44a2-8337-859181ce84e1"), null, "Nurse", "Pediatrics", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Verified" },
                    { new Guid("8ddfffa6-6ac9-4ea1-90dd-e6bba5926530"), "PHARM123", "Pharmacist", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "Verified" },
                    { new Guid("91e3b0e7-e369-450a-8625-6ee978b4a408"), null, "Receptionist", null, new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"), "Verified" }
                });

            migrationBuilder.InsertData(
                table: "user_credentials",
                columns: new[] { "credential_id", "created_at", "credential_value", "provider", "user_id" },
                values: new object[,]
                {
                    { new Guid("0db4d31a-a811-41e4-97e8-aba1b24cb786"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                    { new Guid("40f530e1-4c21-4ecf-a959-ed693e843c2f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("81f453b4-f99d-439d-b340-08791c97c5a4"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("897793fc-7f2e-4c06-a9a0-68da812d069f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("a3538554-deef-491a-87de-29a7fad52c15"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "iT+H2XicTFg7X1oXQTv0RA2RxouLqhuw9tQedJHzdq9O0Q7C5WqsCfgf+rIU24eTuW2tNUi9t9ww3Xrnqd5TG60fzoii9Yz3uWpFZhHb3bX16MkclGqOB7MTEmQb0TvXG5099HRi7hPmO3S/4yislIGSs9Qd36s2cTsVEQwc0qGBkKK8tZpSQSZakA4ES0/Yl8slfNRHhB8KZdwCsv1XLxqecSVi9NRDBDc/LVAFmgq56kDr/vrzE4b/42ycLwMqz3stZxy31jhcDTKNw6z0iN4LE3ys4737nUvfgOtfawzLpDPIR8BGPCCIzvB5jskuXfIyZPTw564VLWEFiDQAFRYHyWzcdKg8ZWy+cOscys4xXIZjSDikha4avUYlUlHKYCRZo7hobHoVdAc3VeM/XWI1E/EvRY+MjsQHiCm3TxClbu0eTpRMiiLLG9FsAUpw/O1pgfy8vvv5IiGr5kIEIQsUFsxc0BJ2/jBIOHA5JTJ2y3/dEmbub4KFxgRS6eksRKuezw10+oiFYBbm5o7hPv/fLAR7K2XI8DB1PExqdRhBsIO5xygym/ojzrbWqizmYWZ/8iguHxEKS2fBGc7nf2O6FLpep1ka9kny0GF/zQR1kyjir8y7wtKuCwM79YcfIqKUo7Nx+dXg7t7Hu4uq8PwZG2Jxl5swiQNiWxGCQpDqnicf3WNMeM+S5HosiVGYfah1yIjsGjl066uqlTqCBk52pm1LCLV2z2quuj3qIlZKwGkpXmlO/9Q/hNxW6uBL9SvH7igEtQOA+qA00B5VNMq18byUacJTyMxoQoFvKERiAw4oskVmOuZiM4GugfHyAbbqvzWLqdV5vEgnGtEe0ChxqQ7PiXpIJpppPf3JGUFRKNv4f6+oz3YZZVxmMJNwqvuuEP7fyH40OHbysqB3Cn7QFp58d0T+62pdLdbKYtvusTGHThKecpZKdAxFr8IsGQDzNIhoaVUYm76mh53EnDH5wBorjYOQ/txMRZbxXZ4DKKhcoEUkdk1kWbl81BrdH32I6g4KN4/XhRG4B2O5TbtWKRKuRMePxceuQOW3z4uEPpdgoijpDUcE5dV9U7Iy/YePXGLP4NVG6Hk92IJpYjI0pKPb2r6/yh15X5GPgdC5a96tSJmAmzAWDpYJ691cv4LhJR9ZsIwnseDvon57+OY4F8cx9w03ypPD09zYLsUKX3ASLwWBnixabT4RHdgiUPpsxnEzkXp2JcKtu96vAFseKpCAZjGgBJGathhuwKXJgtmt/9yEscm46GkZVDcy1zLz49z2YgdRYHwfrTtgKoB25IvIPSAicXcJk/AvHNsUlj1chs77VC90h942dFC5vpomZE6SNrtQ5R2bOlEjZ5lIYLSW4RMvCWhWS7Bxeai8ZukZCAulb/7XGEBtmif+YeOrEqg14+AtDteJSPpirBJdVe/nFrICcmkdz3I0hstkIsIlXEkdNvf/vcrrBTqOhnE8MEfL1mTVPu/7jZNnWwuA5LihSSPwwfS/403eMMxgHmYfn4QNw6Mm1KNJwLFeuTZZJ+3t36wScrMlpwoe+T6An/x+qCbKZVohsheA1QOyGJFiT3AonkD43oE/8jWwiQXB0fMur6MIMYoitVi4y7xWYY1aS2BRBpnDJ2y9puZV4uQ7UVLuvMtuzuSmc8vtLyeOR8csuAKeYzHsIBV0sPMQKas0jySu5J/e9edxMDiEwxt2m+Cfamm++M9nmhYEUdwsjYmznA0jm53NeB140gv8DtDHFCMYlEXi2yRWCJISHZlzjl6YprGk5RhEDvFkXVcRfELzoQFJoJgdn+kWEXHY5KiiY1d3eK7QW8OXRibMgBckNAAhBdAWyKeTExfw9xl/sYCsxTDNArlB+rXof3UI0gEl/ZiKmFmZZUi0ZNHX2BOQB+K4tOlpxvBtEoQkeZn7IE8k8Btu+TwN2GgCYofEEAI+rVPrMvO4noTTlXpwe0mOwpV68WMnnmpipqweZzp3xQD3KfZHzI2uRVPYTImGnvBFouvUyrFbP2HKvFqrD/E2nqLlBuVi6ENZdc8XMSXGt5ZgEyROWD2N9AG6adOcXJRtAp76F1cUbJFQrAbcVMg+Z5Fn6i+3t/lnC3uO+OZTv8tQ8fH4F7fMVWIQ93cOlH0a+NUUtnZE5HTUBKQJ+j7AVaXjLP/rttHVsz7oXhlk27NSr3HJH/yHmG1eOQ==", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { new Guid("d44bbb4b-7436-4032-aef3-cb2d2a8d94f5"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$JZpmqlMDCSyJrK54ZAW.peCJZ4.W..BZP7Yh.vI2nFWdFGHleEVLu", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEArBj1UDgnqQ97wKdpQN/rkBWMSSDIbcFekfvp+BKG0HdovI4dsygjUnjPEstFMNVQ5J58GWc32NuiPaXgmBdgwd8H04b0ZPoIKO8hQUTkHgWdmrZgs/ChWk2nl/+iVLcslYUfWADVshLIBoQJoS/wM8AcgRYAiofisJfNAjg+/meVazFs5jTc4D6B/kuwtxcKAVS97PkWh8XaV82cdqLHFQsNEU2fLsdWkL6Z0CDfIhFkjtKQUVsKiOIF3cGdrhVkHQfKL0mk2Jk8uTD4ay2n0Hq1R9HqhdYmzFALpe4CItAPM2n6TBdCF9MB9/9a1tw+4ddw4bD/PLVNTyUOm7ih2QIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$wfRNmZYH3t7RwwHmQvSCPeavO4wUFiBOs8Lge91RHORc4C86lX8SO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3LvqzhUCxbqGe9OekLwsdgYLsv4shC4rj2WXDK2DlXBIziqY/yl+zL5AIA7ubAILUIqgNngRQaH482tE7OuY/LwSHLzn3GIPxm0J+pxsKN2j/Wc2SVWk4pBAoEXqcmJ5WBJwQCjQaHLCMRjKBNRb8EP3S+De7L0Z0XtKs9V4Qkpx5KUH8Cpyi3hLMyRlL7/jBlCtxv8WelDSzyLX1zCLahhyTiQtmCkblzvzCyyn8hfePCPoVCmJkQlJS9fCO+e33uS1KrTFhbHaI5U/J8z860/MWara7wNux5GvnwcZ67JFj8A/I6eUohT6jPPhY3GkakBtjGWDVuphU0yTM7ZSEQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$7iMvPlcOrLzmROeo/BkG3e8tGC2gL3cqyG83MK.SupUzqEiKtTDx6", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA+yXe6wQj46PsVr5SG//7HT23N1Uiff+ff210bRB486g7lpAMbo5w5ZdQGPAPw/kyXraK+U7eMmNnenLMC58blmdtF6mtHpvyDS+jdwqwjx61Pd1abmcmGfCq0PZFgO5B7tQEhQq17OWZVw1bIJpTJa2UgKT+m8w7r9lfEWDUl+zHpYq6JHnr3EUJlJTbMlLYpfTKx639PDJoyX7RmFTM7vwnWY/F/VDprOwzI8YlSp7Qwgj6PwK2IKkIYUBPIFf2wdC0b9Vd84NrbQ50hE08TqQx5tAjAB0SOEVRtOYlaQqqXJpVKVuGUcJQgSBKFpDtFJbrTZhBMGPs67K3/TgnNQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$5kZkJV/Q03yYUbD4K95rfO.AAvGQRlN4mResx8xjGfOY81dHNKkWO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqlGUuiacotBV9K4pblisdB844Kf0UlIsU+32YOXt666HMxoueDXDE2817NhNVAb73Jn1o8ArhICYB7L5M6c2MM9mgBM7GvoXIIYmnO4fXqLjLRs0VEN2YqrgLX60Ie90GmQgT/Y6LW187/pV7t/WCqeDIqbXRRmBQAIg5Sp0mPW8Gk1K6Q6GIfy0G5q+yrNop6+iSEoVGgL4a2DUra9WZ7xp9U9njt5GUy5yIV8Axfsr5eEBRopw3S+nMReMo4Yh8SYDPuneen9EmBbYaabdFpHeKeuXcbz2OI44dotcoG6ko1/LiGdHKVfq77fgOmJbt+jzAfdaJVsfUvzdFwHlVQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$Ss1s.nFuHMcUJ/piKEIR0uRj1LFBpaFg5eynupLirfR8w0/VP3FlO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwz04/ga70TwD4lDn2x6IbvurlwpmbYF/hmN9utR5xaNEYaMWtMp3c5KmhTMNRCF91ODtABJulJ2BQwzxuWHi8mFTYtzepKHhDOtmHmJlHj3Ykx5X9S+x3wxXKuen3A5YWZejSjT2FvM7nRy4KALk8SAJ1dl3SuIuJU6PtK9tdJDqWHRPL0IjcauNEvehMQFuU2ct6nJDvdDhUlVeU8RuPc+xWjK0JqAIqRRG76X9FsFOaZNZeFbN4T3XL2Hk2Q1U4tnaiE9jBeI4qHPiFiL5kSHVsyp3jDIr42S/w5q6/8MxcW53+P89+CHBmuVqUuyt2orlfrTTCUK5X01zaducaQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$U9LFJ1oQ7SWxEysF41FLk.MYPj6B1SXburktjSMN1VA699rZtz/cO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwNT+HisjHFOZUqfh4QphEwgRbQiv0LIuIHGQXFVX4Mfkzf3Ti7IqEwpV/bPSZ8h641SLsiwE7nisf7pMRDeEdWwpORBKs1cQEBzcUWrQtLg0xSyfoIB11lP1t0vm/wbdkoVRs5onO50vnP/IbDGRLY3IOSZvJBKndFB6XHChVKKz/9IR9poeeuFjhdh0xxoVrls5s+Hg+s4HBPOuDdK2rQnNCuiXh5NQRfNQNmnLCZgRUIjG8quKIzydBWwjE6siBwR0XXYcB/MRYz2cIWraM4Bve6bSgNDcCRR0ThRFUVTMuPpkDA3opkD81ne7CCCFivfZ1e5K2LdjF/sDUNZpQQIDAQAB" });
        }
    }
}
