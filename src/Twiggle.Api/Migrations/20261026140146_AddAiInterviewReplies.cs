using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Twiggle.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiInterviewReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceQuote",
                table: "Claims",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceTurnId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    NextQuestion = table.Column<string>(type: "text", nullable: false),
                    QuestionReason = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    PolicyVersion = table.Column<string>(type: "text", nullable: false),
                    SessionRevision = table.Column<int>(type: "integer", nullable: false),
                    ExternalProcessingAllowedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiReplies_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiReplies_SessionId_SourceTurnId",
                table: "AiReplies",
                columns: new[] { "SessionId", "SourceTurnId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiReplies");

            migrationBuilder.DropColumn(
                name: "SourceQuote",
                table: "Claims");
        }
    }
}
