using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolSchedule.Migrations
{
    /// <inheritdoc />
    public partial class TestDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classrooms", 
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classrooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: true),
                    AcademicClassId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AcademicClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    Shift = table.Column<int>(type: "int", nullable: false),
                    CuratorTeacherId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicClasses_Teachers_CuratorTeacherId",
                        column: x => x.CuratorTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    LessonIndex = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AcademicClassId = table.Column<int>(type: "int", nullable: false),
                    ClassroomId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lessons_AcademicClasses_AcademicClassId",
                        column: x => x.AcademicClassId,
                        principalTable: "AcademicClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lessons_Classrooms_ClassroomId",
                        column: x => x.ClassroomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Lessons_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lessons_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AcademicClassId = table.Column<int>(type: "int", nullable: false),
                    HoursPerWeek = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workloads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workloads_AcademicClasses_AcademicClassId",
                        column: x => x.AcademicClassId,
                        principalTable: "AcademicClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Workloads_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Workloads_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Classrooms",
                columns: new[] { "Id", "Capacity", "Number", "Type" },
                values: new object[,]
                {
                    { 1, 30, "101", "Обычный" },
                    { 2, 15, "202", "Компьютерный" },
                    { 3, 60, "Спортзал", "Спортзал" }
                });

            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Математика" },
                    { 2, "Русский язык" },
                    { 3, "Информатика" },
                    { 4, "Физкультура" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AcademicClassId", "FullName", "Password", "Role", "TeacherId", "Username" },
                values: new object[,]
                {
                    { 1, null, "Системный Администратор", "admin", 0, null, "admin" },
                    { 2, null, "Петров Петр Петрович", "teacher1", 1, 1, "teacher1" },
                    { 3, 1, "Ученик 11-А", "student1", 2, null, "student1" }
                });

            migrationBuilder.InsertData(
                table: "Teachers",
                columns: new[] { "Id", "FullName", "SubjectId" },
                values: new object[,]
                {
                    { 1, "Петров Петр Петрович", 1 },
                    { 2, "Сидорова Анна Ивановна", 2 }
                });

            migrationBuilder.InsertData(
                table: "AcademicClasses",
                columns: new[] { "Id", "CuratorTeacherId", "Name", "Shift", "StudentCount" },
                values: new object[,]
                {
                    { 1, 1, "11-А", 1, 25 },
                    { 2, 2, "9-Б", 2, 28 }
                });

            migrationBuilder.InsertData(
                table: "Workloads",
                columns: new[] { "Id", "AcademicClassId", "HoursPerWeek", "SubjectId", "TeacherId" },
                values: new object[,]
                {
                    { 1, 1, 5, 1, 1 },
                    { 2, 1, 3, 2, 2 },
                    { 3, 2, 4, 1, 1 },
                    { 4, 2, 3, 2, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicClasses_CuratorTeacherId",
                table: "AcademicClasses",
                column: "CuratorTeacherId",
                unique: true,
                filter: "[CuratorTeacherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_AcademicClassId_DayOfWeek_LessonIndex",
                table: "Lessons",
                columns: new[] { "AcademicClassId", "DayOfWeek", "LessonIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_ClassroomId_DayOfWeek_LessonIndex",
                table: "Lessons",
                columns: new[] { "ClassroomId", "DayOfWeek", "LessonIndex" },
                unique: true,
                filter: "[ClassroomId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_SubjectId",
                table: "Lessons",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TeacherId_DayOfWeek_LessonIndex",
                table: "Lessons",
                columns: new[] { "TeacherId", "DayOfWeek", "LessonIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SubjectId",
                table: "Teachers",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AcademicClassId",
                table: "Users",
                column: "AcademicClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TeacherId",
                table: "Users",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Workloads_AcademicClassId",
                table: "Workloads",
                column: "AcademicClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Workloads_SubjectId",
                table: "Workloads",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Workloads_TeacherId",
                table: "Workloads",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_AcademicClasses_AcademicClassId",
                table: "Users",
                column: "AcademicClassId",
                principalTable: "AcademicClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Teachers_TeacherId",
                table: "Users",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Workloads");

            migrationBuilder.DropTable(
                name: "Classrooms");

            migrationBuilder.DropTable(
                name: "AcademicClasses");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Subjects");
        }
    }
}
