using System.Security.Cryptography;

namespace Eden.Server.Administration;

internal static class AdministratorBootstrap
{
    internal const string DefaultName = "admin";
    internal const string PasswordEnvironmentVariable = "EDEN_ADMIN_BOOTSTRAP_PASSWORD";

    internal static bool EnsureProvisioned(
        AdministratorAccounts accounts,
        bool interactiveConsole,
        string? initialPassword,
        TextWriter console,
        TextWriter diagnostics
    )
    {
        if (accounts.Provisioned)
        {
            return true;
        }
        if (initialPassword is null && !interactiveConsole)
        {
            diagnostics.WriteLine(
                "Administration is disabled until an administrator is provisioned. "
                    + "Set EDEN_ADMIN_BOOTSTRAP_PASSWORD before starting unattended, "
                    + "or use --set-admin-password admin in an interactive terminal."
            );
            return false;
        }
        bool generated = initialPassword is null;
        string password = initialPassword ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        if (!accounts.CreateIfMissing(DefaultName, password))
        {
            return true;
        }
        if (generated)
        {
            // Print only to an attached local terminal, never to the buffered server log.
            console.WriteLine("Created local Eden administrator for http://127.0.0.1:26003.");
            console.WriteLine($"Administrator username: {DefaultName}");
            console.WriteLine($"Initial administrator password: {password}");
            console.WriteLine("Save this password now. Eden stores only its hash and will not display it again.");
        }
        else
        {
            console.WriteLine("Created local Eden administrator from EDEN_ADMIN_BOOTSTRAP_PASSWORD (password not displayed).");
        }
        return true;
    }
}
