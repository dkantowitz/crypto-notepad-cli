# Crypto Notepad CLI

A command-line tool for encrypting and decrypting `.cnp` files compatible with the [Crypto Notepad](https://github.com/nicknameisname/CryptoNotepad) desktop application.

## Features

- Encrypt plaintext files to `.cnp` format
- Decrypt `.cnp` files to plaintext
- Full compatibility with Crypto Notepad desktop application
- Support for stdin/stdout for pipeline integration
- Configurable encryption parameters (key size, hash algorithm, iterations)
- Multiple password input methods (CLI, environment variable, interactive prompt)

## Installation

### Download Pre-built Binaries

Pre-built self-contained executables are available from the [GitHub Releases](https://github.com/your-username/crypto-notepad-cli/releases) page. No .NET runtime installation required.

| Platform       | Download |
|----------------|----------|
| Windows x64    | `cnp-win-x64.exe` |
| Windows ARM64  | `cnp-win-arm64.exe` |
| macOS x64      | `cnp-osx-x64` |
| macOS ARM64    | `cnp-osx-arm64` |
| Linux x64      | `cnp-linux-x64` |
| Linux ARM64    | `cnp-linux-arm64` |

#### Quick Install (Linux/macOS)

```bash
# Download the latest release for your platform (example for Linux x64)
curl -L -o cnp https://github.com/your-username/crypto-notepad-cli/releases/latest/download/cnp-linux-x64

# Make it executable
chmod +x cnp

# Move to a directory in your PATH
sudo mv cnp /usr/local/bin/
```

#### Quick Install (Windows PowerShell)

```powershell
# Download the latest release
Invoke-WebRequest -Uri "https://github.com/your-username/crypto-notepad-cli/releases/latest/download/cnp-win-x64.exe" -OutFile "cnp.exe"

# Optionally move to a directory in your PATH
Move-Item cnp.exe "$env:LOCALAPPDATA\Microsoft\WindowsApps\"
```

### Build from Source

#### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

#### Build

```bash
git clone https://github.com/your-username/crypto-notepad-cli.git
cd crypto-notepad-cli
dotnet build
```

## Usage

```
cnp <command> [options]
```

### Commands

| Command   | Description                      |
|-----------|----------------------------------|
| `encrypt` | Encrypt plaintext to .cnp format |
| `decrypt` | Decrypt .cnp format to plaintext |

### Options

| Option                     | Description                                           |
|----------------------------|-------------------------------------------------------|
| `-p, --password <password>`| Password (or set CNP_PASSWORD env var; prompts if omitted) |
| `-i, --input <file>`       | Input file (default: stdin)                           |
| `-o, --output <file>`      | Output file (default: stdout)                         |
| `--key-size <128\|192\|256>`| AES key size in bits (default: 256)                  |
| `--hash <algorithm>`       | Hash: SHA1, SHA256, SHA384, SHA512, MD5 (default: SHA1) |
| `--iterations <n>`         | PBKDF1 iterations (default: 1000)                     |
| `--salt <string>`          | Custom salt as ASCII string (default: random 64 bytes)|
| `-h, --help`               | Show help information                                 |

### Password Resolution Order

1. `--password` / `-p` command-line option
2. `CNP_PASSWORD` environment variable
3. Interactive prompt (masked input)

## Examples

### Encrypt a file

```bash
cnp encrypt -i secret.txt -o secret.cnp -p mypassword
```

### Decrypt a file

```bash
cnp decrypt -i secret.cnp -o secret.txt -p mypassword
```

### Decrypt to stdout

```bash
cnp decrypt -i secret.cnp -p mypassword
```

### Encrypt from stdin

```bash
echo "hello world" | cnp encrypt -o secret.cnp -p mypassword
```

### Pipeline usage

```bash
cnp decrypt -i secret.cnp -p mypassword | grep "pattern" | wc -l
```

### Round-trip: decrypt, process, re-encrypt

```bash
cnp decrypt -i data.cnp -p pass | sort | cnp encrypt -o sorted.cnp -p pass
```

### Using environment variable

```bash
export CNP_PASSWORD=mypassword
cnp decrypt -i secret.cnp
cnp encrypt -i plain.txt -o secret.cnp
```

### Custom encryption parameters

```bash
cnp encrypt -i file.txt -o file.cnp --key-size 128 --hash SHA256 --iterations 5000
```

## File Format

`.cnp` files are Base64-encoded and contain:

```
[IV (16 bytes)] [0x00] [salt] [0x00] [AES-CBC ciphertext]
```

Files are fully compatible with the Crypto Notepad desktop application.

## Technical Details

- **Encryption**: AES-CBC with PKCS7 padding
- **Key Derivation**: PBKDF1 (for Crypto Notepad compatibility)
- **Default Key Size**: 256 bits
- **Default Hash**: SHA1
- **Default Iterations**: 1000

## License

MIT License
