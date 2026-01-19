using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
//using StarRuptureSaveEditor.Models;

namespace StarRuptureSaveEditor.ViewModels;

/// <summary>
/// ViewModel for a single locked recipe, allowing editing of its item requirements.
/// </summary>
public partial class LockedRecipeViewModel : ObservableObject
{
    private readonly LockedRecipe _model;
    private readonly Action _onModified;
    
    [ObservableProperty]
    private bool _isMarkedForUnlock;
    
    public LockedRecipeViewModel(LockedRecipe model, Action onModified)
    {
        _model = model;
        _onModified = onModified;
        
        Items = new ObservableCollection<RecipeItemViewModel>(
            model.Items.Select(item => new RecipeItemViewModel(item, onModified))
        );
    }
    
    public string RecipePath => _model.RecipePath;
    public string FriendlyName => _model.FriendlyName;
    public string RecipeId => _model.RecipeId;
    
    public ObservableCollection<RecipeItemViewModel> Items { get; }
    
    /// <summary>
    /// Summary of items required (for display in list)
    /// </summary>
    public string ItemsSummary
    {
        get
        {
            var nonZeroItems = Items.Where(i => i.Count > 0).ToList();
            if (nonZeroItems.Count == 0)
                return "No items required";
            
            return string.Join(", ", nonZeroItems.Select(i => $"{i.FriendlyName}: {i.Count}"));
        }
    }
    
    partial void OnIsMarkedForUnlockChanged(bool value)
    {
        _onModified();
    }
    
    /// <summary>
    /// Gets the underlying model for serialization.
    /// </summary>
    public LockedRecipe GetModel() => _model;
    
    /// <summary>
    /// Refreshes the items summary when counts change.
    /// </summary>
    public void RefreshSummary()
    {
        OnPropertyChanged(nameof(ItemsSummary));
    }
}
