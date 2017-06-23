using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using System;
using System.IO;

namespace JSONSchemaTest
{
	class Program
	{
		static void Main(string[] args)
		{
			Process();
		}

		static void Process()
		{
			// Main payload to convert into C# code
			var schemaPath = @"D:\Go\src\github.com\HearthSim\kettle-design\schemas\types\payload.json";

			// Base directory of all kettle schemas
			var referenceDir = @"D:\Go\src\github.com\HearthSim\kettle-design\schemas\";
			Func<JsonSchema4, JsonReferenceResolver> resolveFactory = s =>
			new CustomKettleRefResolver(new JsonSchemaResolver(s, new JsonSchemaGeneratorSettings()), referenceDir);

			// Blocks until completely parsed..
			var schema = JsonSchema4.FromFileAsync(schemaPath, resolveFactory).Result;

			// Build the CS file for our main payload
			var nameResolver = new CustomKettleTypeNameResolver();
			var generatorSettings = new CSharpGeneratorSettings() { TypeNameGenerator = nameResolver };
			var generator = new CSharpGenerator(schema, generatorSettings);
			generator.Settings.Namespace = "Kettle.Protocol";
			var fileContents = generator.GenerateFile("Payload");

			File.WriteAllText(@"D:\Go\src\github.com\HearthSim\kettle-design\csharp\generated\payloads.cs", fileContents);

			return;
		}
	}
}
