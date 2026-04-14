# Contributing to clidoc

Thank you for your interest in contributing to clidoc! 🎉

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/your-username/clidoc.git
   cd clidoc
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Setup

### Prerequisites
- .NET 10.0 SDK or later
- Git

### Build and Test

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run clidoc locally
dotnet run --project src/CliDoc/CliDoc.csproj -- --help
```

### Test Your Changes

Test clidoc with its own assembly (dogfooding):

```bash
# Generate metadata
dotnet run --project src/CliDoc/CliDoc.csproj -- init \
  --assembly src/CliDoc/bin/Debug/net10.0/CliDoc.dll \
  --entry-type CliDoc.Program

# Generate documentation
dotnet run --project src/CliDoc/CliDoc.csproj -- generate \
  --assembly src/CliDoc/bin/Debug/net10.0/CliDoc.dll \
  --entry-type CliDoc.Program \
  --output test-docs

# Open the generated docs
open test-docs/commands.html
```

## Code Style

- Follow existing code conventions
- Use **file-scoped namespaces**
- Enable **nullable reference types**
- Use **records** for immutable data models
- Keep methods focused and single-purpose

## Commit Messages

Use conventional commits format:

- `feat: add new feature`
- `fix: resolve bug`
- `docs: update documentation`
- `refactor: improve code structure`
- `test: add or update tests`
- `chore: maintenance tasks`

Include the Co-authored-by trailer:
```
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

## Pull Request Process

1. **Update tests** - Add or update tests for your changes
2. **Update documentation** - Update README or docs as needed
3. **Run tests** - Ensure all tests pass
4. **Build successfully** - Verify the build completes without errors
5. **Create PR** - Open a pull request with a clear description

### PR Template

**Description:**
Brief description of the changes

**Motivation:**
Why are these changes needed?

**Changes:**
- List of specific changes

**Testing:**
How were these changes tested?

**Screenshots:** (if applicable)

## Reporting Issues

When reporting issues, please include:

- **Description** - Clear description of the issue
- **Steps to reproduce** - How to reproduce the problem
- **Expected behavior** - What you expected to happen
- **Actual behavior** - What actually happened
- **Environment** - OS, .NET version, clidoc version
- **Logs/Screenshots** - Any relevant output or images

## Feature Requests

Feature requests are welcome! Please:

1. Check if the feature has already been requested
2. Describe the use case clearly
3. Explain how it would benefit users
4. Provide examples if applicable

## Questions?

Feel free to:
- Open a GitHub Discussion
- Open an issue with the "question" label
- Reach out to the maintainers

## Code of Conduct

Be respectful, constructive, and professional in all interactions.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to clidoc! 🚀
