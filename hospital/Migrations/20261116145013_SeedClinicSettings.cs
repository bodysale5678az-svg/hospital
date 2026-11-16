using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hospital.Migrations
{
    /// <inheritdoc />
    public partial class SeedClinicSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ClinicSettings",
                columns: new[] { "Id", "BreakEnd", "BreakStart", "ClinicName", "CloseTime", "OpenTime", "SlotMinutes", "WorkingDays" },
                values: new object[] { 1, new TimeSpan(0, 18, 0, 0, 0), new TimeSpan(0, 17, 0, 0, 0), "عيادات آل صالح التخصصي", new TimeSpan(0, 23, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), 30, "Saturday,Sunday,Monday,Tuesday,Wednesday,Thursday" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ClinicSettings",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
