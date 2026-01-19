using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ForeverSkiesSaveEditor.ViewModels;

/// <summary>
/// ViewModel for the Raw JSON viewer tab.
/// Displays the save file as formatted JSON (read-only).
/// </summary>
public partial class RawJsonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _jsonText = string.Empty;
    
    [ObservableProperty]
    private string _statusMessage = "No file loaded";
    
    /// <summary>
    /// Loads and formats the JSON for display.
    /// </summary>
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
            
            // Calculate some stats
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
