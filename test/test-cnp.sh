#!/bin/bash
#
# test-cnp-pipeline.sh - Verify cnp encrypt/decrypt pipeline operations
#
# Tests:
#   1. Encrypt a file, then decrypt it and verify content matches
#   2. Decrypt to stdout and verify output
#   3. Process decrypted output through a shell pipeline (grep, sort, wc)
#   4. Pipe stdin to cnp encrypt, write encrypted file
#   5. Round-trip: decrypt -> pipeline processing -> re-encrypt -> decrypt and verify
#   6. Environment variable password support
#   7. Custom encryption parameters round-trip

set -euo pipefail

CNP="${CNP:-/workspace/cnp}"
PASS="testpassword123"
TMPDIR=$(mktemp -d)
TESTS_RUN=0
TESTS_PASSED=0

cleanup() {
    rm -rf "$TMPDIR"
}
trap cleanup EXIT

pass() {
    TESTS_PASSED=$((TESTS_PASSED + 1))
    echo "  PASS: $1"
}

fail() {
    echo "  FAIL: $1"
    echo "        $2"
}

run_test() {
    TESTS_RUN=$((TESTS_RUN + 1))
    echo "[$TESTS_RUN] $1"
}

# --- Test 1: Basic encrypt then decrypt ---
run_test "Encrypt file, then decrypt and verify content matches"

cat > "$TMPDIR/plain.txt" <<'EOF'
Hello, World!
This is a test of the Crypto Notepad CLI.
Line three with special chars: !@#$%^&*()
Unicode: café naïve résumé
EOF

"$CNP" encrypt -i "$TMPDIR/plain.txt" -o "$TMPDIR/encrypted.cnp" -p "$PASS"

if [ ! -f "$TMPDIR/encrypted.cnp" ]; then
    fail "Encrypted file was not created" ""
else
    "$CNP" decrypt -i "$TMPDIR/encrypted.cnp" -o "$TMPDIR/decrypted.txt" -p "$PASS"
    if diff -q "$TMPDIR/plain.txt" "$TMPDIR/decrypted.txt" > /dev/null 2>&1; then
        pass "Decrypted content matches original"
    else
        fail "Decrypted content differs from original" \
             "$(diff "$TMPDIR/plain.txt" "$TMPDIR/decrypted.txt")"
    fi
fi

# --- Test 2: Decrypt to stdout ---
run_test "Decrypt to stdout and verify output"

STDOUT_OUTPUT=$("$CNP" decrypt -i "$TMPDIR/encrypted.cnp" -p "$PASS")
EXPECTED=$(cat "$TMPDIR/plain.txt")

if [ "$STDOUT_OUTPUT" = "$EXPECTED" ]; then
    pass "Stdout output matches original plaintext"
else
    fail "Stdout output differs" "Got: ${STDOUT_OUTPUT:0:80}..."
fi

# --- Test 3: Decrypt and process through shell pipeline ---
run_test "Decrypt and pipe through grep"

GREP_RESULT=$("$CNP" decrypt -i "$TMPDIR/encrypted.cnp" -p "$PASS" | grep "special chars")

if echo "$GREP_RESULT" | grep -q "special chars"; then
    pass "grep correctly filtered decrypted output"
else
    fail "grep pipeline failed" "Got: $GREP_RESULT"
fi

# --- Test 4: Decrypt, pipe through wc -l ---
run_test "Decrypt and pipe through wc -l"

LINE_COUNT=$("$CNP" decrypt -i "$TMPDIR/encrypted.cnp" -p "$PASS" | wc -l)
LINE_COUNT=$(echo "$LINE_COUNT" | tr -d ' ')

if [ "$LINE_COUNT" = "4" ]; then
    pass "Line count matches (4 lines)"
else
    fail "Line count mismatch" "Expected 4, got $LINE_COUNT"
fi

# --- Test 5: Pipe stdin to cnp encrypt, write file ---
run_test "Encrypt from stdin pipe, write encrypted file"

echo "piped input data" | "$CNP" encrypt -o "$TMPDIR/from_stdin.cnp" -p "$PASS"

if [ ! -f "$TMPDIR/from_stdin.cnp" ]; then
    fail "Encrypted file from stdin was not created" ""
else
    RESULT=$("$CNP" decrypt -i "$TMPDIR/from_stdin.cnp" -p "$PASS")
    if [ "$RESULT" = "piped input data" ]; then
        pass "Stdin encrypt -> file -> decrypt round-trip successful"
    else
        fail "Stdin round-trip content mismatch" "Got: $RESULT"
    fi
