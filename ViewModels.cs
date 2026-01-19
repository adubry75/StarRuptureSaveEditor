using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace StarRuptureSaveEditor;

/// <summary>
/// ViewModel for editing a single inventory item.
/// </summary>
public partial class InventoryItemViewModel : ObservableObject
{
    private readonly InventoryItem _model;
    private readonly Action _onModified;

    [ObservableProperty]
    private int _amount;

    public InventoryItemViewModel(InventoryItem model, Action onModified)
    {
        _model = model;
        _onModified = onModified;
        _amount = model.Amount;
    }

    public string Handle => _model.Handle;
    public string ItemData => _model.ItemData;
    public string FriendlyName => _model.FriendlyName;
    public string ItemId => _model.ItemId;
    public string Category => _model.Category;
    public int ArrayIndex => _model.ArrayIndex;

    partial void OnAmountChanged(int value)
    {
        _model.Amount = value;
        _onModified();
    }

    public InventoryItem GetModel() => _model;
}

/// <summary>
/// ViewModel for the Inventory editor tab.
/// </summary>
public partial class InventoryEditorViewModel : ObservableObject
{
    private readonly Action _onModified;
    private JsonArray? _itemsArrayNode;
    private string? _playerId;

    [ObservableProperty]
    private InventoryItemViewModel? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    public InventoryEditorViewModel(Action onModified)
    {
        _onModified = onModified;
        AllItems = new ObservableCollection<InventoryItemViewModel>();
        FilteredItems = new ObservableCollection<InventoryItemViewModel>();
        Categories = new ObservableCollection<string> { "All" };
    }

    public ObservableCollection<InventoryItemViewModel> AllItems { get; }
    public ObservableCollection<InventoryItemViewModel> FilteredItems { get; }
    public ObservableCollection<string> Categories { get; }

    public int TotalItemCount => AllItems.Count;
    public int TotalQuantity => AllItems.Sum(i => i.Amount);

    public string? PlayerId => _playerId;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilter();
    }

    public void LoadFromJson(JsonNode? root)
    {
        AllItems.Clear();
        FilteredItems.Clear();
        Categories.Clear();
        Categories.Add("All");
        SelectedItem = null;
        _itemsArrayNode = null;
        _playerId = null;

        if (root == null)
        {
            StatusMessage = "No data loaded";
            return;
        }

        try
        {
            // Navigate to the inventory path
            var allPlayersSaveData = root["itemData"]?["GameStateData"]?["allCharactersBaseSaveData"]?["allPlayersSaveData"]?.AsObject();

            if (allPlayersSaveData == null || allPlayersSaveData.Count == 0)
            {
                StatusMessage = "No player data found in save file";
                return;
            }

            // Get first player's ID and inventory
            var firstPlayer = allPlayersSaveData.First();
            _playerId = firstPlayer.Key;

            var itemsArray = firstPlayer.Value?["itemsStoreState"]?["itemsArray"]?.AsArray();

            if (itemsArray == null)
            {
                StatusMessage = "No inventory found for player";
                return;
            }

            _itemsArrayNode = itemsArray;

            var categorySet = new HashSet<string>();

            for (int i = 0; i < itemsArray.Count; i++)
            {
                var itemNode = itemsArray[i];
                if (itemNode == null) continue;

                var item = new InventoryItem
                {
                    Handle = itemNode["handle"]?["handle"]?.GetValue<string>() ?? string.Empty,
                    ItemData = itemNode["itemData"]?.GetValue<string>() ?? string.Empty,
                    Amount = itemNode["amount"]?.GetValue<int>() ?? 0,
                    ArrayIndex = i
                };

                var vm = new InventoryItemViewModel(item, OnItemModified);
                AllItems.Add(vm);
                categorySet.Add(vm.Category);
            }

            // Sort by friendly name
            var sorted = AllItems.OrderBy(i => i.FriendlyName).ToList();
            AllItems.Clear();
            foreach (var item in sorted)
                AllItems.Add(item);

            // Add categories
            foreach (var cat in categorySet.OrderBy(c => c))
            {
                Categories.Add(cat);
            }

            ApplyFilter();

            StatusMessage = $"Loaded {AllItems.Count} items (Player: {_playerId})";
            OnPropertyChanged(nameof(TotalItemCount));
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(PlayerId));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading inventory: {ex.Message}";
        }
    }

    public void ApplyToJson(JsonNode root)
    {
        if (_itemsArrayNode == null) return;

        foreach (var item in AllItems)
        {
            var index = item.ArrayIndex;
            if (index >= 0 && index < _itemsArrayNode.Count)
            {
                var itemNode = _itemsArrayNode[index]?.AsObject();
                if (itemNode != null)
                {
                    itemNode["amount"] = JsonValue.Create(item.Amount);
                }
            }
        }
    }

    [RelayCommand]
    private void SetAllAmounts(string amountStr)
    {
        if (!int.TryParse(amountStr, out int amount)) return;

        foreach (var item in FilteredItems)
        {
            item.Amount = amount;
        }
        OnPropertyChanged(nameof(TotalQuantity));
    }

    [RelayCommand]
    private void MultiplyAllAmounts(string multiplierStr)
    {
        if (!double.TryParse(multiplierStr, out double multiplier)) return;

        foreach (var item in FilteredItems)
        {
            item.Amount = (int)(item.Amount * multiplier);
        }
        OnPropertyChanged(nameof(TotalQuantity));
    }

    [RelayCommand]
    private void MaxSelectedItem()
    {
        if (SelectedItem != null)
        {
            SelectedItem.Amount = 9999;
            OnPropertyChanged(nameof(TotalQuantity));
        }
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();

        var searchLower = SearchText.ToLowerInvariant();
        var filtered = AllItems.AsEnumerable();

        // Filter by category
        if (SelectedCategory != "All")
        {
            filtered = filtered.Where(i => i.Category == SelectedCategory);
        }

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(searchLower))
        {
            filtered = filtered.Where(i =>
                i.FriendlyName.ToLowerInvariant().Contains(searchLower) ||
                i.ItemId.ToLowerInvariant().Contains(searchLower));
        }

        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }
    }

    private void OnItemModified()
    {
        OnPropertyChanged(nameof(TotalQuantity));
        _onModified();
    }
}

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

    public RecipeItem GetModel() => _model;
}

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

    public LockedRecipe GetModel() => _model;

    public void RefreshSummary()
    {
        OnPropertyChanged(nameof(ItemsSummary));
    }
}

