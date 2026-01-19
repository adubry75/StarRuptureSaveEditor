# Star Rupture Save Editor

A WPF desktop application for editing Star Rupture save files (.sav).

## Features

- **Load/Save .sav files**: Handles zlib-compressed JSON save format
- **Crafting Recipe Editor**: View and modify locked recipe requirements
  - Mark recipes as unlocked (removes from locked list)
  - Edit item count requirements
  - Search/filter recipes
  - Bulk unlock all recipes
- **Raw JSON Viewer**: Read-only view of the complete save structure
- **Round-trip safe**: Preserves unknown fields and data structure

## Usage

1. Open a .sav file from C:\Program Files (x86)\Steam\userdata\[your steam id]\1631270\remote\Saved\SaveGames\YourSaveFolderName\
2. Edit recipes in the Crafting tab
3. Save (creates automatic backup)

BE SURE YOU HAVE MADE YOUR OWN BACKUP OF YOUR SAVE FILES BEFORE PROCEEDING! Not responsible for any lost or corrupted data. This editor was made mostly by AI so no guarantees! :)

## Building

Requires .NET 8.0 SDK or later with Windows desktop workload.

```bash
dotnet build
dotnet run
```

Or open in Visual Studio 2022+ and build.

## Project Structure

```
StarRuptureSaveEditor/
├── Models/                  # Data models
│   ├── LockedRecipe.cs     # Recipe with item requirements
│   └── RecipeItem.cs       # Single item requirement
├── ViewModels/             # MVVM ViewModels
│   ├── ViewModelBase.cs    # Base class with INPC
│   ├── MainViewModel.cs    # App coordinator, file I/O
│   ├── CraftingEditorViewModel.cs  # Recipe editing logic
│   ├── LockedRecipeViewModel.cs    # Single recipe VM
│   ├── RecipeItemViewModel.cs      # Single item VM
│   └── RawJsonViewModel.cs         # JSON viewer
├── Views/                  # XAML views
│   ├── CraftingEditorView.xaml
│   └── RawJsonView.xaml
├── Services/               # Business logic
│   └── SaveFileService.cs  # Load/save .sav files
├── Converters/             # WPF value converters
│   └── ValueConverters.cs
├── MainWindow.xaml         # Main application window
└── App.xaml               # Application resources
```

## Save File Format

Star Rupture uses a simple format:
- **Header**: 4 bytes - uncompressed size (little-endian uint32)
- **Body**: zlib-compressed JSON

The JSON must be **minified** (no whitespace) when saving, or the game won't load it.

## Extending the Editor

### Adding a New Editor Tab

1. **Create the Model** (`Models/YourModel.cs`):
   ```csharp
   public class YourModel
   {
       public string SomeProperty { get; set; }
   }
   ```

2. **Create the ViewModel** (`ViewModels/YourEditorViewModel.cs`):
   ```csharp
   public partial class YourEditorViewModel : ViewModelBase
   {
       private readonly Action _onModified;
       
       public YourEditorViewModel(Action onModified)
       {
           _onModified = onModified;
       }
       
       public void LoadFromJson(JsonNode? root)
       {
           // Navigate to your data: root["itemData"]?["YourSubsystem"]
       }
       
       public void ApplyToJson(JsonNode root)
       {
           // Write modifications back to the JSON tree
       }
   }
   ```

3. **Create the View** (`Views/YourEditorView.xaml`):
   ```xml
   <UserControl x:Class="StarRuptureSaveEditor.Views.YourEditorView" ...>
       <!-- Your UI -->
   </UserControl>
   ```

4. **Register in MainViewModel**:
   ```csharp
   public YourEditorViewModel YourEditor { get; }
   
   // In constructor:
   YourEditor = new YourEditorViewModel(OnDataModified);
   
   // In LoadFile:
   YourEditor.LoadFromJson(_rootNode);
   
   // In SaveFile (before saving):
   YourEditor.ApplyToJson(_rootNode);
   ```

5. **Add tab in MainWindow.xaml**:
   ```xml
   <TabItem Header="Your Tab">
       <views:YourEditorView DataContext="{Binding YourEditor}"/>
   </TabItem>
   ```

### Key Data Locations in Save JSON

Based on analysis of the save file:

| Feature | JSON Path |
|---------|-----------|
| Crafting recipes | `itemData.CrCraftingRecipeOwner.lockedRecipes` |
| Picked up items | `itemData.CrCraftingRecipeOwner.pickedUpItems` |
| Building names | `itemData.CrBuildingCustomNameSubsystem.customNames` |
| World time | `worldTimeSeconds`, `worldRealTimeSeconds` |
| Current map | `level` |
| Save metadata | `timestamp`, `version`, `sessionName`, `saveName` |

### Tips for New Editors

1. **Use JsonNode**: Preserves structure and unknown fields
2. **Don't deserialize everything**: Only read/write what you need
3. **Call `_onModified()`**: When user changes anything
4. **Test round-trips**: Load → save → load should preserve data

## Dependencies

- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM infrastructure
- System.Text.Json - JSON parsing with JsonNode
- System.IO.Compression - zlib support

## License

Personal use only. Not affiliated with Star Rupture developers.
