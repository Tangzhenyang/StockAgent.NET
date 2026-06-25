using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockAgent.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResearchStepArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchStepArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    ArtifactType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    JsonPayload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchStepArtifacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchStepArtifacts_ResearchTaskId_ResearchStepId",
                table: "ResearchStepArtifacts",
                columns: new[] { "ResearchTaskId", "ResearchStepId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchStepArtifacts");
        }
    }
}
