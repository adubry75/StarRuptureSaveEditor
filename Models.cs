namespace StarRuptureSaveEditor;

/// <summary>
/// Represents an inventory item from the player's itemsArray
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// Unique handle/ID for this item stack
    /// </summary>
    public string Handle { get; set; } = string.Empty;

    /// <summary>
    /// The full asset path to the item
    /// </summary>
    public string ItemData { get; set; } = string.Empty;

    /// <summary>
    /// The quantity of this item
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Index in the original array (for updating)
    /// </summary>
    public int ArrayIndex { get; set; }

    /// <summary>
    /// Extracts a human-friendly name from the item path.
    /// </summary>
    public string FriendlyName => ExtractFriendlyName(ItemData);

    /// <summary>
    /// Extracts just the item identifier
    /// </summary>
    public string ItemId => ExtractItemId(ItemData);

    /// <summary>
    /// Gets the category based on the item path
    /// </summary>
    public string Category => ExtractCategory(ItemData);

    private static string ExtractFriendlyName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown Item";

        try
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0) return path;

            string segment = path.Substring(lastSlash + 1);

            // Remove the _C suffix and everything after the dot
            int dotIndex = segment.IndexOf('.');
            if (dotIndex > 0)
                segment = segment.Substring(0, dotIndex);

            // Remove I_ prefix if present
            if (segment.StartsWith("I_"))
                segment = segment.Substring(2);

            return AddSpacesToPascalCase(segment);
        }
        catch
        {
            return path;
        }
    }

    private static string ExtractItemId(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        try
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0) return path;

            string segment = path.Substring(lastSlash + 1);

            int dotIndex = segment.IndexOf('.');
            if (dotIndex > 0)
                segment = segment.Substring(0, dotIndex);

            return segment;
        }
        catch
        {
            return path;
        }
    }

    private static string ExtractCategory(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Other";

        if (path.Contains("/Weapons/") || path.Contains("/AmmoTypes/"))
            return "Weapons & Ammo";
        if (path.Contains("/Items/"))
            return "Items";
        if (path.Contains("/Corporations/"))
            return "Corporation";
        if (path.Contains("/Food/") || path.Contains("/Consumables/"))
            return "Consumables";

        return "Other";
    }

    private static string AddSpacesToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new System.Text.StringBuilder();
        result.Append(text[0]);

        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                result.Append(' ');
            result.Append(text[i]);
        }

        return result.ToString();
    }
}

/// <summary>
/// Represents an item requirement within a recipe.
/// </summary>
public class RecipeItem
{
    /// <summary>
    /// The full asset path to the item
    /// </summary>
    public string ItemPath { get; set; } = string.Empty;

    /// <summary>
    /// The required count of this item.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Extracts a human-friendly name from the item path.
    /// </summary>
    public string FriendlyName => ExtractFriendlyName(ItemPath);

    /// <summary>
    /// Extracts just the item identifier
    /// </summary>
    public string ItemId => ExtractItemId(ItemPath);

    private static string ExtractFriendlyName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown Item";

        try
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0) return path;

            string segment = path.Substring(lastSlash + 1);

            int dotIndex = segment.IndexOf('.');
            if (dotIndex > 0)
                segment = segment.Substring(0, dotIndex);

            if (segment.StartsWith("I_"))
                segment = segment.Substring(2);

            return AddSpacesToPascalCase(segment);
        }
        catch
        {
            return path;
        }
    }

    private static string ExtractItemId(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        try
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0) return path;

            string segment = path.Substring(lastSlash + 1);

            int dotIndex = segment.IndexOf('.');
            if (dotIndex > 0)
                segment = segment.Substring(0, dotIndex);

            return segment;
        }
        catch
        {
            return path;
        }
    }

    private static string AddSpacesToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new System.Text.StringBuilder();
        result.Append(text[0]);

        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                result.Append(' ');
            result.Append(text[i]);
        }

        return result.ToString();
    }
}

/// <summary>
/// Represents a locked recipe entry from CrCraftingRecipeOwner.lockedRecipes
/// </summary>
public class LockedRecipe
{
    /// <summary>
    /// The full recipe path/key
    /// </summary>
    public string RecipePath { get; set; } = string.Empty;

    /// <summary>
    /// The items required to unlock this recipe.
    /// </summary>
    public List<RecipeItem> Items { get; set; } = new();

    /// <summary>
    /// Extracts a human-friendly name from the recipe path.
    /// </summary>
    public string FriendlyName => ExtractFriendlyName(RecipePath);

    /// <summary>
    /// Extracts the recipe identifier
    /// </summary>
    public string RecipeId => ExtractRecipeId(RecipePath);

    private static string ExtractFriendlyName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Unknown Recipe";

        try
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0) return path;

            string segment = path.Substring(lastSlash + 1);

            int dotIndex = segment.IndexOf('.');
            if (dotIndex > 0)
                segment = segment.Substring(0, dotIndex);

            if (segment.StartsWith("CR_"))
                segment = segment.Substring(3);

            return AddSpacesToPascalCase(segment);
        }
        catch
        {
            return path;
        }
    }

    private static string ExtractRecipeId(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        try
        {
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0) return path;

            string segment = path.Substring(lastSlash + 1);

            int dotIndex = segment.IndexOf('.');
            if (dotIndex > 0)
                segment = segment.Substring(0, dotIndex);

            return segment;
        }
        catch
        {
            return path;
        }
    }

    private static string AddSpacesToPascalCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new System.Text.StringBuilder();
        result.Append(text[0]);

        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                result.Append(' ');
            result.Append(text[i]);
        }

        return result.ToString();
    }
}
