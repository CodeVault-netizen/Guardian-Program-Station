using Guardian.ProgramStation.Core.Models;
using Guardian.ProgramStation.Infrastructure.Services;
using Xunit;

namespace Guardian.ProgramStation.Tests;

/// <summary>
/// Tests the wire format used to put copied tree nodes on the system clipboard
/// and rebuild them on paste. This is the UI-free core of Copy/Paste: whatever
/// <see cref="TreeClipboardFormat.Serialize"/> produces is what the running app
/// writes to and reads back from the real Windows clipboard.
/// </summary>
public sealed class TreeClipboardFormatTests
{
    [Fact]
    public void SerializeThenTryDeserialize_RoundTripsFullSubtree()
    {
        var node = new TreeNodeModel
        {
            Name = "Projects",
            NodeType = "folder",
            Children =
            {
                new TreeNodeModel { Name = "Src", NodeType = "folder" },
                new TreeNodeModel { Name = "readme.txt", NodeType = "file" },
                new TreeNodeModel
                {
                    Name = "Docs",
                    NodeType = "folder",
                    Children = { new TreeNodeModel { Name = "guide.md", NodeType = "file" } },
                },
            },
        };

        var json = TreeClipboardFormat.Serialize(node);
        var restored = TreeClipboardFormat.TryDeserialize(json);

        Assert.NotNull(restored);
        Assert.Equal("Projects", restored!.Name);
        Assert.Equal("folder", restored.NodeType);
        Assert.Equal(3, restored.Children.Count);
        Assert.Equal("Src", restored.Children[0].Name);
        Assert.Equal("folder", restored.Children[0].NodeType);
        Assert.Equal("readme.txt", restored.Children[1].Name);
        Assert.Equal("file", restored.Children[1].NodeType);
        Assert.Equal("guide.md", restored.Children[2].Children[0].Name);
        Assert.Equal("file", restored.Children[2].Children[0].NodeType);
    }

    [Fact]
    public void TryDeserialize_ForeignText_ReturnsNull()
    {
        Assert.Null(TreeClipboardFormat.TryDeserialize("hello from notepad"));
    }

    [Fact]
    public void TryDeserialize_UnrelatedJson_ReturnsNull()
    {
        Assert.Null(TreeClipboardFormat.TryDeserialize("{\"foo\":42}"));
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ReturnsNull()
    {
        Assert.Null(TreeClipboardFormat.TryDeserialize("{not json"));
    }

    [Fact]
    public void Serialize_ProducesText_ThatCanBePlacedOnClipboard()
    {
        var node = new TreeNodeModel { Name = "App.exe", NodeType = "file" };

        var json = TreeClipboardFormat.Serialize(node);

        // The format is plain text, so it can be written via SetTextAsync and
        // survives as foreign text (with a fallback) in other applications.
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains(TreeClipboardFormat.FormatId, json);
    }

    // ---- ExtractName: the plain-text name used by Copy Name / Paste Name ----

    [Fact]
    public void ExtractName_WhenClipboardHoldsSerializedNode_ReturnsOnlyTheName()
    {
        // The exact reported bug: the clipboard holds the full node envelope
        // (e.g. {"Format":"guardian-program-station.tree-node","Node":{"Name":"5455",...}}).
        // Paste Name must yield the plain name "5455", never the raw JSON.
        var node = new TreeNodeModel { Name = "5455", NodeType = "folder" };
        var json = TreeClipboardFormat.Serialize(node);

        var name = TreeClipboardFormat.ExtractName(json);

        Assert.Equal("5455", name);
        Assert.DoesNotContain(TreeClipboardFormat.FormatId, name ?? string.Empty);
    }

    [Fact]
    public void ExtractName_WhenClipboardHoldsPlainText_ReturnsTextAsIs()
    {
        // A folder name copied from Explorer stays plain text.
        Assert.Equal("7-Zip", TreeClipboardFormat.ExtractName("7-Zip"));
    }

    [Fact]
    public void ExtractName_WhenClipboardHoldsPlainTextWithWhitespace_TrimsIt()
    {
        Assert.Equal("7-Zip", TreeClipboardFormat.ExtractName("  7-Zip  "));
    }

    [Fact]
    public void ExtractName_WhenEmptyOrNull_ReturnsNull()
    {
        Assert.Null(TreeClipboardFormat.ExtractName(null));
        Assert.Null(TreeClipboardFormat.ExtractName(string.Empty));
        Assert.Null(TreeClipboardFormat.ExtractName("   "));
    }

    [Fact]
    public void ExtractName_WhenSerializedNodeHasEmptyName_FallsBackToText()
    {
        var node = new TreeNodeModel { Name = "", NodeType = "file" };
        var json = TreeClipboardFormat.Serialize(node);

        Assert.Equal(string.Empty, TreeClipboardFormat.ExtractName(json));
    }

    [Fact]
    public async Task ExtractName_RoundTripsThroughInMemoryClipboard()
    {
        // Mirrors the real flow: Copy Node puts the envelope on the clipboard,
        // then Paste Name reads it back and must get the plain name only.
        var clipboard = new ClipboardService();
        var node = new TreeNodeModel { Name = "5455", NodeType = "folder" };

        await clipboard.SetTextAsync(TreeClipboardFormat.Serialize(node));
        var name = TreeClipboardFormat.ExtractName(await clipboard.GetTextAsync());

        Assert.Equal("5455", name);
    }
}
