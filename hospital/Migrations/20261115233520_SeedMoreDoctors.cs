using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace hospital.Migrations
{
    /// <inheritdoc />
    public partial class SeedMoreDoctors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "ConsultationFee", "ImageUrl", "Name", "Phone", "SpecializationId" },
                values: new object[,]
                {
                    { 5, 160m, null, "د. ماجد الزهراني", "0500000005", 4 },
                    { 6, 190m, null, "د. ريم السبيعي", "0500000006", 6 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
