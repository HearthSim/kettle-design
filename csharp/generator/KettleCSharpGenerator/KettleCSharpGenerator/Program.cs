using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NJsonSchema.Generation;
using System;
using System.IO;

namespace KettleCSharpGenerator
{
	class Program
	{
		public static void PrintHelp()
		{
			Console.WriteLine("CSharp code generator tool");
			Console.WriteLine("--------------------------");
			Console.WriteLine();
			Console.WriteLine("Usage: [exe] [kettle schemas root folder] [path to write generated file to]");
			Console.WriteLine();
			Console.WriteLine("Example: KettleCSharpGenerator 'C:\\kettle\\' 'C:\\kettle-code\\csharp\\protocol.cs");
		}

		static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				PrintHelp();
				return;
			}

			var kettleRoot = args[0];
			var protocolOut = args[1];

			if (!Directory.Exists(kettleRoot))
			{
				Console.Error.WriteLine("The provided path does not exist on your computer!");
				Console.Error.WriteLine("`{0}`", kettleRoot);
				return;
			}

			var payloadSchemaPath = Path.Combine(kettleRoot, Path.Combine("types", "payload.json"));
			if (!File.Exists(payloadSchemaPath))
			{
				Console.Error.WriteLine("The schema file `payload.json` is not found, make sure the Kettle root director is correct!");
				return;
			}

			var outParentFolder = Path.GetDirectoryName(protocolOut);
			if (!Directory.Exists(outParentFolder))
			{
				try
				{
					// Create the folder structure up until the parent directory of the target file.
					Directory.CreateDirectory(outParentFolder);
				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e.Message);
					return;
				}
			}

			Process(payloadSchemaPath, kettleRoot, protocolOut);
			Console.WriteLine("OK");
		}

		static void Process(string schemaPath, string referenceDir, string outPath)
		{
			Func<JsonSchema4, JsonReferenceResolver> resolveFactory = s =>
			new CustomKettleRefResolver(new JsonSchemaResolver(s, new JsonSchemaGeneratorSettings()), referenceDir);

			// Blocks until completely parsed..
			var schema = JsonSchema4.FromFileAsync(schemaPath, resolveFactory).Result;

			// Build the CS file for our main payload
			var nameResolver = new CustomKettleTypeNameResolver();
			var generatorSettings = new CSharpGeneratorSettings() { TypeNameGenerator = nameResolver, ClassStyle = CSharpClassStyle.Poco };
			var generator = new CSharpGenerator(schema, generatorSettings);
			generator.Settings.Namespace = "Kettle.Protocol";
			var fileContents = generator.GenerateFile("Payload");

			File.WriteAllText(outPath, fileContents);

			return;
		}
	}
}
