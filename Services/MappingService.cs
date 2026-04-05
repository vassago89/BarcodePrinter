using System.IO;
using System.Text.Json;
using BarcodePrinter.Models;

namespace BarcodePrinter.Services;

public class MappingService
{
    private readonly string _filePath;
    private MappingData _data = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<BarcodeMapping> Mappings => _data.Mappings;

    public MappingService(string filePath)
    {
        _filePath = filePath;
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            _data = new MappingData();
            Save();
            return;
        }

        string json = File.ReadAllText(_filePath);
        _data = JsonSerializer.Deserialize<MappingData>(json, JsonOptions) ?? new MappingData();
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(_data, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    public List<string> GetAllModelNames()
        => _data.Mappings.Select(m => m.ModelName).Distinct().ToList();

    public BarcodeMapping? GetByModel(string modelName)
        => _data.Mappings.FirstOrDefault(m =>
            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));

    public void Add(BarcodeMapping mapping)
    {
        _data.Mappings.Add(mapping);
        Save();
    }

    public bool Remove(string modelName)
    {
        var item = _data.Mappings.FirstOrDefault(m =>
            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        if (item == null) return false;
        _data.Mappings.Remove(item);
        Save();
        return true;
    }
}
