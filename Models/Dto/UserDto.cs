namespace TaskBoard.Models.Dto;

public record UserDto(int Id, string Username, string Email)
{
    public static UserDto From(User user) => new(user.Id, user.Username, user.Email);
}

