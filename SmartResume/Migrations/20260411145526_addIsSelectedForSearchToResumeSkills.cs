using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartResume.Migrations
{
    /// <inheritdoc />
    public partial class addIsSelectedForSearchToResumeSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelectedForSearch",
                table: "ResumeSkills",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelectedForSearch",
                table: "ResumeSkills");
        }
    }
}
