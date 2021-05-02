using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TableService.Core.Models;

namespace TableService.Core.Extensions
{
    public static class TableExtensions
    {
        public static string[] GetTableFields(this Table table)
        {
            return table.FieldNames.ToLower().Split(",");
        }

        public static string[] GetTableFieldTypes(this Table table)
        {
            return table.FieldTypes.ToLower().Split(",");
        }

        public static Dictionary<string, string> GetTableSchema(this Table table)
        {
            Dictionary<string, string> schema = new Dictionary<string, string>();

            var fieldNames = table.FieldNames.Split(",");
            var fieldTypes = table.FieldTypes.Split(",");

            schema.Add("Id", "number");
            schema.Add("TeamId", "number");
            schema.Add("SubscriberId", "number");
            schema.Add("TeamName", "string");

            for (int i = 0; i < fieldNames.Length; i++)
            {
                schema.Add(fieldNames[i], fieldTypes[i]);
            }
            schema.Add("CreatedUserName", "string");
            schema.Add("CreatedAt", "datetime");
            schema.Add("UpdatedUserName", "string");
            schema.Add("UpdatedAt", "datetime");

            return schema;
        }

        private static string ConvertClauseTypeToSql(string clauseType)
        {
            switch (clauseType)
            {
                case "eq":
                    return " = ";
                case "ne":
                    return " <> ";
                case "like":
                    return " like ";
                case "ge":
                    return " > ";
                case "lt":
                    return " < ";
            }
            return " = ";
        }

        private static string ConvertToTableRecordFieldType(string fieldType)
        {
            switch (fieldType)
            {
                case "number":
                    return "NumberValue";
                case "datetime":
                    return "DateTimeValue";
                case "string":
                default:
                    return "StringValue";
            }
        }

        public static string CreateSelectFields(this Table table, string select)
        {
            if (string.IsNullOrEmpty(select)) return " * ";
            var tableFields = table.GetTableFields();
            var fieldTypes = table.GetTableFieldTypes();
            var selectFields = select.Split(",");

            if (selectFields.Length == 0)
            {
                return " * ";
            }

            var fields = new List<string>();
            for (int i = 0; i < selectFields.Length; i++)
            {
                int fieldIndex = Array.IndexOf(tableFields, selectFields[i].ToLower());
                if (fieldIndex > -1)
                {
                    fields.Add("Field" + (fieldIndex + 1) + ConvertToTableRecordFieldType(fieldTypes[fieldIndex]));
                }
                else
                {
                    fields.Add(selectFields[i]);
                }
            }
            return string.Join(",", fields);
        }

        public static dynamic CreateQueryFromTableAndFilter(this Table table, string filter, StringBuilder clauseQuery)
        {
            if (string.IsNullOrEmpty(filter)) return new Dictionary<string, object>();
            var schema = table.GetTableSchema();
            var tableFields = table.GetTableFields();
            var fieldTypes = table.GetTableFieldTypes();
            string[] clauses = filter.Split(",");
            dynamic result = new Dictionary<string, object>();
            for (int i = 0; i < clauses.Length; i++)
            {
                string clause = clauses[i];
                int startBrackets = clause.IndexOf("[");
                int endBrackets = clause.IndexOf("]");

                if (startBrackets > -1 && endBrackets > -1)
                {
                    string fieldName = clause.Substring(0, startBrackets);
                    string searchValue = clause.Substring(endBrackets + 1);
                    string clauseType = clause.Substring(startBrackets + 1, endBrackets - startBrackets - 1);

                    if (schema.ContainsKey(fieldName))
                    {
                        int fieldIndex = Array.IndexOf(tableFields, fieldName.ToLower());
                        string actualFieldName = (fieldIndex > -1) ? "Field" + (fieldIndex + 1) + ConvertToTableRecordFieldType(fieldTypes[fieldIndex]) : fieldName;

                        clauseQuery
                            .Append("AND ")
                            .Append(actualFieldName)
                            .Append(ConvertClauseTypeToSql(clauseType))
                            .Append("@")
                            .Append(fieldName.TrimEnd());

                        result[fieldName] = searchValue.TrimStart().Replace("\"", "");
                    }
                }

            }
            return result;
        }
    }
}
