using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameCloudinaryIdToObjectKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "icon_cloudinary_id",
                table: "tags",
                newName: "icon_object_key");

            migrationBuilder.RenameColumn(
                name: "icon_cloudinary_id",
                table: "services",
                newName: "icon_object_key");

            migrationBuilder.RenameColumn(
                name: "cloudinary_id",
                table: "project_images",
                newName: "object_key");

            migrationBuilder.RenameColumn(
                name: "cover_image_id",
                table: "articles",
                newName: "cover_image_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "icon_object_key",
                table: "tags",
                newName: "icon_cloudinary_id");

            migrationBuilder.RenameColumn(
                name: "icon_object_key",
                table: "services",
                newName: "icon_cloudinary_id");

            migrationBuilder.RenameColumn(
                name: "object_key",
                table: "project_images",
                newName: "cloudinary_id");

            migrationBuilder.RenameColumn(
                name: "cover_image_key",
                table: "articles",
                newName: "cover_image_id");
        }
    }
}
