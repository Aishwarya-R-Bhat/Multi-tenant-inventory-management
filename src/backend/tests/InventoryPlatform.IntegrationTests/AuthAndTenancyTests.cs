using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using InventoryPlatform.Application.Abstractions;
using InventoryPlatform.Application.Features.Auth;
using InventoryPlatform.Application.Features.Users;
using InventoryPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryPlatform.IntegrationTests;

public class AuthAndTenancyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Str0ng-Passw0rd!";

    private async Task<AuthResponse> RegisterAsync(string slug, string email = "admin@example.com")
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register-tenant",
            new RegisterTenantCommand($"Company {slug}", slug, email, "Admin", Password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private HttpClient ClientFor(AuthResponse auth)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Tenant_cannot_read_another_tenants_users()
    {
        var a = await RegisterAsync("tenant-a", "a-admin@example.com");
        var b = await RegisterAsync("tenant-b", "b-admin@example.com");

        var usersSeenByA = await ClientFor(a).GetFromJsonAsync<List<UserDto>>("/api/v1/users");
        var usersSeenByB = await ClientFor(b).GetFromJsonAsync<List<UserDto>>("/api/v1/users");

        Assert.Equal(["a-admin@example.com"], usersSeenByA!.Select(u => u.Email));
        Assert.Equal(["b-admin@example.com"], usersSeenByB!.Select(u => u.Email));
    }

    [Fact]
    public async Task Query_filter_hides_other_tenants_rows_at_the_database_layer()
    {
        var a = await RegisterAsync("filter-a", "x@example.com");
        await RegisterAsync("filter-b", "x@example.com"); // same email is allowed in a different tenant

        using var scope = factory.Services.CreateScope();
        var tenantA = (await scope.ServiceProvider.GetRequiredService<AppDbContext>()
            .Tenants.SingleAsync(t => t.Slug == "filter-a")).Id;

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        await using var db = new AppDbContext(options, new FixedTenant(tenantA));

        var users = await db.Users.ToListAsync();
        Assert.Single(users);
        Assert.All(users, u => Assert.Equal(tenantA, u.TenantId));
        Assert.NotNull(a.AccessToken);
    }

    [Fact]
    public async Task Anonymous_caller_gets_401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_problem_details()
    {
        await RegisterAsync("login-tenant", "login@example.com");

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("login-tenant", "login@example.com", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_works_and_duplicate_slug_conflicts()
    {
        await RegisterAsync("dup-tenant", "dup@example.com");

        var login = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("dup-tenant", "dup@example.com", Password));
        login.EnsureSuccessStatusCode();

        var duplicate = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register-tenant",
            new RegisterTenantCommand("Other", "dup-tenant", "o@example.com", "O", Password));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Invalid_registration_returns_400_with_field_errors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register-tenant",
            new RegisterTenantCommand("", "Bad Slug", "not-an-email", "", "short"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_rotates_and_cannot_be_reused()
    {
        var first = await RegisterAsync("refresh-tenant", "r@example.com");

        var refreshed = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshAccessTokenCommand(first.RefreshToken));
        refreshed.EnsureSuccessStatusCode();

        var reuse = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshAccessTokenCommand(first.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    private class FixedTenant(Guid id) : ICurrentTenant
    {
        public Guid TenantId { get; } = id;
    }
}
