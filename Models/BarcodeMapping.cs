namespace BarcodePrinter.Models;

public class BarcodeMapping
{
    public string ModelName { get; set; } = "";
    public string Barcode1 { get; set; } = "";
    public string Barcode2 { get; set; } = "";
    public string QrData { get; set; } = "";
}

public class MappingData
{
    public List<BarcodeMapping> Mappings { get; set; } = [];
}
