using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartResume.Migrations
{
    /// <inheritdoc />
    public partial class AddImportanceToResumeSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Importance",
                table: "ResumeSkills",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Importance",
                table: "ResumeSkills");
        }
    }
}