/// <summary>
/// ViewModel for the Crafting Recipe editor tab.
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

    public ObservableCollection<LockedRecipeViewModel> AllRecipes { get; }
    public ObservableCollection<LockedRecipeViewModel> FilteredRecipes { get; }

    public int MarkedForUnlockCount => AllRecipes.Count(r => r.IsMarkedForUnlock);
    public int TotalRecipeCount => AllRecipes.Count;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

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

    public void ApplyToJson(JsonNode root)
    {
        if (_lockedRecipesNode == null) return;

        var toRemove = AllRecipes.Where(r => r.IsMarkedForUnlock).ToList();
        foreach (var recipe in toRemove)
        {
            _lockedRecipesNode.Remove(recipe.RecipePath);
        }

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

/// <summary>
/// ViewModel for the Raw JSON viewer tab.
/// </summary>
public partial class RawJsonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _jsonText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "No file loaded";

    public void LoadFromJson(JsonNode? root)
    {
        if (root == null)
        {
            JsonText = string.Empty;
            StatusMessage = "No data loaded";
            return;
        }

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            JsonText = root.ToJsonString(options);

            int lineCount = JsonText.Split('\n').Length;
            StatusMessage = $"{lineCount:N0} lines, {JsonText.Length:N0} characters";
        }
        catch (Exception ex)
        {
            JsonText = $"Error formatting JSON: {ex.Message}";
            StatusMessage = "Error";
        }
    }
}

