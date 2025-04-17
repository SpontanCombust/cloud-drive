using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudDrive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SizeBytesTypo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SizeByes",
                table: "FileVersions",
                newName: "SizeBytes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SizeBytes",
                table: "FileVersions",
                newName: "SizeByes");
        }
    }
}
