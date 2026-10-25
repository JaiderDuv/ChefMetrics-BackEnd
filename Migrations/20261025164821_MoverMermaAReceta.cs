using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChefMetrics_BackEnd.Migrations
{
    /// <inheritdoc />
    public partial class MoverMermaAReceta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Primero se agrega la merma a cada línea de receta...
            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeMerma",
                table: "RecetaInsumo",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            // ...se copia el valor que tenía el insumo para conservar los costos actuales.
            // Se usa SQL dinámico para que el script generado también funcione cuando
            // las operaciones se ejecutan en un solo lote (la columna nueva todavía
            // no existe al compilar el lote).
            migrationBuilder.Sql(
                """
                EXEC(N'
                UPDATE ri
                   SET PorcentajeMerma = i.PorcentajeMerma
                  FROM RecetaInsumo AS ri
                  INNER JOIN Insumo AS i ON i.Id = ri.InsumoId;
                ');
                """);

            // La base actual tiene una restricción CHECK sobre la columna; hay que
            // retirarla antes de eliminar la columna.
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[CK_Insumo_Merma]', N'C') IS NOT NULL
                    ALTER TABLE [Insumo] DROP CONSTRAINT [CK_Insumo_Merma];
                """);

            // ...y solo entonces se retira del insumo.
            migrationBuilder.DropColumn(
                name: "PorcentajeMerma",
                table: "Insumo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeMerma",
                table: "Insumo",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            // Si un insumo tiene varias recetas con mermas distintas, se conserva
            // el mayor valor para no subestimar la pérdida al revertir.
            migrationBuilder.Sql(
                """
                EXEC(N'
                UPDATE i
                   SET PorcentajeMerma = x.PorcentajeMerma
                  FROM Insumo AS i
                  INNER JOIN (
                      SELECT InsumoId, MAX(PorcentajeMerma) AS PorcentajeMerma
                        FROM RecetaInsumo
                       GROUP BY InsumoId
                  ) AS x ON x.InsumoId = i.Id;
                ');
                """);

            // Se restaura la restricción que tenía la columna original.
            // También va en SQL dinámico por la misma razón que la copia.
            migrationBuilder.Sql(
                """
                EXEC(N'
                IF OBJECT_ID(N''[CK_Insumo_Merma]'', N''C'') IS NULL
                    ALTER TABLE [Insumo] WITH CHECK
                        ADD CONSTRAINT [CK_Insumo_Merma]
                        CHECK ([PorcentajeMerma] >= 0 AND [PorcentajeMerma] <= 100);
                ');
                """);

            migrationBuilder.DropColumn(
                name: "PorcentajeMerma",
                table: "RecetaInsumo");
        }
    }
}
