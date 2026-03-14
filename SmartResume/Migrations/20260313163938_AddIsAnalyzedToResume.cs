using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartResume.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAnalyzedToResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnalyzed",
                table: "Resumes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnalyzed",
                table: "Resumes");
        }
    }
}
