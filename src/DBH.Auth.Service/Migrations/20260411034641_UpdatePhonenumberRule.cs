using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DBH.Auth.Service.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePhonenumberRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                    { new Guid("0db4d31a-a811-41e4-97e8-aba1b24cb786"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SEEnrLSt77O/E83pgvVOFEjJBXhyz1Le9b4gvPKAIwb3SubA3tAaW7nBN5sX4XDOHcYivgoDuHotAvJhfmI5oP1JAgLUlw5j1XXBpda4yEs972nNtfLQNfilqv2ReCkqgFveBur6MmOit62CdUXjxbg9PDthrnsQnWVCsKIT9Uirr7R4usbDnn4mqjgvZzU3Xw9VbfH/GtBHEi/25phuszzSfxL/CasBc9bXEuwcYf7+SzJ+kRnJ0sIs3yCIPf6sdq/lJIOK41n72b716A/KoVb6kXmYty89UBoc8f4dk2dxJD9RjAsGhWGqgeJyt0hyQnDZO6M9PqGeZMIEe8Feradx7dl7Y3CkDVwULqhn/kbs9OEcXdw5kKDJECsRIb8vjM2slaRqyRQ5wKXq+1FKwZNFWFuMCDhJgBF8Vq1qxU8sYWE7h04mg6RRuJTVR8XNLEqVmZTm9VznGHwvBHtZfjXQ/D36wiDqv6OBj35+WEVpbqUX5vmUJMcR1tcWIsvAhBPEOFobMbx94688gfDpSlH1afUxnuxuhWSRWR5X9gQwCjmlxMkb1KxQWgCbWdc44BilKXvtL7eR3ZdMdwqkoCh1l5EEzIQ9Tv7NfqdiMZiyOkOKnqM3Zw1FFt/gYVP5rXpkjoosczMukKDrtOQEOVDGLZDF9DbKYHbqXnZJt1sS+JYtb/Eq/O//IlMDY7fNgxtrNukAWXP36mZHzjgb92mLiam7kEQY9WDwUUDKfDSjidJgEdGW2U4VAVThcpFfX+vVl2//YYinIYR1beWD6IJLu8xs5PeZhsQW0STr86E9WaCeHGjZIvX6wjdSHtlVjrGf3LOksUQzZzDuIrlXAZZadVEeeK6cDAj1sR798Wws64O0vIWXQg/PYTo8whLe+jl14fKUY5UWQ9Rdb4hac+oO5vTHMknsQBLtQgjVH9+KnjfjMX99h2xcENhxpd0ImwKXPQrao+wHk23D239+DWW1fN66oq2hpQ0j+EB6VgcNBqHwrPda/dC6F/5Lcad+kwQOk5mcCaLwuRNNStdNZs7CREacZq8/mxyGS4owzs7dZe4WDKOUNGJq1R48FJjlD+gaEOqxLtsbXPj0JXAk3RQQ+ywRGURclPycm/UjuOdH2rONgw1Z34B5kw42KxFljJP/VD9q55ObYFQjndgpkgE0R411ZQ9gF5DVx8E5EST+jZPWqZA0NTh5XAkwPqNONGf3+XtbizmKPnwseFt7W41YSIRbaR+Dtjhqm9CRK9m5GcRv64mYmbb2Qi9AvtNOFCwVuiXwfT+cUElYNN/pxGhkV7cGO2BTsUqJNpN1FTduijADenyHwnPzDnP2kJBeNr0BsFt2Xun92iP7hi8zPDDS87bui6vNWvHwVNve/s8EgTjeTU6j/iMvwgXKIWdAfu+PEqB4M+PmgG6YJDcz5eA5PhhPtMZ++2iTOg9Zx85JSrr2u6rcBAsVPIedh5vO7DBIJ+MS6k/6xOzd1dqzdVUnZ2JrO7j8MTQi4dopyTOew2VRqv7pq+2ykV+Xun4Ls2XudRiorhthxe7sWncOi/H9pWfufbNYdzJIRr89djRJfndSl0gvbZRIgkUCocfeUijnS1/B+4PwjP4+KHOu3zK3I8Y4LRcxlfXBjIkimRqOXAZtqR18uoWHtU+00ct2vDVsS65cTNe4ii6v13DiqgRh4LkLtPVhDB+5uUYLCmxVbgA3Uhv291O5fzr99Ol5GBdXzFJipmE7IIdbs6HoKeEFNJQuA6hL+TLl8qseDi2wcPrKIUDKZRZ6XPMHYABAfVKmZ0sOn5FGm9/CDAJTU6Lm3sbMZuQ0Fzf7iap4FdHGuBWlxkqIh7shJtNsrjleJ96+2bz0O6r5SjA+/7fb7ObcCiVxreDvN55qQ2WvhkcE34ndI39DjXgF5gvYQmHFDI+SJsS4ty4HsboBEMCOVIKaVW0V7SyN0DRuTmzXnTDCzSQfd3+ZBIcFJR03FaVPQ9UWmsxKNVDWIdgmUHoOeNP0vtl0fIR3VjW4O6Znq+VvBCLKbHozz+Q/IPDRWPw+rppkEnkGfLuaVc3g6hF3tPlH46tHzqR4tBW6NUmf019YerroAwhbjUr79pIN/z1bzaaZPwkKfE86N6bIROt8C0ifIJuf3fMHQpuvBy4YUaNfOZEqdzhCv/1oYXw3VTnEvAAoWskP9qlS5x754ofyTA==", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
                    { new Guid("40f530e1-4c21-4ecf-a959-ed693e843c2f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("81f453b4-f99d-439d-b340-08791c97c5a4"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("897793fc-7f2e-4c06-a9a0-68da812d069f"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("a3538554-deef-491a-87de-29a7fad52c15"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
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

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_phone",
                table: "users");

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
                    { new Guid("1b30a28b-2243-4745-a2ea-92c70eed2e3c"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") },
                    { new Guid("5919a63b-24be-4240-982c-1fc1596cb279"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc") },
                    { new Guid("7e740404-5c03-405d-9254-60e11699a633"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
                    { new Guid("c527a0e2-6375-4a23-a08c-dbb765065d81"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
                    { new Guid("e4e4be39-a542-4e37-84b2-7e8bab033908"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff") },
                    { new Guid("ed9060e0-9fe0-4839-be8d-88eca956f5a5"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "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", "EncryptedPrivateKey", new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$ujWUeJZ4pr6cnHWlGvUPMuwwWVA5Y9uBpRfzHXnBf9paIseQC61mC", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAzZDG6gSrB+jsOtEA3Nxf0/yJ309EapC37+KhHDEfrMYz58HXgC6lJ3sR3eiG7K0YCha8UHMrdgDGtFmdTt6F8ShZlDanfpAkkWdA5iH+HLUPPwmps7NvXW8YbdH0CuZRkEcuhph93jILc5SNoS5dtlzmRpgdp9sJ+OgE8fO6VzyjGT/vwwmZEpCgUHLjScylaMnMk3esjOo8L4CAB3o5MZdUybMTyLThJt7lKXL8I+epiQSa+EmtP1wy8s0YInCOXPb2FoMDkQc3ZKEdi0xuWvELNkIC6NtsmzoJHker04kwMWUjwfxHajWt2Hr1BXWKNtYgOOlBkf/EtOMlbcr8dQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$rCZlBK7kEDtsJWntRlAhDO7tMKbZ1ZY6sbN6.7R10HzWyIV8uNIBe", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAyWvWHIbG/CZDjEnCch9RYNU7dAOv/hKxh/AaSZinie/n1ksEAEJ3NikaTH79x7Q78dB89YRA+2128UkjpAcdhXN3wHLO2nrjMvpaeis1713lGpRkVdPHzs80RmfoZsOKXRiB0bAxiYWX4Qsy8HJa+fS5+O5u1rQUru5J1G7dtkjhUGFRVM0aauFQXxGQMpugz3FIbk79u7aym9XAnQ1ZhZdSIBEll651v3peF41WEfGIDgcnMe90FROfrPA6k91ykD7cMSfsUslzG9bjysrUPUbHWdwoUdBCTLsmjqqbeU9nNqFX0ggX718YXViZzy1D0TNuz9vmNa5ko9yhq3eZzQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$ZzOnuEug3XZOPvT9i65MtewRHvrm9rxrU8qRMcrjp/tugBdfzBTdq", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwHYdMIRZedpgCHLuuJTge75F95PT3dGQSzFEpsrK+LrBl782kJihBNvZ2q+9sQYpEd8fMqWYKUnhdyCtfY0Eilux8/tnS4CIRONBbo6Khqhxgk4W+2lKErIGc9e4HRmV8XFDv8mVw0o/VkcoG+tSeR/vQbihcVwK66bN5mFPQ9YT1WIFkvH2jXsXZ4cL0RUKeHC6vky9x+8t5ng9sisGzHbl4WhH3YfLNOlLSqvANXI54P+t/NgMsJLFr/6qt77GrKA9Ow0EkKPa61qX6ZOnmN2+i6g5p7MV5AXF4pl3TKZSPd/VzGj5JOGOrym63IQtoSk9yjzsbbZmpxu2RMwgeQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$tJdZgsmSuB.Kl2CSzUS/Ze3iqoA/ghj4A3.1bOF1A58MyJx7wi9n.", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwKMtWVVhVPjjbU6xkSzcJtC1gTP+pWM3aMy/MHKE3DRVM2qtaIF0gI5fyRE2ZHcxN/AN7eLYsx/QhSQnzl3gTxfdW/WvLdoXUGeEin3KmAga/JjGMpz9F/cS0lJEZVeHZlQK3Obh/ZA8qOIO7rtdwCdrVnEAu5tK9+bFFdvdmjswkQpThmQRsXxC2+CCfWl+mt4iSD1He2P234TptCA1zciG9dApSnSIQIOWguhefLkR4te/kQcxhMj6oKFKstJG1wGkeEjmTLMRdRtAjxSn7PBoC+YwCReOT98OitorOnoOKIi2BWrNEBS9H106yMNgXGZlA4A4duC7dY6VsyLdEQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$QSXoyMqDoAmMMPMkSyA9D.j7WXo5zhnLmZR/8KGk98hQsxEEV57YO", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA3EhgacxBOBe3cl5906a4BYSBICvg7D4SVwMsGzo4zqezTiJs4LLzYz7PquL1XtDQqSSpZrKGN4ohLIDb75s/Kz2XVlZ9FVdPjiFo4pnHYVEnZ7CaLZUfMij7kWIHUkXtTrTtcDYT7YopAwzvnaVE0GaItGejOeF5rkvH4xRuwcPMoC6lTPx2nZtkKaHKphwIOGmQ8lGY7aT39sJH34BeDIZZJnEAS3j+3+lzpqAWdOE/xgElO0dl18rMvGYS8oukNfmipLyVnyN/++8YYsJxD+Kh/9cDs5jDVli4zqEYXn25odFlR3jTmG0agYzuRUV0+pZ98dm+81DRlJmnq4dZNQIDAQAB" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "user_id",
                keyValue: new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                columns: new[] { "password", "public_key" },
                values: new object[] { "$2a$11$KueAq6Drm7lda3n6CUkUB.31iK04mAKEaKYh4vl2I6ITYxW39utni", "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwAP5DAdnvpM2P6PUVHQs6yPIDqG6WGoYBImWj7wGIk8a8AIbFEurhl9tsvZaUJ/ZZ/rAu+QpHT3MwsJvFoDtRs9A5rMwsu3IZH74JSLaJmE2nigl1f8fauxYRKqZKgF2jZwpUfRrwt3Hx05tLOJDU+sXpEXh6uXkd0FmEWRFSUKYnLCFPhONRHP7Ije6MHNRCXbc0QrGUjgB2c4EAQLNnplwQuO1r+/uoJT88RzKbk8H/ywnruMUtQWLVLa4Vg6PxuMKK5Rah3ao/FzYpj/2Hy6tFZjtqRodEKPzfGFyFxIJsEZlMebOLWEJZFZfWhweuTryZmzem7IgAjZ+CMXhvQIDAQAB" });
        }
    }
}
