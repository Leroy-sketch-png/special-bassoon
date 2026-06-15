using Microsoft.EntityFrameworkCore;
using MoePortal.Core.Domain.Entities;
using MoePortal.Core.Domain.Enums;
using MoePortal.Infrastructure.Data;
using MoePortal.Api.Controllers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MoePortal.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MoePortal.Tests
{
    public class BillingLedgerTests
    {
        private DbContextOptions<AppDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task CreatePaymentIntent_FullEaCoverage_DeductsFromLedgerAndMarksPaid()
        {
            // Arrange
            var options = CreateNewContextOptions();
            var citizenId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var nric = "S1234567A";

            using (var context = new AppDbContext(options))
            {
                var citizen = new CitizenRecord
                {
                    Id = citizenId,
                    Nric = nric,
                    FullName = "Test Citizen",
                    DateOfBirth = new DateOnly(2000, 1, 1),
                    EducationAccount = new EducationAccount
                    {
                        Balance = 500m,
                        Status = AccountStatus.Active
                    }
                };

                var invoice = new Invoice
                {
                    Id = invoiceId,
                    CitizenRecordId = citizenId,
                    CitizenRecord = citizen,
                    InvoiceNumber = "INV-001",
                    TotalAmount = 200m,
                    Status = InvoiceStatus.Pending
                };

                context.CitizenRecords.Add(citizen);
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();
            }

            using (var context = new AppDbContext(options))
            {
                var mockPsp = new Mock<IPaymentProviderService>();
                var mockConfig = new Mock<IConfiguration>();
                var mockLogger = new Mock<ILogger<PaymentsController>>();

                var controller = new PaymentsController(context, mockPsp.Object, mockConfig.Object, mockLogger.Object);
                
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, nric),
                    new Claim("sub", nric)
                }, "mock"));

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                };
                
                // Act
                var request = new PaymentsController.CreateIntentRequest(invoiceId, "test@test.com");
                var result = await controller.CreatePaymentIntent(request, CancellationToken.None);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
                var value = System.Text.Json.JsonDocument.Parse(json).RootElement;
                
                Assert.Equal(200m, value.GetProperty("EaPortion").GetDecimal());
                Assert.Equal(0m, value.GetProperty("PspPortion").GetDecimal());
                Assert.False(value.GetProperty("RequiresPspPayment").GetBoolean());
            }

            // Verify Ledger
            using (var context = new AppDbContext(options))
            {
                var citizen = await context.CitizenRecords.Include(c => c.EducationAccount).FirstAsync(c => c.Id == citizenId);
                Assert.Equal(300m, citizen.EducationAccount!.Balance); // 500 - 200

                var invoice = await context.Invoices.Include(i => i.Allocations).FirstAsync(i => i.Id == invoiceId);
                Assert.Equal(InvoiceStatus.Paid, invoice.Status);
                Assert.Single(invoice.Allocations);
                Assert.Equal("EducationAccount", invoice.Allocations.First().Source);
                Assert.Equal(200m, invoice.Allocations.First().Amount);

                var transaction = await context.EducationAccountTransactions.FirstAsync(t => t.AccountId == citizen.EducationAccount.Id);
                Assert.Equal(-200m, transaction.Amount);
                Assert.Equal("Payment", transaction.TransactionType);
            }
        }

        [Fact]
        public async Task HitPayWebhook_PartialEaCoverage_CompletesPaymentAndLogsLedger()
        {
            // Arrange
            var options = CreateNewContextOptions();
            var citizenId = Guid.NewGuid();
            var invoiceId = Guid.NewGuid();
            var nric = "S1234567A";
            var paymentId = "psp_123";

            using (var context = new AppDbContext(options))
            {
                var citizen = new CitizenRecord
                {
                    Id = citizenId,
                    Nric = nric,
                    FullName = "Test Citizen",
                    EducationAccount = new EducationAccount
                    {
                        Balance = 150m, // Partial coverage
                        Status = AccountStatus.Active
                    }
                };

                var invoice = new Invoice
                {
                    Id = invoiceId,
                    CitizenRecordId = citizenId,
                    CitizenRecord = citizen,
                    InvoiceNumber = "INV-002",
                    TotalAmount = 200m,
                    EducationAccountPortion = 150m, // EA covers 150
                    ExternalPspPortion = 50m,      // PSP covers 50
                    Status = InvoiceStatus.Pending,
                    PspPaymentSessionId = "sess_123"
                };

                context.CitizenRecords.Add(citizen);
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();
            }

            using (var context = new AppDbContext(options))
            {
                var mockPsp = new Mock<IPaymentProviderService>();
                mockPsp.Setup(x => x.VerifyWebhookSignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);

                var mockLogger = new Mock<ILogger<PaymentWebhookController>>();

                var controller = new PaymentWebhookController(mockPsp.Object, context, mockLogger.Object);

                // Mock Request Body & Form
                var httpContext = new DefaultHttpContext();
                httpContext.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("fake=body"));
                httpContext.Request.ContentType = "application/x-www-form-urlencoded";
                httpContext.Request.Form = new FormCollection(new System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    { "payment_id", paymentId },
                    { "status", "completed" },
                    { "reference_number", invoiceId.ToString() },
                    { "amount", "50.00" },
                    { "hmac", "valid_hmac" }
                });

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };

                // Act
                var result = await controller.HandleHitPayWebhook(CancellationToken.None);

                // Assert
                Assert.IsType<OkObjectResult>(result);
            }

            // Verify Ledger
            using (var context = new AppDbContext(options))
            {
                var citizen = await context.CitizenRecords.Include(c => c.EducationAccount).FirstAsync(c => c.Id == citizenId);
                Assert.Equal(0m, citizen.EducationAccount!.Balance); // 150 - 150

                var invoice = await context.Invoices.Include(i => i.Allocations).FirstAsync(i => i.Id == invoiceId);
                Assert.Equal(InvoiceStatus.Paid, invoice.Status);
                Assert.Equal(paymentId, invoice.PspTransactionReference);
                Assert.Equal(2, invoice.Allocations.Count); // One EA, One PSP

                var pspAlloc = invoice.Allocations.First(a => a.Source == "PSP");
                Assert.Equal(50m, pspAlloc.Amount);
                Assert.Equal(paymentId, pspAlloc.Reference);

                var eaAlloc = invoice.Allocations.First(a => a.Source == "EducationAccount");
                Assert.Equal(150m, eaAlloc.Amount);

                var transaction = await context.EducationAccountTransactions.FirstAsync(t => t.AccountId == citizen.EducationAccount.Id);
                Assert.Equal(-150m, transaction.Amount);
            }
        }
    }
}