fi

# --- Test 6: Round-trip with pipeline processing ---
run_test "Round-trip: decrypt -> sort -> re-encrypt -> decrypt"

cat > "$TMPDIR/unsorted.txt" <<'EOF'
cherry
apple
banana
date
EOF

"$CNP" encrypt -i "$TMPDIR/unsorted.txt" -o "$TMPDIR/unsorted.cnp" -p "$PASS"

# Decrypt, sort, re-encrypt in a pipeline
"$CNP" decrypt -i "$TMPDIR/unsorted.cnp" -p "$PASS" \
    | sort \
    | "$CNP" encrypt -o "$TMPDIR/sorted.cnp" -p "$PASS"

SORTED_RESULT=$("$CNP" decrypt -i "$TMPDIR/sorted.cnp" -p "$PASS")
EXPECTED_SORTED=$(printf "apple\nbanana\ncherry\ndate")

if [ "$SORTED_RESULT" = "$EXPECTED_SORTED" ]; then
    pass "Decrypt -> sort -> encrypt -> decrypt pipeline works"
else
    fail "Pipeline round-trip content mismatch" "Got: $SORTED_RESULT"
fi

# --- Test 7: Environment variable password ---
run_test "Encrypt/decrypt using CNP_PASSWORD environment variable"

export CNP_PASSWORD="$PASS"

echo "env var test" | "$CNP" encrypt -o "$TMPDIR/envvar.cnp"
RESULT=$(CNP_PASSWORD="$PASS" "$CNP" decrypt -i "$TMPDIR/envvar.cnp")

unset CNP_PASSWORD

if [ "$RESULT" = "env var test" ]; then
    pass "CNP_PASSWORD environment variable works"
else
    fail "Environment variable password failed" "Got: $RESULT"
fi

# --- Test 8: Custom parameters round-trip ---
run_test "Encrypt with custom parameters (key-size=128, hash=SHA256, iterations=2000)"

echo "custom params test" | "$CNP" encrypt \
    -o "$TMPDIR/custom.cnp" \
    -p "$PASS" \
    --key-size 128 \
    --hash SHA256 \
    --iterations 2000

RESULT=$("$CNP" decrypt \
    -i "$TMPDIR/custom.cnp" \
    -p "$PASS" \
    --key-size 128 \
    --hash SHA256 \
    --iterations 2000)

if [ "$RESULT" = "custom params test" ]; then
    pass "Custom encryption parameters round-trip works"
else
    fail "Custom parameters round-trip failed" "Got: $RESULT"
fi

# --- Test 9: Binary-safe stdin/stdout piping ---
run_test "Encrypt from stdin, decrypt to stdout via pipe (no files)"

RESULT=$(echo "pure pipe test" | "$CNP" encrypt -p "$PASS" | "$CNP" decrypt -p "$PASS")

if [ "$RESULT" = "pure pipe test" ]; then
    pass "Pure stdin->encrypt->pipe->decrypt->stdout works"
else
    fail "Pure pipe round-trip failed" "Got: $RESULT"
fi

# --- Test 10: Multi-line content through full pipeline ---
run_test "Multi-line content: decrypt -> grep -> wc through pipeline"

cat > "$TMPDIR/multi.txt" <<'EOF'
ERROR: disk full
INFO: startup complete
ERROR: connection refused
INFO: request processed
WARNING: slow query
ERROR: timeout exceeded
INFO: shutdown
EOF

"$CNP" encrypt -i "$TMPDIR/multi.txt" -o "$TMPDIR/multi.cnp" -p "$PASS"

ERROR_COUNT=$("$CNP" decrypt -i "$TMPDIR/multi.cnp" -p "$PASS" | grep -c "^ERROR:")

if [ "$ERROR_COUNT" = "3" ]; then
    pass "Pipeline grep count correct (3 ERROR lines)"
else
    fail "Pipeline grep count wrong" "Expected 3, got $ERROR_COUNT"
fi

# --- Summary ---
echo ""
echo "==============================="
echo "Results: $TESTS_PASSED/$TESTS_RUN tests passed"
echo "==============================="

if [ "$TESTS_PASSED" -eq "$TESTS_RUN" ]; then
    echo "All tests passed!"
    exit 0
else
    echo "Some tests failed."
    exit 1
fi
