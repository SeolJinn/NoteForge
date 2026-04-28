using NoteForge.Models;
using NoteForge.Services;

namespace NoteForge.Tests.Services;

public class TabManagerTests
{
    private static Note MakeNote(string path) => new()
    {
        FilePath = path,
        Filename = Path.GetFileNameWithoutExtension(path)
    };

    [Fact]
    public void OpenTab_first_open_creates_active_tab()
    {
        var mgr = new TabManager();

        mgr.OpenTab(MakeNote("/a.md"));

        Assert.Single(mgr.Tabs);
        Assert.Equal("/a.md", mgr.ActiveTab!.FilePath);
        Assert.True(mgr.ActiveTab.IsActive);
    }

    [Fact]
    public void OpenTab_existing_filepath_activates_existing_tab_no_duplicate()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));

        mgr.OpenTab(MakeNote("/a.md"));

        Assert.Equal(2, mgr.Tabs.Count);
        Assert.Equal("/a.md", mgr.ActiveTab!.FilePath);
    }

    [Fact]
    public void OpenNewTab_creates_untitled_tab_marked_as_new()
    {
        var mgr = new TabManager();

        var tab = mgr.OpenNewTab();

        Assert.True(tab.IsNewTab);
        Assert.Same(tab, mgr.ActiveTab);
        Assert.Equal(string.Empty, tab.FilePath);
    }

    [Fact]
    public void OpenTab_when_active_is_new_tab_converts_in_place()
    {
        var mgr = new TabManager();
        var newTab = mgr.OpenNewTab();

        mgr.OpenTab(MakeNote("/a.md"));

        Assert.Single(mgr.Tabs);
        Assert.Same(newTab, mgr.ActiveTab);
        Assert.Equal("/a.md", mgr.ActiveTab!.FilePath);
        Assert.False(mgr.ActiveTab.IsNewTab);
    }

    [Fact]
    public void CloseTab_active_tab_activates_neighbor()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));
        mgr.OpenTab(MakeNote("/c.md"));
        mgr.ActivateTab(mgr.Tabs[1]);

        mgr.CloseTab(mgr.Tabs[1]);

        Assert.Equal(2, mgr.Tabs.Count);
        Assert.Equal("/c.md", mgr.ActiveTab!.FilePath);
    }

    [Fact]
    public void CloseTab_last_tab_opens_new_blank_tab()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));

        mgr.CloseTab(mgr.Tabs[0]);

        Assert.Single(mgr.Tabs);
        Assert.True(mgr.ActiveTab!.IsNewTab);
    }

    [Fact]
    public void CloseTab_inactive_tab_keeps_active_tab()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));
        var activeBefore = mgr.ActiveTab;

        mgr.CloseTab(mgr.Tabs[0]);

        Assert.Same(activeBefore, mgr.ActiveTab);
    }

    [Fact]
    public void SetDirty_updates_tab_by_filepath()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));

        mgr.SetDirty("/a.md", true);

        Assert.True(mgr.Tabs.First(t => t.FilePath == "/a.md").IsDirty);
        Assert.False(mgr.Tabs.First(t => t.FilePath == "/b.md").IsDirty);
    }

    [Fact]
    public void SetDirty_unknown_filepath_is_noop()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));

        mgr.SetDirty("/never-opened.md", true);

        Assert.False(mgr.Tabs[0].IsDirty);
    }

    [Fact]
    public void ReorderTab_moves_tab_to_new_index()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));
        mgr.OpenTab(MakeNote("/c.md"));

        mgr.ReorderTab(mgr.Tabs[0], 2);

        Assert.Equal("/b.md", mgr.Tabs[0].FilePath);
        Assert.Equal("/c.md", mgr.Tabs[1].FilePath);
        Assert.Equal("/a.md", mgr.Tabs[2].FilePath);
    }

    [Fact]
    public void ReorderTab_invalid_index_is_noop()
    {
        var mgr = new TabManager();
        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));

        mgr.ReorderTab(mgr.Tabs[0], 99);
        mgr.ReorderTab(mgr.Tabs[0], -1);

        Assert.Equal("/a.md", mgr.Tabs[0].FilePath);
        Assert.Equal("/b.md", mgr.Tabs[1].FilePath);
    }

    [Fact]
    public void ActiveTabChanged_fires_on_open_and_switch()
    {
        var mgr = new TabManager();
        var fired = 0;
        mgr.ActiveTabChanged += (_, _) => fired++;

        mgr.OpenTab(MakeNote("/a.md"));
        mgr.OpenTab(MakeNote("/b.md"));

        Assert.Equal(2, fired);
    }

    [Fact]
    public void OpenTab_with_empty_path_is_noop()
    {
        var mgr = new TabManager();
        mgr.OpenTab(new Note { FilePath = "", Filename = "x" });

        Assert.Empty(mgr.Tabs);
        Assert.Null(mgr.ActiveTab);
    }
}
