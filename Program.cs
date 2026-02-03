using System.CommandLine;
using CryptoNotepadCli;

// Pipe detection at startup
bool stdinIsPiped = Console.IsInputRedirected;
bool stdoutIsPiped = Console.IsOutputRedirected;

var passwordOption = new Option<string?>("--password", "-p") { Description = "Encryption/decryption password" };
var inputOption = new Option<FileInfo?>("--input", "-i") { Description = "Input file path (default: stdin)" };
var outputOption = new Option<FileInfo?>("--output", "-o") { Description = "Output file path (default: stdout)" };
var keySizeOption = new Option<int>("--key-size") { Description = "Key size in bits: 128, 192, or 256", DefaultValueFactory = _ => 256 };
var hashOption = new Option<string>("--hash") { Description = "Hash algorithm: SHA1, SHA256, SHA384, SHA512, MD5", DefaultValueFactory = _ => "SHA1" };
var iterationsOption = new Option<int>("--iterations") { Description = "PBKDF1 iterations", DefaultValueFactory = _ => 1000 };
var saltOption = new Option<string?>("--salt") { Description = "Custom salt (ASCII string). If omitted, random 64 bytes are generated" };
var quietOption = new Option<bool>("--quiet", "-q") { Description = "Suppress non-essential informational output" };

var encryptCommand = new Command("encrypt", "Encrypt plaintext to .cnp format")
{
    passwordOption, inputOption, outputOption, keySizeOption, hashOption, iterationsOption, saltOption, quietOption
};

var decryptCommand = new Command("decrypt", "Decrypt .cnp format to plaintext")
{
    passwordOption, inputOption, outputOption, keySizeOption, hashOption, iterationsOption, saltOption, quietOption
};

encryptCommand.SetAction((ParseResult result) =>
{
    var password = result.GetValue(passwordOption);
    var input = result.GetValue(inputOption);
    var output = result.GetValue(outputOption);
    var keySize = result.GetValue(keySizeOption);
    var hash = result.GetValue(hashOption) ?? "SHA1";
    var iterations = result.GetValue(iterationsOption);
    var salt = result.GetValue(saltOption);
    var quiet = result.GetValue(quietOption);

    return RunEncrypt(password, input, output, keySize, hash, iterations, salt, quiet, stdinIsPiped, stdoutIsPiped);
});

decryptCommand.SetAction((ParseResult result) =>
{
    var password = result.GetValue(passwordOption);
    var input = result.GetValue(inputOption);
    var output = result.GetValue(outputOption);
    var keySize = result.GetValue(keySizeOption);
    var hash = result.GetValue(hashOption) ?? "SHA1";
    var iterations = result.GetValue(iterationsOption);
    var salt = result.GetValue(saltOption);
    var quiet = result.GetValue(quietOption);

    return RunDecrypt(password, input, output, keySize, hash, iterations, salt, quiet, stdinIsPiped, stdoutIsPiped);
});

var rootCommand = new RootCommand("Crypto Notepad CLI - encrypt/decrypt .cnp files")
{
    encryptCommand,
    decryptCommand
};

rootCommand.SetAction((ParseResult result) =>
{
    PrintUsage();
    return 0;
});

// Show detailed usage for no-args or root-level help flags
if (args.Length == 0 || args[0] is "--help" or "-h" or "-?")
{
    PrintUsage();
    return 0;
}

var parseResult = rootCommand.Parse(args);
return parseResult.Invoke();

