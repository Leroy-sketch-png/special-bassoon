using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoePortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CitizenRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nric = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DateOfDeath = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EducationAccountStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    EducationAccountBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    AccountOpenedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AccountClosedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AccountClosureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducationAccountTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CitizenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CitizenRecordId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationAccountTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationAccountTransactions_CitizenRecords_CitizenRecordId",
                        column: x => x.CitizenRecordId,
                        principalTable: "CitizenRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FasApplicationDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CitizenRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConversationHistoryJson = table.Column<string>(type: "TEXT", nullable: false),
                    DraftFieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FasApplicationDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FasApplicationDrafts_CitizenRecords_CitizenRecordId",
                        column: x => x.CitizenRecordId,
                        principalTable: "CitizenRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FasApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CitizenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ApplicationDataJson = table.Column<string>(type: "TEXT", nullable: false),
                    AdminRemarks = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CitizenRecordId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FasApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FasApplications_CitizenRecords_CitizenRecordId",
                        column: x => x.CitizenRecordId,
                        principalTable: "CitizenRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CitizenRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    EducationAccountPortion = table.Column<decimal>(type: "TEXT", nullable: false),
                    ExternalPspPortion = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    PspPaymentSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    PspTransactionReference = table.Column<string>(type: "TEXT", nullable: true),
                    WebhookIdempotencyKey = table.Column<string>(type: "TEXT", nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_CitizenRecords_CitizenRecordId",
                        column: x => x.CitizenRecordId,
                        principalTable: "CitizenRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CitizenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnrollmentDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CitizenRecordId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_CitizenRecords_CitizenRecordId",
                        column: x => x.CitizenRecordId,
                        principalTable: "CitizenRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseEnrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseFeeComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsGstApplicable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFeeComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseFeeComponents_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLineItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRecords_Nric",
                table: "CitizenRecords",
                column: "Nric",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CitizenRecordId",
                table: "CourseEnrollments",
                column: "CitizenRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_CourseId",
                table: "CourseEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFeeComponents_CourseId",
                table: "CourseFeeComponents",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationAccountTransactions_CitizenRecordId",
                table: "EducationAccountTransactions",
                column: "CitizenRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_FasApplicationDrafts_CitizenRecordId",
                table: "FasApplicationDrafts",
                column: "CitizenRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_FasApplications_CitizenRecordId",
                table: "FasApplications",
                column: "CitizenRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLineItems_InvoiceId",
                table: "InvoiceLineItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CitizenRecordId",
                table: "Invoices",
                column: "CitizenRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_WebhookIdempotencyKey",
                table: "Invoices",
                column: "WebhookIdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_InvoiceId",
                table: "PaymentAllocations",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseEnrollments");

            migrationBuilder.DropTable(
                name: "CourseFeeComponents");

            migrationBuilder.DropTable(
                name: "EducationAccountTransactions");

            migrationBuilder.DropTable(
                name: "FasApplicationDrafts");

            migrationBuilder.DropTable(
                name: "FasApplications");

            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "CitizenRecords");
        }
    }
}
