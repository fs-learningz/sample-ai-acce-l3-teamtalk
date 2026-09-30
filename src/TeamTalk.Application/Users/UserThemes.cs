namespace TeamTalk.Application.Users;

public static class UserThemes
{
    public const string Default = "light";

    public static readonly IReadOnlyList<string> All =
    [
        "light",
        "cupcake",
        "nord",
        "dark",
        "midnight",
        "dracula",
    ];

    public static bool IsValid(string? theme)
    {
        return theme is not null && All.Contains(theme);
    }
}
