using System.Collections.Generic;
using TableService.Core.Models;

namespace TableService.Core.Utility
{
    public static class TableExtensions
    {
        public static List<FieldDefinition> ToFieldDefinitions(this Table table)
        {
            string[] fieldNameParts = table.FieldNames.Split(",");
            string[] fieldTypeParts = table.FieldTypes.Split(",");

            if (fieldNameParts.Length != fieldTypeParts.Length)
            {
                throw new InvalidTableException(table);
            }

            var fieldDefinitions = new List<FieldDefinition>();
            for (var i = 0; i < fieldNameParts.Length; i++)
            {
                var fieldDefinition = new FieldDefinition
                {
                    FieldName = fieldNameParts[i],
                    FieldType = fieldTypeParts[i]
                };
                fieldDefinitions.Add(fieldDefinition);
            }

            fieldDefinitions.Insert(0, new FieldDefinition { FieldName = "Id", FieldType = "number" });
            fieldDefinitions.Add(new FieldDefinition { FieldName = "CreatedUserName", FieldType = "string" });
            fieldDefinitions.Add(new FieldDefinition { FieldName = "CreatedAt", FieldType = "datetime" });
            fieldDefinitions.Add(new FieldDefinition { FieldName = "UpdatedUserName", FieldType = "string" });
            fieldDefinitions.Add(new FieldDefinition { FieldName = "UpdatedAt", FieldType = "datetime" });

            return fieldDefinitions;
        }
    }
}
