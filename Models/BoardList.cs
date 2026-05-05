namespace TaskBoard.Models;

public class BoardList
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public int Position { get; set; }

    public int BoardId { get; set; }

    public Board? Board { get; set; }
}

