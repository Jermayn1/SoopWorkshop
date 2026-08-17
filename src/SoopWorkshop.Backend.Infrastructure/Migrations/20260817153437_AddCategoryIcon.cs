using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoopWorkshop.Backend.Infrastructure.Migrations
{
    /// <summary>
    /// Die Kategorie bekommt ein eigenes Symbol. Bisher haben die Seitenleisten
    /// dafuer den Namen ausgewertet - damit wechselte das Symbol beim Umbenennen,
    /// und eine neue Kategorie bekam nie ein eigenes.
    /// </summary>
    public partial class AddCategoryIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IconName",
                table: "TaskCategories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Bestehende Kategorien bekommen genau das Symbol, das der bisherige
            // Namens-Switch geliefert haette. Damit sieht die Seitenleiste nach
            // der Migration unveraendert aus, statt alles auf das Standardsymbol
            // zurueckfallen zu lassen. Alle uebrigen bleiben leer - das ist der
            // Rueckfall, den es vorher auch war.
            migrationBuilder.Sql("""
                UPDATE "TaskCategories" SET "IconName" = CASE lower("Name")
                    WHEN 'grundlagen' THEN 'Terminal'
                    WHEN 'oop'        THEN 'Layers'
                    WHEN 'arrays'     THEN 'Code'
                    WHEN 'schleifen'  THEN 'Repeat'
                    ELSE "IconName"
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IconName",
                table: "TaskCategories");
        }
    }
}
