using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Muyu.Shared.Infrastructure;
using Xunit;

namespace Muyu.API.Tests;

/// <summary>Runs the real API against a throwaway SQLite file.</summary>
public sealed class MuyuApiFactory : WebApplicationFactory<Program>
{
    private readonly string _directory;

    public MuyuApiFactory() : this(Path.Combine(Path.GetTempPath(), "muyu-tests-" + Guid.NewGuid().ToString("N")))
    {
    }

    internal MuyuApiFactory(string directory) => _directory = directory;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_directory);
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MuyuDatabase", $"Data Source={Path.Combine(_directory, "test.db")}");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // SQLite may still hold the file briefly; it is only a temporary folder.
        }
    }
}

public sealed class MobileApiTests(MuyuApiFactory factory) : IClassFixture<MuyuApiFactory>
{
    private const string Password = "Muyu*2026";
    private static int _sequence = 1000;

    private async Task<(HttpClient Client, string Email)> SignUpAsync(string name)
    {
        var number = Interlocked.Increment(ref _sequence);
        var email = $"user{number}@gmail.com";
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name,
            email,
            phone = $"9{number:D8}",
            birthdate = "03 May 2000",
            password = Password,
            dni = $"{number:D8}"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return (client, email);
    }

    private static int NextPlotId() => Interlocked.Increment(ref _sequence);

    private static JsonObject Plot(int id, string owner, string? funder = null, string status = "available") => new()
    {
        ["id"] = id,
        ["name"] = "Esperanza",
        ["crop"] = "Papa",
        ["stage"] = "Published",
        ["progress"] = 0,
        ["status"] = status,
        ["ownerEmail"] = owner,
        ["fundedByEmail"] = funder
    };

