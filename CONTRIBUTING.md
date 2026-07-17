Contributing to shad360

Thanks for taking the time to contribute to shad360. Every contribution is appreciated, whether it's fixing a bug, improving documentation, testing games, or writing code.

Before getting started, please read our "Code of Conduct" (CODE_OF_CONDUCT.md).

---

Ways to Contribute

You don't have to write code to help the project.

You can contribute by:

- Reporting bugs
- Testing games and hardware
- Improving documentation
- Fixing typos
- Suggesting new features
- Reviewing pull requests
- Improving performance or compatibility

---

Reporting Bugs

Before opening a bug report, search the issue tracker to see if it has already been reported.

A good bug report should include:

- Operating system and version
- CPU architecture (x64 or ARM64)
- shad360 version or commit hash
- Game title and Title ID (if applicable)
- Steps to reproduce
- Expected result
- Actual result
- Log files
- Screenshots or videos if they help explain the issue

If the bug only happens with one game, mention whether it also occurs in Xenia Canary.

---

Feature Requests

Feature requests are welcome.

Please include:

- A clear description of the feature
- Why it would be useful
- Possible implementation ideas (optional)

Keep requests focused on the goals of the project.

---

Pull Requests

1. Fork the repository.
2. Create a branch from "main".

git checkout -b feature/my-feature

3. Make your changes.
4. Test your changes.
5. Commit with a clear message.
6. Push your branch.
7. Open a pull request.

Keep pull requests focused on one change whenever possible.

---

Development Setup

Requirements

- CMake 3.20+
- C++20 compiler
- Python 3.8+
- Ninja (recommended)
- .NET 10 SDK
- Vulkan SDK
- Git

Windows

Visual Studio 2022 is recommended.

git clone --recursive https://github.com/shad360-emu/shad360.git
cd shad360

.\build.ps1 -Configuration Release

Linux

git clone --recursive https://github.com/shad360-emu/shad360.git
cd shad360

chmod +x build.sh
./build.sh --release

macOS

git clone --recursive https://github.com/shad360-emu/shad360.git
cd shad360

chmod +x build.sh
./build.sh --release

---

Repository Layout

src/
 ├── backend/
 └── frontend/

docs/
tests/
third_party/
.github/

---

Coding Style

C++

- Use C++20.
- Follow the existing style.
- Run "clang-format" before submitting.
- Keep functions small and readable.
- Avoid unnecessary allocations.
- Write comments only when they explain why, not what.

Naming:

- Classes: "PascalCase"
- Functions: "snake_case"
- Variables: "snake_case"
- Constants: "kPascalCase"

---

C#

- Follow standard .NET naming conventions.
- Use nullable reference types.
- Prefer "async" and "await".
- Keep UI logic inside ViewModels.
- Avoid unnecessary code-behind.

---

Commit Messages

Use Conventional Commits.

Examples:

feat(ui): add recent games list

fix(cpu): fix invalid register mapping

perf(gpu): reduce shader compilation time

docs: update build guide

test(vfs): add filesystem tests

---

Testing

Please test your changes before opening a pull request.

Native tests:

ctest --output-on-failure

Frontend tests:

dotnet test

If your change affects a game, test it with at least one known working title.

---

Documentation

If your change affects users or developers, update the documentation.

This may include:

- README.md
- CHANGELOG.md
- docs/
- Public API documentation

---

Pull Request Checklist

Before opening a PR, make sure:

- [ ] The project builds successfully.
- [ ] Tests pass.
- [ ] New code follows the project style.
- [ ] Documentation has been updated if needed.
- [ ] Commit history is clean.
- [ ] CI passes.

---

Code Review

Maintainers may request changes before merging.

Reviews usually focus on:

- Correctness
- Readability
- Performance
- Maintainability
- Compatibility

Please keep discussions respectful and constructive.

---

Third-Party Code

If your contribution adds a new dependency:

- Explain why it is needed.
- Prefer small, actively maintained libraries.
- Ensure the license is compatible with BSD 3-Clause.

Large dependencies may not be accepted.

---

Security

Do not publicly disclose security vulnerabilities.

Instead, follow the instructions in "SECURITY.md".

---

License

By submitting a contribution, you agree that it will be licensed under the BSD 3-Clause License.