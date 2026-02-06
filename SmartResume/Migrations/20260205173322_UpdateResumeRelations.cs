using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartResume.Migrations
{
    /// <inheritdoc />
    public partial class UpdateResumeRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResumeSkills_Skills_SkillID",
                table: "ResumeSkills");

            migrationBuilder.AddForeignKey(
                name: "FK_ResumeSkills_Skills_SkillID",
                table: "ResumeSkills",
                column: "SkillID",
                principalTable: "Skills",
                principalColumn: "SkillID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResumeSkills_Skills_SkillID",
                table: "ResumeSkills");

            migrationBuilder.AddForeignKey(
                name: "FK_ResumeSkills_Skills_SkillID",
                table: "ResumeSkills",
                column: "SkillID",
                principalTable: "Skills",
                principalColumn: "SkillID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
