using ETutor.Infrastructure.Persistence.Seed;

namespace ETutor.Api.Configuration;

/// <summary>
/// `dotnet run --project src/ETutor.Api -- seed-dev`: creates demo accounts in the Development
/// database and prints their one-time passwords. Refuses to run in any other environment.
/// </summary>
internal static class DevSeedCommand
{
    public const string Name = "seed-dev";

    public static async Task RunAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException($"'{Name}' only runs in the Development environment.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DevDataSeeder>();
        var accounts = await seeder.SeedAsync(CancellationToken.None);

        // Console output on purpose: the developer needs the passwords once; they are not logged.
        if (accounts.Count == 0)
        {
            Console.WriteLine("Demo accounts already exist; nothing created. Use db-reset to start over.");
            return;
        }

        Console.WriteLine("Demo accounts created (save these passwords; they are not stored anywhere):");
        foreach (var account in accounts)
        {
            Console.WriteLine($"  {account.Role,-8} {account.Email,-30} {account.Password}");
        }
    }
}
