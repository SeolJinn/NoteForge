using NoteForge.Helpers;

namespace NoteForge.Tests.Helpers;

public class PathValidatorTests
{
    [Fact]
    public void IsWithinVault_file_inside_vault_returns_true()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a");
        var file = Path.Combine(vault, "note.md");

        Assert.True(PathValidator.IsWithinVault(file, vault));
    }

    [Fact]
    public void IsWithinVault_nested_file_returns_true()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a");
        var file = Path.Combine(vault, "sub", "deeper", "note.md");

        Assert.True(PathValidator.IsWithinVault(file, vault));
    }

    [Fact]
    public void IsWithinVault_vault_root_itself_returns_true()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a");

        Assert.True(PathValidator.IsWithinVault(vault, vault));
    }

    [Fact]
    public void IsWithinVault_parent_directory_returns_false()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a");
        var outside = Path.Combine(Path.GetTempPath(), "other.md");

        Assert.False(PathValidator.IsWithinVault(outside, vault));
    }

    [Fact]
    public void IsWithinVault_traversal_resolves_outside_returns_false()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a");
        var traversal = Path.Combine(vault, "..", "vault-b", "note.md");

        Assert.False(PathValidator.IsWithinVault(traversal, vault));
    }

    [Fact]
    public void IsWithinVault_traversal_that_resolves_back_inside_returns_true()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a");
        var roundTrip = Path.Combine(vault, "sub", "..", "note.md");

        Assert.True(PathValidator.IsWithinVault(roundTrip, vault));
    }

    [Fact]
    public void IsWithinVault_sibling_with_shared_prefix_returns_false()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault");
        var sibling = Path.Combine(Path.GetTempPath(), "vault-other", "note.md");

        Assert.False(PathValidator.IsWithinVault(sibling, vault));
    }

    [Fact]
    public void IsWithinVault_case_insensitive_on_windows()
    {
        var vault = Path.Combine(Path.GetTempPath(), "Vault-A");
        var file = Path.Combine(Path.GetTempPath(), "vault-a", "note.md");

        Assert.True(PathValidator.IsWithinVault(file, vault));
    }

    [Fact]
    public void IsWithinVault_trailing_separator_on_vault()
    {
        var vault = Path.Combine(Path.GetTempPath(), "vault-a") + Path.DirectorySeparatorChar;
        var file = Path.Combine(Path.GetTempPath(), "vault-a", "note.md");

        Assert.True(PathValidator.IsWithinVault(file, vault));
    }
}