static void PrintUsage()
{
    Console.Error.WriteLine(@"cnp - Crypto Notepad CLI

Encrypt and decrypt .cnp files compatible with Crypto Notepad.

USAGE:
    cnp <command> [options]

COMMANDS:
    encrypt    Encrypt plaintext to .cnp format
    decrypt    Decrypt .cnp format to plaintext

COMMON OPTIONS:
    -p, --password <password>    Password (or set CNP_PASSWORD env var; prompts if omitted)
    -i, --input <file>           Input file (default: stdin)
    -o, --output <file>          Output file (default: stdout)
    -q, --quiet                  Suppress non-essential informational output
    --key-size <128|192|256>     AES key size in bits (default: 256)
    --hash <algorithm>           Hash: SHA1, SHA256, SHA384, SHA512, MD5 (default: SHA1)
    --iterations <n>             PBKDF1 iterations (default: 1000)
    --salt <string>              Custom salt as ASCII string (default: random 64 bytes)
    -h, --help                   Show help information

PASSWORD RESOLUTION ORDER:
    1. --password / -p command-line option
    2. CNP_PASSWORD environment variable
    3. Interactive prompt (masked input, only when stdin is a terminal)

PIPE DETECTION:
    The tool automatically detects when stdin/stdout are pipes:
    - When stdin is piped: reads input from stdin automatically
    - When stdout is piped: writes output to stdout, suppresses info messages
    - When stdin is piped and no password provided: fails with clear error
      (use -p or CNP_PASSWORD when piping input)
    - Informational messages always go to stderr (never corrupts piped output)

EXAMPLES:
    Encrypt a file:
        cnp encrypt -i secret.txt -o secret.cnp -p mypassword

    Decrypt a file:
        cnp decrypt -i secret.cnp -o secret.txt -p mypassword

    Decrypt to stdout:
        cnp decrypt -i secret.cnp -p mypassword

    Encrypt from stdin (pipe detection):
        echo ""hello world"" | cnp encrypt -o secret.cnp -p mypassword

    Decrypt and pipe through a shell pipeline:
        cnp decrypt -i secret.cnp -p mypassword | grep ""pattern"" | wc -l

    Round-trip: decrypt, process, re-encrypt:
        cnp decrypt -i data.cnp -p pass | sort | cnp encrypt -o sorted.cnp -p pass

    Use environment variable for password (recommended for pipelines):
        export CNP_PASSWORD=mypassword
        cnp decrypt -i secret.cnp
        cnp encrypt -i plain.txt -o secret.cnp

    Quiet mode (suppress informational messages):
        cnp encrypt -i file.txt -o file.cnp -p pass -q

    Encrypt with custom parameters:
        cnp encrypt -i file.txt -o file.cnp --key-size 128 --hash SHA256 --iterations 5000

FILE FORMAT:
    .cnp files are Base64-encoded and contain:
        [IV (16 bytes)] [0x00] [salt] [0x00] [AES-CBC ciphertext]
    Files are compatible with the Crypto Notepad desktop application.
");
}

static string? ResolvePassword(string? cliPassword, bool stdinIsPiped, bool quiet)
{
    if (!string.IsNullOrEmpty(cliPassword))
        return cliPassword;

    var envPassword = Environment.GetEnvironmentVariable("CNP_PASSWORD");
    if (!string.IsNullOrEmpty(envPassword))
        return envPassword;

    // If stdin is piped, we cannot prompt for password interactively
    if (stdinIsPiped)
    {
        Console.Error.WriteLine("Error: Password required but stdin is piped.");
        Console.Error.WriteLine("Please provide password via --password/-p option or CNP_PASSWORD environment variable.");
        return null;
    }

    return PasswordReader.ReadPassword();
}

static int RunEncrypt(string? password, FileInfo? input, FileInfo? output,
    int keySize, string hash, int iterations, string? salt, bool quiet,
    bool stdinIsPiped, bool stdoutIsPiped)
{
    try
    {
        string? resolved = ResolvePassword(password, stdinIsPiped, quiet);
        if (string.IsNullOrEmpty(resolved))
        {
            return 1;
        }

        string plainText;
        if (input != null)
        {
            plainText = File.ReadAllText(input.FullName);
            if (!quiet && !stdoutIsPiped)
            {
                Console.Error.WriteLine($"Reading from: {input.FullName}");
            }
        }
        else
        {
            // When stdin is piped, read from it automatically
            if (stdinIsPiped)
            {
                if (!quiet && !stdoutIsPiped)
                {
                    Console.Error.WriteLine("Reading from stdin (piped)...");
                }
            }
            using var stdin = Console.OpenStandardInput();
            using var reader = new StreamReader(stdin);
            plainText = reader.ReadToEnd();
        }

        byte[] encrypted = CnpCrypto.Encrypt(plainText, resolved, salt, hash, iterations, keySize);

        if (output != null)
        {
            File.WriteAllBytes(output.FullName, encrypted);
            if (!quiet)
            {
                Console.Error.WriteLine($"Encrypted output written to: {output.FullName}");
            }
        }
        else
        {
            // Write to stdout (may be piped)
            using var stdout = Console.OpenStandardOutput();
            stdout.Write(encrypted, 0, encrypted.Length);
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

static int RunDecrypt(string? password, FileInfo? input, FileInfo? output,
    int keySize, string hash, int iterations, string? salt, bool quiet,
    bool stdinIsPiped, bool stdoutIsPiped)
{
    try
    {
        string? resolved = ResolvePassword(password, stdinIsPiped, quiet);
        if (string.IsNullOrEmpty(resolved))
        {
            return 1;
        }

        byte[] cipherData;
        if (input != null)
        {
            cipherData = File.ReadAllBytes(input.FullName);
            if (!quiet && !stdoutIsPiped)
            {
                Console.Error.WriteLine($"Reading from: {input.FullName}");
            }
        }
        else
        {
            // When stdin is piped, read from it automatically
            if (stdinIsPiped)
            {
                if (!quiet && !stdoutIsPiped)
                {
                    Console.Error.WriteLine("Reading from stdin (piped)...");
                }
            }
            using var stdin = Console.OpenStandardInput();
            using var ms = new MemoryStream();
            stdin.CopyTo(ms);
            cipherData = ms.ToArray();
        }

        string plainText = CnpCrypto.Decrypt(cipherData, resolved, hash, iterations, keySize);

        if (output != null)
        {
            File.WriteAllText(output.FullName, plainText);
            if (!quiet)
            {
                Console.Error.WriteLine($"Decrypted output written to: {output.FullName}");
            }
        }
        else
        {
            // Write to stdout (may be piped)
            Console.Write(plainText);
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}
