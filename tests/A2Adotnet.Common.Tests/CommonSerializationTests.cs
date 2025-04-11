using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using A2Adotnet.Common.Models; // Assuming models are here
using A2Adotnet.Common.Protocol.Messages; // Assuming messages are here

namespace A2Adotnet.Common.Tests;

[TestClass]
public class CommonSerializationTests
{
    private JsonSerializerOptions _options = null!;

    [TestInitialize]
    public void Setup()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { new RequestIdConverter() /* Add Part converter if needed */ }
            // Add Part polymorphism handling if using custom converter
        };
        // If using JsonDerivedType, it should work automatically with default options or these options.
    }

    [TestMethod]
    public void TestRequestIdSerialization_String()
    {
        RequestId id = "req-123";
        var json = JsonSerializer.Serialize(id, _options);
        Assert.AreEqual("\"req-123\"", json);
    }

     [TestMethod]
    public void TestRequestIdSerialization_Long()
    {
        RequestId id = 12345L;
        var json = JsonSerializer.Serialize(id, _options);
        Assert.AreEqual("12345", json);
    }

    [TestMethod]
    public void TestRequestIdDeserialization_String()
    {
        var json = "\"req-abc\"";
        var id = JsonSerializer.Deserialize<RequestId>(json, _options);
        Assert.AreEqual("req-abc", id.StringValue);
        Assert.IsNull(id.LongValue);
    }

     [TestMethod]
    public void TestRequestIdDeserialization_Long()
    {
        var json = "98765";
        var id = JsonSerializer.Deserialize<RequestId>(json, _options);
        Assert.AreEqual(98765L, id.LongValue);
        Assert.IsNull(id.StringValue);
    }

    // TODO: Add tests for AgentCard serialization/deserialization
    // TODO: Add tests for Task serialization/deserialization
    // TODO: Add tests for Part polymorphism serialization/deserialization (Text, File, Data)
    // TODO: Add tests for other common models
}