using System.IO;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
//using StarRuptureSaveEditor.Services;
using Microsoft.Win32;

namespace StarRuptureSaveEditor.ViewModels;

/// <summary>
/// Main ViewModel that coordinates the application.
/// Handles file operations and manages child editor ViewModels.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SaveFileService _saveFileService;
    private JsonNode? _rootNode;
    private string? _currentFilePath;
    
    [ObservableProperty]
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
        RawJsonViewer = new RawJsonViewModel();
    }
    
    /// <summary>
    /// The crafting recipe editor.
    /// </summary>
    public CraftingEditorViewModel CraftingEditor { get; }
    
    /// <summary>
    /// The raw JSON viewer.
    /// </summary>
    public RawJsonViewModel RawJsonViewer { get; }
    
    /// <summary>
    /// The currently loaded file name (for display).
    /// </summary>
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
        
        // Try to default to the Star Rupture save location
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
            // Apply all modifications to the JSON tree
            CraftingEditor.ApplyToJson(_rootNode);
            
            // Create backup
            string backupPath = _currentFilePath + ".backup";
            if (File.Exists(_currentFilePath))
            {
                File.Copy(_currentFilePath, backupPath, overwrite: true);
            }
            
            // Save the file
            _saveFileService.SaveToFile(_rootNode, _currentFilePath);
            
            HasUnsavedChanges = false;
            StatusMessage = $"Saved to {CurrentFileName} (backup created)";
            UpdateWindowTitle();
            
            // Reload to reflect changes (e.g., removed recipes)
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
            
            // Load data into editors
            CraftingEditor.LoadFromJson(_rootNode);
            RawJsonViewer.LoadFromJson(_rootNode);
            
            IsFileLoaded = true;
            HasUnsavedChanges = false;
            UpdateWindowTitle();
            OnPropertyChanged(nameof(CurrentFileName));
            
            StatusMessage = $"Loaded {CurrentFileName}";
            
            // Switch to crafting tab
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
