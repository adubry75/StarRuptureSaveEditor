using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
//using ForeverSkiesSaveEditor.Models;

namespace ForeverSkiesSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Crafting Recipe editor tab.
/// Manages the list of locked recipes and their modifications.
/// </summary>
public partial class CraftingEditorViewModel : ObservableObject
{
    private readonly Action _onModified;
    private JsonObject? _lockedRecipesNode;
    
    [ObservableProperty]
    private LockedRecipeViewModel? _selectedRecipe;
    
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    public CraftingEditorViewModel(Action onModified)
    {
        _onModified = onModified;
        AllRecipes = new ObservableCollection<LockedRecipeViewModel>();
        FilteredRecipes = new ObservableCollection<LockedRecipeViewModel>();
    }
    
    /// <summary>
    /// All locked recipes loaded from the save file.
    /// </summary>
    public ObservableCollection<LockedRecipeViewModel> AllRecipes { get; }
    
    /// <summary>
    /// Filtered recipes based on search text.
    /// </summary>
    public ObservableCollection<LockedRecipeViewModel> FilteredRecipes { get; }
    
    /// <summary>
    /// Number of recipes marked for unlock.
    /// </summary>
    public int MarkedForUnlockCount => AllRecipes.Count(r => r.IsMarkedForUnlock);
    
    /// <summary>
    /// Total number of locked recipes.
    /// </summary>
    public int TotalRecipeCount => AllRecipes.Count;
    
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }
    
    /// <summary>
    /// Loads recipe data from the save file's JSON structure.
    /// </summary>
    public void LoadFromJson(JsonNode? root)
    {
        AllRecipes.Clear();
        FilteredRecipes.Clear();
        SelectedRecipe = null;
        _lockedRecipesNode = null;
        
        if (root == null)
        {
            StatusMessage = "No data loaded";
            return;
        }
        
        try
        {
            // Navigate to itemData.CrCraftingRecipeOwner.lockedRecipes
            var lockedRecipes = root["itemData"]?["CrCraftingRecipeOwner"]?["lockedRecipes"]?.AsObject();
            
            if (lockedRecipes == null)
            {
                StatusMessage = "No locked recipes found in save file";
                return;
            }
            
            _lockedRecipesNode = lockedRecipes;
            
            foreach (var kvp in lockedRecipes)
            {
                var recipe = new LockedRecipe
                {
                    RecipePath = kvp.Key
                };
                
                var itemsArray = kvp.Value?["items"]?.AsArray();
                if (itemsArray != null)
                {
                    foreach (var itemNode in itemsArray)
                    {
                        var recipeItem = new RecipeItem
                        {
                            ItemPath = itemNode?["item"]?.GetValue<string>() ?? string.Empty,
                            Count = itemNode?["count"]?.GetValue<int>() ?? 0
                        };
                        recipe.Items.Add(recipeItem);
                    }
                }
                
                var vm = new LockedRecipeViewModel(recipe, OnRecipeModified);
                AllRecipes.Add(vm);
            }
            
            // Sort by friendly name
            var sorted = AllRecipes.OrderBy(r => r.FriendlyName).ToList();
            AllRecipes.Clear();
            foreach (var r in sorted)
                AllRecipes.Add(r);
            
            ApplyFilter();
            
            StatusMessage = $"Loaded {AllRecipes.Count} locked recipes";
            OnPropertyChanged(nameof(TotalRecipeCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading recipes: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Applies modifications back to the JSON structure.
    /// </summary>
    public void ApplyToJson(JsonNode root)
    {
        if (_lockedRecipesNode == null) return;
        
        // Remove recipes marked for unlock
        var toRemove = AllRecipes.Where(r => r.IsMarkedForUnlock).ToList();
        foreach (var recipe in toRemove)
        {
            _lockedRecipesNode.Remove(recipe.RecipePath);
        }
        
        // Update item counts for remaining recipes
        foreach (var recipe in AllRecipes.Where(r => !r.IsMarkedForUnlock))
        {
            var recipeNode = _lockedRecipesNode[recipe.RecipePath];
            if (recipeNode == null) continue;
            
            var itemsArray = recipeNode["items"]?.AsArray();
            if (itemsArray == null) continue;
            
            for (int i = 0; i < itemsArray.Count && i < recipe.Items.Count; i++)
            {
                var itemNode = itemsArray[i]?.AsObject();
                if (itemNode != null)
                {
                    itemNode["count"] = JsonValue.Create(recipe.Items[i].Count);
                }
            }
        }
    }
    
    [RelayCommand]
    private void UnlockAll()
    {
        foreach (var recipe in AllRecipes)
        {
            recipe.IsMarkedForUnlock = true;
        }
        OnPropertyChanged(nameof(MarkedForUnlockCount));
        _onModified();
    }
    
    [RelayCommand]
    private void UnlockNone()
    {
        foreach (var recipe in AllRecipes)
        {
            recipe.IsMarkedForUnlock = false;
        }
        OnPropertyChanged(nameof(MarkedForUnlockCount));
        _onModified();
    }
    
    [RelayCommand]
    private void ZeroAllCounts()
    {
        if (SelectedRecipe == null) return;
        
        foreach (var item in SelectedRecipe.Items)
        {
            item.Count = 0;
        }
        SelectedRecipe.RefreshSummary();
    }
    
    private void ApplyFilter()
    {
        FilteredRecipes.Clear();
        
        var searchLower = SearchText.ToLowerInvariant();
        var filtered = string.IsNullOrWhiteSpace(searchLower)
            ? AllRecipes
            : AllRecipes.Where(r => 
                r.FriendlyName.ToLowerInvariant().Contains(searchLower) ||
                r.RecipeId.ToLowerInvariant().Contains(searchLower));
        
        foreach (var recipe in filtered)
        {
            FilteredRecipes.Add(recipe);
        }
    }
    
    private void OnRecipeModified()
    {
        OnPropertyChanged(nameof(MarkedForUnlockCount));
        _onModified();
    }
}
