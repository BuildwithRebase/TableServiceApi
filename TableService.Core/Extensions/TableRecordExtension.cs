using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TableService.Core.Models;
using TableService.Core.Utility;

namespace TableService.Core.Extensions
{
    public static class TableRecordExtension
    {
        public static void PopulateWithData(this TableRecord tableRecord, Table table, Dictionary<string, object> data)
        {
            var tableRecordType = typeof(TableRecord);

            string[] fieldNames = table.FieldNames.Split(new char[] { ',' });
            string[] fieldTypes = table.FieldTypes.Split(new char[] { ',' });
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];
                string fieldType = fieldTypes[i];
                int index = i + 1;

                switch (fieldType)
                {
                    case "number":
                        DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "Field" + index + "NumberValue", ((JsonElement)data[fieldName]).GetInt32());
                        break;
                    case "datetime":
                        DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "Field" + index + "DateTimeValue", ((JsonElement)data[fieldName]).GetDateTime());
                        break;
                    case "string":
                    default:
                        DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "Field" + index + "StringValue", ((JsonElement)data[fieldName]).GetString());
                        break;
                }
            }
        }

        private static void AddToDataIfInSelect(this TableRecord tableRecord, Type tableRecordType, string fieldName, string tableFieldName, string[] selectFields, Dictionary<string, object> data)
        {
            if (selectFields.Length == 0)
            {
                data.Add(fieldName, DynamicClassUtility.GetPropertyValue(tableRecordType, tableRecord, tableFieldName));
            }
            else if (selectFields.Length > 0 && Array.IndexOf(selectFields, fieldName) > -1)
            {
                data.Add(fieldName, DynamicClassUtility.GetPropertyValue(tableRecordType, tableRecord, tableFieldName));
            }            
        }

        public static Dictionary<string, object> MapToData(this TableRecord tableRecord, Table table, string select = "")
        {
            var tableRecordType = typeof(TableRecord);
            var data = new Dictionary<string, object>();

            string[] fieldNames = table.FieldNames.Split(new char[] { ',' });
            string[] fieldTypes = table.FieldTypes.Split(new char[] { ',' });
            string[] selectFields = string.IsNullOrEmpty(select) ? new List<string>().ToArray() : select.Split(",");

            tableRecord.AddToDataIfInSelect(tableRecordType, "Id", "Id", selectFields, data);
            tableRecord.AddToDataIfInSelect(tableRecordType, "TeamId", "TeamId", selectFields, data);
            tableRecord.AddToDataIfInSelect(tableRecordType, "SubscriberId", "SubscriberId", selectFields, data);
            tableRecord.AddToDataIfInSelect(tableRecordType, "TeamName", "TeamName", selectFields, data);

            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];
                string fieldType = fieldTypes[i];
                int index = i + 1;

                switch (fieldType)
                {
                    case "number":
                        tableRecord.AddToDataIfInSelect(tableRecordType, fieldName, "Field" + index + "NumberValue", selectFields, data);
                        break;
                    case "datetime":
                        tableRecord.AddToDataIfInSelect(tableRecordType, fieldName, "Field" + index + "DateTimeValue", selectFields, data);
                        break;
                    case "string":
                    default:
                        tableRecord.AddToDataIfInSelect(tableRecordType, fieldName, "Field" + index + "StringValue", selectFields, data);
                        break;
                }
            }
            tableRecord.AddToDataIfInSelect(tableRecordType, "CreatedUserName", "CreatedUserName", selectFields, data);
            tableRecord.AddToDataIfInSelect(tableRecordType, "CreatedAt", "CreatedAt", selectFields, data);
            tableRecord.AddToDataIfInSelect(tableRecordType, "UpdatedUserName", "UpdatedUserName", selectFields, data);
            tableRecord.AddToDataIfInSelect(tableRecordType, "UpdatedAt", "UpdatedAt", selectFields, data);

            return data;
        }
    }
}
