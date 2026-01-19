using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ForeverSkiesSaveEditor;

/// <summary>
/// Handles loading and saving .sav files (zlib-compressed JSON with 4-byte size header)
/// </summary>
public class SaveFileService
{
    /// <summary>
    /// Loads a .sav file and returns the JSON as a mutable JsonNode tree.
    /// </summary>
    public JsonNode? LoadSaveFile(string filePath)
    {
        byte[] rawData = File.ReadAllBytes(filePath);
        
        if (rawData.Length < 6)
            throw new InvalidDataException("File too small to be a valid save file");
        
        if (rawData[4] != 0x78)
            throw new InvalidDataException("Invalid save file format - expected zlib compression");
        
        uint expectedSize = BitConverter.ToUInt32(rawData, 0);
        
        byte[] compressedData = new byte[rawData.Length - 4];
        Array.Copy(rawData, 4, compressedData, 0, compressedData.Length);
        
        byte[] decompressed = DecompressZlib(compressedData);
        
        if (decompressed.Length != expectedSize)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Expected {expectedSize} bytes, got {decompressed.Length} bytes");
        }
        
        string json = Encoding.UTF8.GetString(decompressed);
        
        return JsonNode.Parse(json);
    }
    
    /// <summary>
    /// Saves a JsonNode tree back to a .sav file.
    /// The JSON is minified before compression (required by the game).
    /// </summary>
    public void SaveToFile(JsonNode root, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        string json = root.ToJsonString(options);
        byte[] uncompressedData = Encoding.UTF8.GetBytes(json);
        
        byte[] compressedData = CompressZlib(uncompressedData);
        
        using var outputStream = File.Create(filePath);
        
        byte[] sizeHeader = BitConverter.GetBytes((uint)uncompressedData.Length);
        outputStream.Write(sizeHeader, 0, 4);
        outputStream.Write(compressedData, 0, compressedData.Length);
    }
    
    /// <summary>
    /// Exports the current state as pretty-printed JSON for debugging/viewing.
    /// </summary>
    public void ExportAsJson(JsonNode root, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        string json = root.ToJsonString(options);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
    
    /// <summary>
    /// Gets the raw JSON string (pretty-printed) for display purposes.
    /// </summary>
    public string GetPrettyJson(JsonNode root)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        return root.ToJsonString(options);
    }
    
    private byte[] DecompressZlib(byte[] compressed)
    {
        using var inputStream = new MemoryStream(compressed);
        using var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        zlibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }
    
    private byte[] CompressZlib(byte[] uncompressed)
    {
        using var outputStream = new MemoryStream();
        using (var zlibStream = new ZLibStream(outputStream, CompressionLevel.Optimal))
        {
            zlibStream.Write(uncompressed, 0, uncompressed.Length);
        }
        return outputStream.ToArray();
    }
}
