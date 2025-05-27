using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using UAssetAPI;
using UAssetAPI.JSON;
using UAssetAPI.UnrealTypes;

UAsset asset = new("input.umap", EngineVersion.VER_UE5_3);

Dictionary<FName, string> toBeFilled = new Dictionary<FName, string>();
var serializer = JsonSerializer.Create(new JsonSerializerSettings
{
    ContractResolver = new OverrideFPackageIndexResolver(toBeFilled, asset),
    TypeNameHandling = TypeNameHandling.None,
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.None,
    FloatParseHandling = FloatParseHandling.Double,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
    Converters = new List<JsonConverter>()
    {
        //new FSignedZeroJsonConverter(),
        //new FNameJsonConverter(null),
        //new FStringTableJsonConverter(),
        //new FStringJsonConverter(),
        new FPackageIndexJsonConverter(),
        //new StringEnumConverter(),
        //new GuidJsonConverter(),
        //new ByteArrayJsonConverter()
    },
    Error = (sender, args) =>
    {
        // Skip the problematic member
        //Console.WriteLine($"Skipping error: {args.ErrorContext.Error.Message}");
        args.ErrorContext.Handled = true;
    }
});
var stopwatch = Stopwatch.StartNew();  // Start timing
JToken token = JToken.FromObject(asset, serializer);
//var deserialized = token.ToObject(export.GetType(), serializer);
stopwatch.Stop();  // Stop timing
var export = asset.Exports[0];
Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms. Exports count: {asset.Exports.Count}");
Console.WriteLine(export.OuterIndex);
Console.WriteLine(export.SuperIndex);

public class CustomFPackageIndexJsonConverter : JsonConverter
{
    public UAsset CurrentAsset;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(FPackageIndex);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is FPackageIndex pindex)
        {
            pindex.Index = 0;
        }
        //writer.WriteValue((value as FPackageIndex).Index);
        //writer.WriteValue(0);
        writer.WriteNull();
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        //Console.WriteLine($"hacking fucking FPackageIndex :D {CurrentAsset.FilePath} {Convert.ToInt32(reader.Value)}");
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
