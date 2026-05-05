namespace TaskBoard.Models;

public class Label
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string Color { get; set; } = "#cccccc";

    public int WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;
}

