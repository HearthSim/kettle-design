using NJsonSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JSONSchemaTest
{
    class CustomKettleRefResolver : JsonReferenceResolver
    {
        private readonly string _kettleRootSchemaPath;
        private readonly Dictionary<string, Task<JsonSchema4>> _loadedSchemas;

        public CustomKettleRefResolver(JsonSchemaResolver schemaResolver, string kettleRootSchemaPath) : base(schemaResolver)
        {
            _kettleRootSchemaPath = kettleRootSchemaPath;
            _loadedSchemas = new Dictionary<string, Task<JsonSchema4>>();
        }

        public override async Task<JsonSchema4> ResolveFileReferenceAsync(string filePath)
        {
            var kettlePrefix = "kettle:";
            var kettleIdx = filePath.IndexOf(kettlePrefix);
            if (kettleIdx != -1)
            {
                // Change filepath to properly identify schemas referenced by the kettle IRI.
                var kettlePath = filePath.Substring(kettleIdx + kettlePrefix.Length);
                filePath = Path.Combine(_kettleRootSchemaPath, kettlePath + ".json");
            }

            if (_loadedSchemas.ContainsKey(filePath))
            {
                // Wait for parsing to finish!
                return await _loadedSchemas[filePath].ConfigureAwait(false);
            }

            var loadTask = JsonSchema4.FromFileAsync(filePath, schema => this);
            _loadedSchemas[filePath] = loadTask;
            return await loadTask.ConfigureAwait(false);
        }
    }
}
