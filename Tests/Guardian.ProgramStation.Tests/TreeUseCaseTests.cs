using Guardian.ProgramStation.Application.UseCases;
using Guardian.ProgramStation.Core.Models;
using Xunit;

namespace Guardian.ProgramStation.Tests;

/// <summary>
/// Tests the shared preview and validation use cases used by both the GUI and
/// the CLI, so their behavior is identical across entry points.
/// </summary>
public sealed class TreeUseCaseTests
{
    private static TreeModel SampleTree() => new()
    {
        Name = "Sample",
        Nodes =
        {
            new TreeNodeModel
            {
                Name = "Project",
                NodeType = "folder",
                Children =
                {
                    new TreeNodeModel
                    {
                        Name = "Source",
                        NodeType = "folder",
                        Children =
                        {
                            new TreeNodeModel { Name = "Core", NodeType = "folder" },
                            new TreeNodeModel { Name = "UI", NodeType = "folder" },
                        },
                    },
                    new TreeNodeModel { Name = "Tests", NodeType = "folder" },
                },
            },
        },
    };

    // ---- Preview ----

    [Fact]
    public void Preview_RendersAsciiTree()
    {
        var preview = new PreviewTreeUseCase();

        var rendered = preview.Execute(SampleTree());

        var lines = rendered.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);
        Assert.Equal("└── Project", lines[0].TrimEnd());
        Assert.Contains("├── Source", lines[1]);
        Assert.Contains("├── Core", lines[2]);
        Assert.Contains("└── UI", lines[3]);
        Assert.Contains("└── Tests", lines[4]);
    }

    [Fact]
    public void Preview_EmptyTree_ReturnsEmptyString()
    {
        var preview = new PreviewTreeUseCase();

        Assert.Equal(string.Empty, preview.Execute(new TreeModel()));
    }

    [Fact]
    public void Preview_DeepNesting_IndentsCorrectly()
    {
        var preview = new PreviewTreeUseCase();
        var tree = new TreeModel
        {
            Nodes =
            {
                new TreeNodeModel
                {
                    Name = "A",
                    Children =
                    {
                        new TreeNodeModel
                        {
                            Name = "B",
                            Children = { new TreeNodeModel { Name = "C" } },
                        },
                    },
                },
            },
        };

        var rendered = preview.Execute(tree);

        // Single-child levels use the last-child connector and plain indents.
        // Environment.NewLine may be \r\n on Windows.
        var normalized = rendered.Replace("\r\n", "\n");
        Assert.Equal("└── A\n    └── B\n        └── C", normalized);
    }

    // ---- Validate ----

    [Fact]
    public void Validate_ValidTree_IsValid()
    {
        var validator = new ValidateTreeUseCase();

        var result = validator.Execute(SampleTree());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_NullTree_IsInvalid()
    {
        var validator = new ValidateTreeUseCase();

        var result = validator.Execute(null);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null"));
    }

    [Fact]
    public void Validate_EmptyTree_IsInvalid()
    {
        var validator = new ValidateTreeUseCase();

        var result = validator.Execute(new TreeModel { Name = "NoNodes" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("no root nodes"));
    }

    [Fact]
    public void Validate_NodeWithEmptyName_IsInvalid()
    {
        var validator = new ValidateTreeUseCase();
        var tree = new TreeModel
        {
            Name = "T",
            Nodes = { new TreeNodeModel { Name = "  ", NodeType = "folder" } },
        };

        var result = validator.Execute(tree);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty name"));
    }

    [Fact]
    public void Validate_NodeWithUnknownType_IsInvalid()
    {
        var validator = new ValidateTreeUseCase();
        var tree = new TreeModel
        {
            Name = "T",
            Nodes = { new TreeNodeModel { Name = "Thing", NodeType = "symlink" } },
        };

        var result = validator.Execute(tree);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid type"));
    }

    [Fact]
    public void Validate_CollectsErrorsAtEveryLevel()
    {
        var validator = new ValidateTreeUseCase();
        var tree = new TreeModel
        {
            Name = "",
            Nodes =
            {
                new TreeNodeModel { Name = "A", NodeType = "file" },
                new TreeNodeModel
                {
                    Name = "B",
                    NodeType = "folder",
                    Children = { new TreeNodeModel { Name = "", NodeType = "folder" } },
                },
            },
        };

        var result = validator.Execute(tree);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("no name"));
        Assert.Contains(result.Errors, e => e.Contains("empty name"));
    }
}
