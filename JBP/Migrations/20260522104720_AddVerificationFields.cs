using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JBP.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AadhaarVerified",
                table: "Candidates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentHistory",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PanVerified",
                table: "Candidates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UanNumber",
                table: "Candidates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UanVerified",
                table: "Candidates",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AadhaarVerified",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "EmploymentHistory",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "PanVerified",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "UanNumber",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "UanVerified",
                table: "Candidates");
        }
    }
}