/// <summary>
/// Main ViewModel that coordinates the application.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SaveFileService _saveFileService;
    private JsonNode? _rootNode;
    private string? _currentFilePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileAsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportJsonCommand))]
    private bool _isFileLoaded;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private string _windowTitle = "Star Rupture Save Editor";

    [ObservableProperty]
    private string _statusMessage = "Ready. Open a .sav file to begin.";

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel()
    {
        _saveFileService = new SaveFileService();
        CraftingEditor = new CraftingEditorViewModel(OnDataModified);
        InventoryEditor = new InventoryEditorViewModel(OnDataModified);
        RawJsonViewer = new RawJsonViewModel();
    }

    public CraftingEditorViewModel CraftingEditor { get; }
    public InventoryEditorViewModel InventoryEditor { get; }
    public RawJsonViewModel RawJsonViewer { get; }

    public string CurrentFileName =>
        string.IsNullOrEmpty(_currentFilePath) ? "No file loaded" : Path.GetFileName(_currentFilePath);

    [RelayCommand]
    private void OpenFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Save File",
            Filter = "Save Files (*.sav)|*.sav|All Files (*.*)|*.*",
            DefaultExt = ".sav"
        };

        string defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StarRupture", "Saved", "SaveGames");

        if (Directory.Exists(defaultPath))
        {
            dialog.InitialDirectory = defaultPath;
        }

        if (dialog.ShowDialog() == true)
        {
            LoadFile(dialog.FileName);
        }
    }

    [RelayCommand(CanExecute = nameof(IsFileLoaded))]
    private void SaveFile()
    {
        if (_rootNode == null || string.IsNullOrEmpty(_currentFilePath))
            return;

        try
        {
            CraftingEditor.ApplyToJson(_rootNode);
            InventoryEditor.ApplyToJson(_rootNode);

            string backupPath = _currentFilePath + ".backup";
            if (File.Exists(_currentFilePath))
            {
                File.Copy(_currentFilePath, backupPath, overwrite: true);
            }

            _saveFileService.SaveToFile(_rootNode, _currentFilePath);

            HasUnsavedChanges = false;
            StatusMessage = $"Saved to {CurrentFileName} (backup created)";
            UpdateWindowTitle();

            LoadFile(_currentFilePath);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to save file:\n\n{ex.Message}",
                "Save Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(IsFileLoaded))]
    private void SaveFileAs()
    {
        if (_rootNode == null)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Save As",
            Filter = "Save Files (*.sav)|*.sav|All Files (*.*)|*.*",
            DefaultExt = ".sav",
            FileName = CurrentFileName
        };

        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_currentFilePath);
        }

        if (dialog.ShowDialog() == true)
        {
            try
            {
                CraftingEditor.ApplyToJson(_rootNode);
                InventoryEditor.ApplyToJson(_rootNode);
                _saveFileService.SaveToFile(_rootNode, dialog.FileName);

                _currentFilePath = dialog.FileName;
                HasUnsavedChanges = false;
                StatusMessage = $"Saved to {CurrentFileName}";
                UpdateWindowTitle();
                OnPropertyChanged(nameof(CurrentFileName));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to save file:\n\n{ex.Message}",
                    "Save Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(IsFileLoaded))]
    private void ExportJson()
    {
        if (_rootNode == null)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Export as JSON",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                CraftingEditor.ApplyToJson(_rootNode);
                InventoryEditor.ApplyToJson(_rootNode);
                _saveFileService.ExportAsJson(_rootNode, dialog.FileName);
                StatusMessage = $"Exported to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (HasUnsavedChanges)
        {
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Save before exiting?",
                "Unsaved Changes",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                SaveFile();
            }
            else if (result == System.Windows.MessageBoxResult.Cancel)
            {
                return;
            }
        }

        System.Windows.Application.Current.Shutdown();
    }

    private void LoadFile(string filePath)
    {
        try
        {
            StatusMessage = "Loading...";

            _rootNode = _saveFileService.LoadSaveFile(filePath);
            _currentFilePath = filePath;

            CraftingEditor.LoadFromJson(_rootNode);
            InventoryEditor.LoadFromJson(_rootNode);
            RawJsonViewer.LoadFromJson(_rootNode);

            IsFileLoaded = true;
            HasUnsavedChanges = false;
            UpdateWindowTitle();
            OnPropertyChanged(nameof(CurrentFileName));

            StatusMessage = $"Loaded {CurrentFileName}";

            SelectedTabIndex = 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            IsFileLoaded = false;
            _rootNode = null;
            _currentFilePath = null;

            System.Windows.MessageBox.Show(
                $"Failed to load save file:\n\n{ex.Message}",
                "Load Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnDataModified()
    {
        HasUnsavedChanges = true;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        string title = "Star Rupture Save Editor";

        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            title = $"{CurrentFileName} - {title}";
        }

        if (HasUnsavedChanges)
        {
            title = "* " + title;
        }

        WindowTitle = title;
    }
}
