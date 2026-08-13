using System.Text.Json;
using Guardian.ProgramStation.Core.Models;

namespace Guardian.ProgramStation.Infrastructure.Services;

/// <summary>
/// Clipboard wire format for tree nodes. A copied node is serialized into a
/// small envelope whose <see cref="FormatId"/> lets the app recognize its own
/// payload on the system clipboard, so paste can rebuild the node and fall
/// back to plain text or file paths when the clipboard holds foreign content.
/// </summary>
public static class TreeClipboardFormat
{
    public const string FormatId = "guardian-program-station.tree-node";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static string Serialize(TreeNodeModel node)
    {
        var envelope = new Envelope { Format = FormatId, Node = node };
        return JsonSerializer.Serialize(envelope, Options);
    }

    /// <summary>
    /// Returns the tree node when <paramref name="json"/> is this app's format,
    /// or null when the text came from somewhere else.
    /// </summary>
    public static TreeNodeModel? TryDeserialize(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(json, Options);
            if (envelope is null)
            {
                return null;
            }

            return string.Equals(envelope.Format, FormatId, StringComparison.Ordinal)
                ? envelope.Node
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the plain-text name to use when pasting a node <em>name</em>
    /// (not the node itself). When the clipboard holds this app's serialized
    /// tree-node format, the node's <see cref="TreeNodeModel.Name"/> is returned;
    /// otherwise the text is returned as-is (foreign plain text, folder names
    /// copied from Explorer, ...).
    /// </summary>
    public static string? ExtractName(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var node = TryDeserialize(text);
        if (node is not null)
        {
            // This is our own envelope: the name is whatever the node carries,
            // even if empty. Never fall back to the raw JSON as a "name".
            return node.Name;
        }

        return text.Trim();
    }

    private sealed class Envelope
    {
        public string Format { get; set; } = string.Empty;

        public TreeNodeModel Node { get; set; } = new();
    }
}
