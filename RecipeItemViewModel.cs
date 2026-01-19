using CommunityToolkit.Mvvm.ComponentModel;
//using ForeverSkiesSaveEditor.Models;

namespace ForeverSkiesSaveEditor.ViewModels;

/// <summary>
/// ViewModel for editing a single item requirement within a recipe.
/// </summary>
public partial class RecipeItemViewModel : ObservableObject
{
    private readonly RecipeItem _model;
    private readonly Action _onModified;
    
    [ObservableProperty]
    private int _count;
    
    public RecipeItemViewModel(RecipeItem model, Action onModified)
    {
        _model = model;
        _onModified = onModified;
        _count = model.Count;
    }
    
    public string ItemPath => _model.ItemPath;
    public string FriendlyName => _model.FriendlyName;
    public string ItemId => _model.ItemId;
    
    partial void OnCountChanged(int value)
    {
        _model.Count = value;
        _onModified();
    }
    
    /// <summary>
    /// Gets the underlying model for serialization.
    /// </summary>
    public RecipeItem GetModel() => _model;
}
