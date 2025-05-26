using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.JSON;
using UAssetAPI.UnrealTypes;

UAsset asset = new("input.umap", UAssetAPI.UnrealTypes.EngineVersion.VER_UE5_3);

var stopwatch = Stopwatch.StartNew();  // Start timing
var export = asset.Exports[0];
Dictionary<FName, string> toBeFilled = new Dictionary<FName, string>();
var serializer = JsonSerializer.Create(new JsonSerializerSettings
{
    ContractResolver = new OverrideFPackageIndexResolver(toBeFilled, asset),
    TypeNameHandling = TypeNameHandling.Objects,
    NullValueHandling = NullValueHandling.Include,
    FloatParseHandling = FloatParseHandling.Double,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
    Converters = new List<JsonConverter>()
    {
        new FSignedZeroJsonConverter(),
        new FNameJsonConverter(null),
        new FStringTableJsonConverter(),
        new FStringJsonConverter(),
        new FPackageIndexJsonConverter(),
        new StringEnumConverter(),
        new GuidJsonConverter(),
        new ByteArrayJsonConverter()
    }
    //Error = (sender, args) =>
    //{
    //    // Skip the problematic member
    //    Console.WriteLine($"Skipping error: {args.ErrorContext.Error.Message}");
    //    args.ErrorContext.Handled = true;
    //}
});
JToken token = JToken.FromObject(export, serializer);
var deserialized = token.ToObject<Export>(serializer);
stopwatch.Stop();  // Stop timing
Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");

public class CustomFPackageIndexJsonConverter : JsonConverter
{
    public UAsset CurrentAsset;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(FPackageIndex);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer.WriteValue((value as FPackageIndex).Index);
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        Console.WriteLine($"hacking fucking FPackageIndex :D {CurrentAsset.FilePath}");
        return new FPackageIndex(Convert.ToInt32(reader.Value));
    }

    public CustomFPackageIndexJsonConverter(UAsset asset) : base()
    {
        CurrentAsset = asset;
    }
}

public class OverrideFPackageIndexResolver : DefaultContractResolver
{
    public UAsset CurrentAsset;

    protected override JsonContract CreateContract(Type objectType)
    {
        JsonContract contract = base.CreateContract(objectType);
        if (objectType == typeof(FPackageIndex))
        {
            contract.Converter = new CustomFPackageIndexJsonConverter(CurrentAsset);
        }
        return contract;
    }

    public Dictionary<FName, string> ToBeFilled;

    protected override JsonConverter ResolveContractConverter(Type objectType)
    {
        if (typeof(FName).IsAssignableFrom(objectType))
        {
            return new FNameJsonConverter(ToBeFilled);
        }
        return base.ResolveContractConverter(objectType);
    }

    public OverrideFPackageIndexResolver(Dictionary<FName, string> toBeFilled, UAsset currentAsset) : base()
    {
        ToBeFilled = toBeFilled;
        CurrentAsset = currentAsset;
    }
}
