using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudDrive.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ActiveFileVersionFileAttrib : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveFileVersionId",
                table: "Files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveFileVersionId",
                table: "Files");
        }
    }
}
