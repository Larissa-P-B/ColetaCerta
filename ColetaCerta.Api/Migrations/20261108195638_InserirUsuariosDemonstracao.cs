using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ColetaCerta.Api.Migrations
{
    /// <inheritdoc />
    public partial class InserirUsuariosDemonstracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[]
                {
                    "Id",
                    "Login",
                    "SenhaHash",
                    "Perfil",
                    "Ativo"
                },
                values: new object[,]
                {
            {
                1,
                "admin",
                "AQAAAAIAAYagAAAAEMCvAeSPOQ9OutKsyAb5tZpxfB3goRCAZqQ8GCjcXQSsOwOOu8PsoWhGOOXZgWl5Aw==",
                "ADMIN",
                true
            },
            {
                2,
                "user",
                "AQAAAAIAAYagAAAAEOkBjbvgdIfaG9UuMsxF67Z3SGkLt8FugMN3Da7a7D93jl49NSCQACJbh32WI20yUg==",
                "USER",
                true
            }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "usuarios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "usuarios",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
