using Eden.Server.Administration;

namespace Eden.Protocol.Tests.Hosting;

public sealed class AdministratorBootstrapTests
{
    [Fact]
    public void InteractiveStartupCreatesAdministratorOnceWithRandomPassword()
    {
        using var directory = new TemporaryAdminDirectory();
        AdministratorAccounts accounts = new(directory.World);
        Assert.Equal(26003, AdministrationHost.DefaultPort);
        using StringWriter console = new();
        using StringWriter diagnostics = new();
        Assert.True(AdministratorBootstrap.EnsureProvisioned(accounts, true, null, console, diagnostics));
        Assert.True(accounts.Provisioned);
        string output = console.ToString();
        const string marker = "Initial administrator password: ";
        string passwordLine = Assert.Single(output.Split('\n'), line => line.StartsWith(marker, StringComparison.Ordinal));
        string password = passwordLine[marker.Length..].Trim();
        Assert.Equal(48, password.Length);
        Assert.NotNull(accounts.Authenticate("admin", password));
        Assert.Null(accounts.Authenticate("admin", "invalid password"));
        Assert.True(AdministratorBootstrap.EnsureProvisioned(accounts, true, "replacement-password", new StringWriter(), diagnostics));
        Assert.NotNull(accounts.Authenticate("admin", password));
        Assert.Null(accounts.Authenticate("admin", "replacement-password"));
        Assert.Equal(string.Empty, diagnostics.ToString());
    }

    [Fact]
    public void UnattendedStartupRequiresAnExplicitBootstrapSecret()
    {
        using var directory = new TemporaryAdminDirectory();
        AdministratorAccounts accounts = new(directory.World);
        using StringWriter console = new();
        using StringWriter diagnostics = new();
        Assert.False(AdministratorBootstrap.EnsureProvisioned(accounts, false, null, console, diagnostics));
        Assert.False(accounts.Provisioned);
        Assert.Equal(string.Empty, console.ToString());
        Assert.Contains("Administration is disabled", diagnostics.ToString());
    }

    [Fact]
    public void UnattendedBootstrapDoesNotPrintTheProvidedPassword()
    {
        using var directory = new TemporaryAdminDirectory();
        AdministratorAccounts accounts = new(directory.World);
        using StringWriter console = new();
        using StringWriter diagnostics = new();
        const string secret = "private-admin-bootstrap-password";
        Assert.True(AdministratorBootstrap.EnsureProvisioned(accounts, false, secret, console, diagnostics));
        Assert.NotNull(accounts.Authenticate("admin", secret));
        Assert.DoesNotContain(secret, console.ToString());
        Assert.DoesNotContain(secret, diagnostics.ToString());
        Assert.True(accounts.Provisioned);
    }

    [Fact]
    public void InvalidBootstrapPasswordDoesNotWriteCredentials()
    {
        using var directory = new TemporaryAdminDirectory();
        AdministratorAccounts accounts = new(directory.World);
        Assert.Throws<InvalidDataException>(() =>
            AdministratorBootstrap.EnsureProvisioned(accounts, false, "short", new StringWriter(), new StringWriter())
        );
        Assert.False(accounts.Provisioned);
    }

    private sealed class TemporaryAdminDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("eden-admin-bootstrap-");
        internal string World => Path.Combine(_directory.FullName, "world.sqlite");

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
