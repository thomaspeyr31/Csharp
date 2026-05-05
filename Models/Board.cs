namespace TaskBoard.Models;

public class Board
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int WorkspaceId { get; set; }

    public Workspace Workspace { get; set; } = null!;
}

