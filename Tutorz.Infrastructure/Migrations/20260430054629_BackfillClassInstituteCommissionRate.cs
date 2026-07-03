using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tutorz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillClassInstituteCommissionRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.InstituteCommissionRate = i.CommissionPercentage
                FROM Classes c
                INNER JOIN Institutes i ON c.InstituteId = i.InstituteId
                WHERE c.InstituteCommissionRate = 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.InstituteCommissionRate = 0
                FROM Classes c
                INNER JOIN Institutes i ON c.InstituteId = i.InstituteId
            ");
        }
    }
}
