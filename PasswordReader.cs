using System.Runtime.InteropServices;

namespace CryptoNotepadCli;

public static class PasswordReader
{
    public static string ReadPassword()
    {
        Console.Error.Write("Password: ");

        try
        {
            string ttyPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "CON" : "/dev/tty";
            using var tty = new FileStream(ttyPath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(tty);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ReadMasked();
            }

            // On Unix, disable echo via stty
            DisableEcho();
            try
            {
                string? line = reader.ReadLine();
                Console.Error.WriteLine(); // newline after masked input
                return line ?? string.Empty;
            }
            finally
            {
                EnableEcho();
            }
        }
        catch
        {
            // Fallback: read from Console directly (won't mask)
            string? line = Console.ReadLine();
            return line ?? string.Empty;
        }
    }

    private static string ReadMasked()
    {
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Error.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Length--;
                Console.Error.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Error.Write('*');
            }
        }
        return password.ToString();
    }

    private static void DisableEcho()
    {
        RunStty("-echo");
    }

    private static void EnableEcho()
    {
        RunStty("echo");
    }

    private static void RunStty(string args)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "stty",
                Arguments = args,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            proc?.WaitForExit();
        }
        catch
        {
            // stty not available, continue without echo control
        }
    }
}
