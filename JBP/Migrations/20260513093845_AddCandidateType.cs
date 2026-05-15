using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JBP.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CandidateType",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateType",
                table: "Candidates");
        }
    }
}
