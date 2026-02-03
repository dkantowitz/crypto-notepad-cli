# Backlog Item: Stdin/Stdout Pipe Detection

## Summary

Automatically detect when stdin and/or stdout are pipes and use them for input/output without requiring explicit `-i` or `-o` flags.

## Description

Currently, the CLI requires users to either:
- Explicitly specify `-i`/`-o` options, or
- Rely on the default stdin/stdout behavior

The tool should intelligently detect when it's being used in a pipeline and automatically use stdin for input and stdout for output, improving the user experience for common pipeline scenarios.

### Current Behavior

```bash
# Works but requires no -i/-o flags:
echo "hello" | cnp encrypt -p pass > output.cnp
cnp decrypt -p pass < input.cnp
```

### Desired Behavior

When stdin is a pipe (not a terminal):
- Automatically read input from stdin if `-i` is not specified
- Suppress interactive password prompt (require `-p` or `CNP_PASSWORD`)

When stdout is a pipe (not a terminal):
- Automatically write output to stdout if `-o` is not specified
- Suppress any informational messages that might corrupt the output

Additional smart behaviors:
- When stdin is a terminal and no `-i` is given: show helpful error message
- When password prompt is needed but stdin is a pipe: fail gracefully with clear error

## Acceptance Criteria

- [ ] Detect if stdin is a pipe using `Console.IsInputRedirected`
- [ ] Detect if stdout is a pipe using `Console.IsOutputRedirected`
- [ ] When stdin is piped but no input file specified, read from stdin
- [ ] When stdout is piped but no output file specified, write to stdout
- [ ] Disable interactive password prompt when stdin is piped
- [ ] Show clear error messages for invalid combinations (e.g., piped stdin but needs password prompt)
- [ ] Add `--quiet` or `-q` flag to suppress non-essential stderr output
- [ ] Update help text to document automatic pipe detection

## Technical Notes

### .NET APIs for Pipe Detection

```csharp
// Check if stdin is redirected (piped or from file)
bool stdinIsPiped = Console.IsInputRedirected;

// Check if stdout is redirected (piped or to file)
bool stdoutIsPiped = Console.IsOutputRedirected;

// Check if stderr is redirected
bool stderrIsPiped = Console.IsErrorRedirected;
```

### Implementation Approach

1. At startup, detect pipe status for stdin/stdout/stderr
2. Modify `ResolvePassword()` to fail if stdin is piped and no password provided
3. Add logic to auto-select stdin/stdout when appropriate
4. Ensure all informational messages go to stderr, not stdout

## Priority

High

## Labels

- enhancement
- ux
- cli
