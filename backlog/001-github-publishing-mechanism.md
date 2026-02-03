# Backlog Item: GitHub Publishing Mechanism

## Summary

Implement a publishing mechanism suitable for use with GitHub, enabling automated releases and distribution of the `cnp` CLI tool.

## Description

The project needs a streamlined way to publish releases to GitHub so users can easily download and install the CLI tool. This should include:

1. **GitHub Actions Workflow**: Create a CI/CD workflow that triggers on version tags
2. **Multi-platform Builds**: Build self-contained executables for:
   - Windows (x64, arm64)
   - macOS (x64, arm64)
   - Linux (x64, arm64)
3. **Release Automation**: Automatically create GitHub releases with:
   - Built binaries attached as assets
   - Auto-generated changelog from commits
   - Version tagging
4. **Installation Instructions**: Update README with installation methods:
   - Direct binary download from releases
   - Optional: Homebrew formula for macOS/Linux
   - Optional: Scoop manifest for Windows

## Acceptance Criteria

- [ ] GitHub Actions workflow file created (`.github/workflows/release.yml`)
- [ ] Workflow builds for all target platforms
- [ ] Workflow creates GitHub release with all binaries attached
- [ ] Release binaries are self-contained (no .NET runtime required)
- [ ] README updated with download/installation instructions

## Technical Notes

- Use `dotnet publish -c Release -r <RID> --self-contained true` for builds
- Consider single-file publishing: `-p:PublishSingleFile=true`
- Enable trimming for smaller binaries: `-p:PublishTrimmed=true`
- Runtime identifiers: `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, `linux-arm64`

## Priority

Medium

## Labels

- enhancement
- devops
- ci/cd
