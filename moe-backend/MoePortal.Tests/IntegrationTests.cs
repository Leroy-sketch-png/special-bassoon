using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MoePortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MoePortal.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "mock-key-for-testing");
        
        // Override the DbContext to use an in-memory database for testing
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
                connection.Open();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(connection);
                });
            });
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoices_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/payments/invoices");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
