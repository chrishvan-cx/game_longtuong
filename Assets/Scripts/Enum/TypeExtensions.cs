using UnityEngine;

/// <summary>
/// Extension methods for enum types
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Get the column number for a hero position
    /// </summary>
    public static int GetColumn(this HeroColumn position)
    {
        return (int)position;
    }

    /// <summary>
    /// Get the display name for a hero position
    /// </summary>
    public static string GetDisplayName(this HeroColumn position)
    {
        switch (position)
        {
            case HeroColumn.FrontLine:
                return "Front Line (Column 1)";
            case HeroColumn.MidLine:
                return "Mid Line (Column 2)";
            case HeroColumn.BackLine:
                return "Back Line (Column 3)";
            default:
                return position.ToString();
        }
    }

    /// <summary>
    /// Create HeroColumn from column number
    /// </summary>
    public static HeroColumn FromColumn(int column)
    {
        switch (column)
        {
            case 1: return HeroColumn.FrontLine;
            case 2: return HeroColumn.MidLine;
            case 3: return HeroColumn.BackLine;
            default:
                Debug.LogWarning($"Invalid column {column}, defaulting to MidLine");
                return HeroColumn.MidLine;
        }
    }
}
