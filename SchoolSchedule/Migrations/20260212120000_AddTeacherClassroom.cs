using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSchedule.Migrations
{
    public partial class AddTeacherClassroom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassroomId",
                table: "Teachers",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Teachers",
                keyColumn: "Id",
                keyValue: 1,
                column: "ClassroomId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Teachers",
                keyColumn: "Id",
                keyValue: 2,
                column: "ClassroomId",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_ClassroomId",
                table: "Teachers",
                column: "ClassroomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_Classrooms_ClassroomId",
                table: "Teachers",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_Classrooms_ClassroomId",
                table: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Teachers_ClassroomId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "ClassroomId",
                table: "Teachers");
        }
    }
}
