# Changelog

## [1.0.2] - 2025-08-10
fix: Scale line number text on editor resize

fix: Correct autocomplete input and normalize line endings
- Resolves a bug where autocompletion using the Enter key would fail in specific scenarios.
- Unifies newline characters to a consistent format during text updates to prevent cross-platform inconsistencies.

feat: Refresh suggestions on character deletion
- Updates the autocomplete suggestion list when a character is deleted

fix: Only refresh suggestions on deletion when autocomplete active
- Prevents the autocomplete list from appearing unexpectedly when deleting text. The list now only updates on deletion if it was already visible.

## [1.0.1] - 2025-08-10
fix: Fix alignment issue on multi-line paste

## [1.0.0] - 2025-06-12
This is the first release of Code-Editor as a built in package.
