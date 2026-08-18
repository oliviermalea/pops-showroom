using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShowRoom.Modules.Customer.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_customers_CreatedAt",
                schema: "customers",
                table: "customers",
                column: "CreatedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_customers_CreatedAt",
                schema: "customers",
                table: "customers");
        }
    }
}
