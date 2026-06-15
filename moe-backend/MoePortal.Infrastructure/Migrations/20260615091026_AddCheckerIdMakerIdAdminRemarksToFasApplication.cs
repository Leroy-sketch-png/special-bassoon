using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoePortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckerIdMakerIdAdminRemarksToFasApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EducationAccountTransactions_CitizenRecords_CitizenRecordId",
                table: "EducationAccountTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FasApplications_CitizenRecords_CitizenRecordId",
                table: "FasApplications");

            migrationBuilder.DropIndex(
                name: "IX_EducationAccountTransactions_CitizenRecordId",
                table: "EducationAccountTransactions");

            migrationBuilder.DropColumn(
                name: "AccountClosedDate",
                table: "CitizenRecords");

            migrationBuilder.DropColumn(
                name: "EducationAccountBalance",
                table: "CitizenRecords");

            migrationBuilder.DropColumn(
                name: "EducationAccountStatus",
                table: "CitizenRecords");

            migrationBuilder.RenameColumn(
                name: "CitizenId",
                table: "FasApplications",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "CitizenRecordId",
                table: "EducationAccountTransactions",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "CitizenId",
                table: "EducationAccountTransactions",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "AccountOpenedDate",
                table: "CitizenRecords",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "AccountClosureReason",
                table: "CitizenRecords",
                newName: "CreatedBy");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PaymentAllocations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PaymentAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PaymentAllocations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "PaymentAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "PaymentAllocations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExternalPspPortion",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "EducationAccountPortion",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Invoices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "InvoiceLineItems",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "InvoiceLineItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InvoiceLineItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "InvoiceLineItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "InvoiceLineItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "FasApplications",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "CitizenRecordId",
                table: "FasApplications",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckerId",
                table: "FasApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "FasApplications",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FasApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MakerId",
                table: "FasApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "FasApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FasApplicationDrafts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "FasApplicationDrafts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "EducationAccountTransactions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "EducationAccountTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "EducationAccountTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EducationAccountTransactions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Courses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Courses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Courses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CourseFeeComponents",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CourseFeeComponents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CourseFeeComponents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CourseFeeComponents",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CourseFeeComponents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CourseEnrollments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CourseEnrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "CourseEnrollments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CourseEnrollments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EducationAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CitizenId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpenedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ClosedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ClosureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationAccounts_CitizenRecords_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "CitizenRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManualAccountActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    MakerId = table.Column<string>(type: "TEXT", nullable: true),
                    CheckerId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualAccountActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualAccountActions_EducationAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "EducationAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationAccountTransactions_AccountId",
                table: "EducationAccountTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationAccounts_CitizenId",
                table: "EducationAccounts",
                column: "CitizenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualAccountActions_AccountId",
                table: "ManualAccountActions",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_EducationAccountTransactions_EducationAccounts_AccountId",
                table: "EducationAccountTransactions",
                column: "AccountId",
                principalTable: "EducationAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FasApplications_CitizenRecords_CitizenRecordId",
                table: "FasApplications",
                column: "CitizenRecordId",
                principalTable: "CitizenRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EducationAccountTransactions_EducationAccounts_AccountId",
                table: "EducationAccountTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FasApplications_CitizenRecords_CitizenRecordId",
                table: "FasApplications");

            migrationBuilder.DropTable(
                name: "ManualAccountActions");

            migrationBuilder.DropTable(
                name: "EducationAccounts");

            migrationBuilder.DropIndex(
                name: "IX_EducationAccountTransactions_AccountId",
                table: "EducationAccountTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "CheckerId",
                table: "FasApplications");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FasApplications");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FasApplications");

            migrationBuilder.DropColumn(
                name: "MakerId",
                table: "FasApplications");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FasApplications");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FasApplicationDrafts");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "FasApplicationDrafts");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "EducationAccountTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "EducationAccountTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EducationAccountTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CourseFeeComponents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CourseFeeComponents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CourseFeeComponents");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CourseFeeComponents");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CourseEnrollments");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "FasApplications",
                newName: "CitizenId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "EducationAccountTransactions",
                newName: "CitizenRecordId");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "EducationAccountTransactions",
                newName: "CitizenId");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "CitizenRecords",
                newName: "AccountOpenedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "CitizenRecords",
                newName: "AccountClosureReason");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PaymentAllocations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExternalPspPortion",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EducationAccountPortion",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "InvoiceLineItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FasApplications",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "CitizenRecordId",
                table: "FasApplications",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "EducationAccountTransactions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CourseFeeComponents",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<DateOnly>(
                name: "AccountClosedDate",
                table: "CitizenRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EducationAccountBalance",
                table: "CitizenRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EducationAccountStatus",
                table: "CitizenRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EducationAccountTransactions_CitizenRecordId",
                table: "EducationAccountTransactions",
                column: "CitizenRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_EducationAccountTransactions_CitizenRecords_CitizenRecordId",
                table: "EducationAccountTransactions",
                column: "CitizenRecordId",
                principalTable: "CitizenRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FasApplications_CitizenRecords_CitizenRecordId",
                table: "FasApplications",
                column: "CitizenRecordId",
                principalTable: "CitizenRecords",
                principalColumn: "Id");
        }
    }
}