    private static async Task<JsonElement> ReadPlotAsync(HttpClient client, int id)
    {
        var plots = await client.GetFromJsonAsync<JsonElement>("/api/v1/mobile/plots");
        return plots.EnumerateArray().Single(plot => plot.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Owner_cannot_undo_a_funding_with_stale_data()
    {
        var (farmer, farmerEmail) = await SignUpAsync("Ana Rojas");
        var (merchant, merchantEmail) = await SignUpAsync("Luis Vega");
        var id = NextPlotId();
        (await farmer.PutAsJsonAsync($"/api/v1/mobile/plots/{id}", Plot(id, farmerEmail))).EnsureSuccessStatusCode();
        (await merchant.PutAsJsonAsync(
            $"/api/v1/mobile/plots/{id}",
            Plot(id, farmerEmail, merchantEmail, "funded"))).EnsureSuccessStatusCode();

        var stale = Plot(id, farmerEmail);
        stale["crop"] = "Quinua";
        (await farmer.PutAsJsonAsync($"/api/v1/mobile/plots/{id}", stale)).EnsureSuccessStatusCode();

        var saved = await ReadPlotAsync(farmer, id);
        Assert.Equal("funded", saved.GetProperty("status").GetString());
        Assert.Equal(merchantEmail, saved.GetProperty("fundedByEmail").GetString());
        Assert.Equal("Quinua", saved.GetProperty("crop").GetString());
    }

    [Fact]
    public async Task Owner_cannot_publish_a_plot_already_assigned_to_a_funder()
    {
        var (farmer, farmerEmail) = await SignUpAsync("Ana Rojas");
        var (merchant, merchantEmail) = await SignUpAsync("Luis Vega");
        var id = NextPlotId();

        (await farmer.PutAsJsonAsync(
            $"/api/v1/mobile/plots/{id}",
            Plot(id, farmerEmail, merchantEmail))).EnsureSuccessStatusCode();

        Assert.Equal(JsonValueKind.Null, (await ReadPlotAsync(merchant, id)).GetProperty("fundedByEmail").ValueKind);
    }

    [Fact]
    public async Task Only_the_first_merchant_can_claim_an_available_plot()
    {
        var (farmer, farmerEmail) = await SignUpAsync("Ana Rojas");
        var (first, firstEmail) = await SignUpAsync("Luis Vega");
        var (second, secondEmail) = await SignUpAsync("Eva Soto");
        var id = NextPlotId();
        (await farmer.PutAsJsonAsync($"/api/v1/mobile/plots/{id}", Plot(id, farmerEmail))).EnsureSuccessStatusCode();

        var claimed = await first.PutAsJsonAsync($"/api/v1/mobile/plots/{id}", Plot(id, farmerEmail, firstEmail, "funded"));
        var rejected = await second.PutAsJsonAsync($"/api/v1/mobile/plots/{id}", Plot(id, farmerEmail, secondEmail, "funded"));

        Assert.Equal(HttpStatusCode.NoContent, claimed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
    }

    [Fact]
    public async Task Incidents_are_saved_and_only_their_author_can_read_them()
    {
        var (author, _) = await SignUpAsync("Ana Rojas");
        var (other, _) = await SignUpAsync("Luis Vega");

        var created = await author.PostAsJsonAsync(
            "/api/v1/mobile/incidents",
            new { type = "payment", description = "  The payment did not arrive.  " });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var mine = await author.GetFromJsonAsync<JsonElement>("/api/v1/mobile/incidents");
        Assert.Equal("The payment did not arrive.", mine[0].GetProperty("description").GetString());
        Assert.Equal(0, (await other.GetFromJsonAsync<JsonElement>("/api/v1/mobile/incidents")).GetArrayLength());
    }

    [Theory]
    [InlineData("unknown", "Something happened")]
    [InlineData("technical", "   ")]
    public async Task Invalid_incidents_are_rejected(string type, string description)
    {
        var (author, _) = await SignUpAsync("Ana Rojas");

        var response = await author.PostAsJsonAsync("/api/v1/mobile/incidents", new { type, description });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Incidents_require_a_session()
    {
        var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await anonymous.GetAsync("/api/v1/mobile/incidents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_database_from_an_older_version_gets_the_incidents_table()
    {
        var directory = Path.Combine(Path.GetTempPath(), "muyu-tests-" + Guid.NewGuid().ToString("N"));
        using (var previous = new MuyuApiFactory(directory))
        using (var scope = previous.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MuyuDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE mobile_incidents;");
            await dbContext.Database.CloseConnectionAsync();
        }

        using var upgraded = new MuyuApiFactory(directory);
        var count = await upgraded.Services.CreateScope().ServiceProvider
            .GetRequiredService<MuyuDbContext>().MobileIncidents.CountAsync();

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Finished_contracts_list_only_harvested_plots_of_the_farmer()
    {
        var (farmer, farmerEmail) = await SignUpAsync("Ana Rojas");
        var (merchant, merchantEmail) = await SignUpAsync("Luis Vega");
        var finished = NextPlotId();
        var ongoing = NextPlotId();
        foreach (var id in new[] { finished, ongoing })
        {
            (await farmer.PutAsJsonAsync($"/api/v1/mobile/plots/{id}", Plot(id, farmerEmail))).EnsureSuccessStatusCode();
            (await merchant.PutAsJsonAsync(
                $"/api/v1/mobile/plots/{id}",
                Plot(id, farmerEmail, merchantEmail, "funded"))).EnsureSuccessStatusCode();
        }

        var received = Plot(finished, farmerEmail, merchantEmail, "funded");
        received["harvestReceivedAt"] = "1 Mar 2027";
        (await merchant.PutAsJsonAsync($"/api/v1/mobile/plots/{finished}", received)).EnsureSuccessStatusCode();

        var contracts = await farmer.GetFromJsonAsync<JsonElement>("/api/v1/mobile/contracts/finished");

        var contract = Assert.Single(contracts.EnumerateArray());
        Assert.Equal(finished, contract.GetProperty("plotId").GetInt32());
        Assert.Equal("Luis Vega", contract.GetProperty("buyerName").GetString());
        Assert.Equal("1 Mar 2027", contract.GetProperty("endedOn").GetString());
        Assert.Equal(0, (await merchant.GetFromJsonAsync<JsonElement>("/api/v1/mobile/contracts/finished")).GetArrayLength());
    }
}
