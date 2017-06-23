using NJsonSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSONSchemaTest
{
    class CustomKettleTypeNameResolver : DefaultTypeNameGenerator
    {
        public override string Generate(JsonSchema4 schema, string typeNameHint, IEnumerable<string> reservedTypeNames)
        {
            var title = schema.Title;

            if (title == null || title.Length == 0)
            {
                title = base.Generate(schema, typeNameHint, reservedTypeNames);
            }
            else
            {
                if (reservedTypeNames.Contains(title))
                {
                    var count = 1;
                    do
                    {
                        count++;
                    } while (reservedTypeNames.Contains(title + count));
                    title = title + count;
                }
            }

            return title;
        }
    }
}
