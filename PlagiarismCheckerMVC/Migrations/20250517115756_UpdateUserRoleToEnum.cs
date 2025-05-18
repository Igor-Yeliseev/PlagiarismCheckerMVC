using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlagiarismCheckerMVC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRoleToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала создаем временную колонку целочисленного типа
            migrationBuilder.AddColumn<int>(
                name: "RoleTemp",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Затем обновляем временную колонку значениями из перечисления
            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""RoleTemp"" = 
                CASE 
                    WHEN ""Role"" = 'user' THEN 0 
                    WHEN ""Role"" = 'admin' THEN 1 
                    WHEN ""Role"" = 'moderator' THEN 2 
                    ELSE 0 
                END;");

            // Удаляем старую строковую колонку
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // Добавляем новую колонку с правильным именем и типом
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Копируем данные из временной колонки в новую колонку
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""Role"" = ""RoleTemp"";");

            // Удаляем временную колонку
            migrationBuilder.DropColumn(
                name: "RoleTemp",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Сначала создаем временную строковую колонку
            migrationBuilder.AddColumn<string>(
                name: "RoleTemp",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "user");

            // Затем обновляем временную колонку, преобразуя значения обратно в строки
            migrationBuilder.Sql(@"
                UPDATE ""Users"" SET ""RoleTemp"" = 
                CASE 
                    WHEN ""Role"" = 0 THEN 'user' 
                    WHEN ""Role"" = 1 THEN 'admin' 
                    WHEN ""Role"" = 2 THEN 'moderator' 
                    ELSE 'user' 
                END;");

            // Удаляем старую числовую колонку
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // Добавляем новую колонку с правильным именем и типом
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "user");

            // Копируем данные из временной колонки в новую колонку
            migrationBuilder.Sql(@"UPDATE ""Users"" SET ""Role"" = ""RoleTemp"";");

            // Удаляем временную колонку
            migrationBuilder.DropColumn(
                name: "RoleTemp",
                table: "Users");
        }
    }
}
