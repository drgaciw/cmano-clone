#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ProjectAegis.Unity.Tests
{
    /// <summary>Exercises the converter binding that previously broke the MCP handshake.</summary>
    public sealed class McpSerializerBindingTests
    {
        [TestCase("UNITY_MCP_READY;UNITY_MCP_DEPS_4;APP_UI_EDITOR_ONLY",
            "UNITY_MCP_DEPS_4;APP_UI_EDITOR_ONLY", true)]
        [TestCase("UNITY_MCP_READY;UNITY_MCP_READY", "", true)]
        [TestCase("UNITY_MCP_DEPS_4;NOT_UNITY_MCP_READY",
            "UNITY_MCP_DEPS_4;NOT_UNITY_MCP_READY", false)]
        [TestCase("", "", false)]
        public void Player_build_filter_removes_only_ready_define(
            string input, string expected, bool removed)
        {
            var isolation = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("ProjectAegis.Unity.Editor.McpPlayerBuildIsolation"))
                .Single(t => t != null)!;
            var filter = isolation.GetMethod("RemoveReadyDefine", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(filter, Is.Not.Null, "The player build hook must use the tested define filter.");
            var defines = new List<string>(input.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            Assert.That(filter!.Invoke(null, new object[] { defines }), Is.EqualTo(removed));
            Assert.That(string.Join(";", defines), Is.EqualTo(expected));
        }

        [Test]
        public void Server_transport_converter_matches_the_loaded_serializer()
        {
            var common = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(a => a.GetName().Name == "McpPlugin.Common");
            if (common == null)
                Assert.Ignore("MCP is not loaded in this Editor.");

            var serverData = common!.GetType("com.IvanMurzak.McpPlugin.Common.Model.McpServerData", true)!;
            var attribute = serverData.GetProperty("ServerTransport")!.GetCustomAttributesData()
                .Single(a => a.AttributeType.Name == "JsonConverterAttribute");
            var converter = (Type)attribute.ConstructorArguments[0].Value!;
            var serializers = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name == "System.Text.Json").ToArray();

            Assert.That(serializers.Length, Is.EqualTo(1),
                "Multiple System.Text.Json identities split MCP converters from the serializer.");
            Assert.That(serializers[0].GetType("System.Text.Json.Serialization.JsonConverter")!
                .IsAssignableFrom(converter), Is.True, converter.AssemblyQualifiedName);
        }
    }
}
#endif
