using Olly.Storage.Models;

namespace Olly.Cards.Extensions;

public static class SourceTypeExtensions
{
    public static string GetImageUrl(this SourceType type)
    {
        if (type.IsGithub) return "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR4ExGUTEwAQn95uM4KUU-OZ7Zz1n2lDrnXfw&s";
        return "https://github.com/microsoft/teams.net/raw/main/Assets/icon.png";
    }
}