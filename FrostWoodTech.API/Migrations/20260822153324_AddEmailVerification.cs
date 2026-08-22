using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .Annotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .Annotation("Npgsql:Enum:user_role", "super_admin,admin")
                .Annotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .OldAnnotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .OldAnnotation("Npgsql:Enum:user_role", "super_admin,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "pending,approved,rejected,disabled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "email_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_verification_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_token_hash",
                table: "email_verification_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_user_id_created_at",
                table: "email_verification_tokens",
                columns: new[] { "user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "email_verified_at",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .Annotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .Annotation("Npgsql:Enum:user_role", "super_admin,admin")
                .Annotation("Npgsql:Enum:user_status", "pending,approved,rejected,disabled")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .OldAnnotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .OldAnnotation("Npgsql:Enum:user_role", "super_admin,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
