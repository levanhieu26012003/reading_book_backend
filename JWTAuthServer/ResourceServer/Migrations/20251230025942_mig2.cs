using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceServer.Migrations
{
    /// <inheritdoc />
    public partial class mig2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Books",
                newName: "FileKey");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Books",
                newName: "CoverKey");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "Books",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "Books");

            migrationBuilder.RenameColumn(
                name: "FileKey",
                table: "Books",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "CoverKey",
                table: "Books",
                newName: "ImageUrl");
        }
    }
}
