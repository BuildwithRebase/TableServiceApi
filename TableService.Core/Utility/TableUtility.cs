using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TableService.Core.Models;

namespace TableService.Core.Utility
{
    public static class TableUtility
    {
        public static string GetTablePrefixFromName(string tableName)
        {
            var result = tableName.Replace(" ", "");
            if (result.Length > 20) {
                return result.Substring(0, 20).ToLower();
            }
            return result.ToLower();
        }

        /// <summary>
        /// Creates a TableRecord from a table
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static TableRecord CreateTableRecordFromTable(ApiSession apiSession, Table table, Dictionary<string, object> data, bool update = true)
        {
            var tableName = table.TableName;
            var tableRecord = new TableRecord
            {
                TeamId = apiSession.TeamId,
                TeamName = apiSession.TeamName,
                TableName = tableName,
                UpdatedUserName = apiSession.UserName,
                UpdatedAt = DateTime.Now
            };

            if (update)
            {
                tableRecord.Id = ((JsonElement)data["Id"]).GetInt32();
            }
            else
            {
                tableRecord.CreatedAt = DateTime.Now;
                tableRecord.CreatedUserName = apiSession.UserName;
            }

            Type tableRecordType = typeof(TableRecord);
            var fields = table.ToFieldDefinitions();
            for (var i = 0; i<Math.Min(fields.Count, 5); i++)
            {
                var field = fields[i];
                if (data.ContainsKey(field.FieldName))
                {
                    object value = null;

                    StringBuilder fieldName = new StringBuilder();
                    fieldName.Append("Field").Append((i + 1));

                    if (field.FieldType == "datetime")
                    {
                        fieldName.Append("DateTimeValue");
                        if (data[field.FieldName] != null)
                        {
                            value = ((JsonElement)data[field.FieldName]).GetDateTime();
                        }                        
                    }
                    else if (field.FieldType == "string")
                    {
                        fieldName.Append("StringValue");
                        if (data[field.FieldName] != null)
                        {
                            value = ((JsonElement)data[field.FieldName]).GetString();
                        }                        
                    }
                    else if (field.FieldType == "number")
                    {
                        fieldName.Append("NumberValue");
                        if (data[field.FieldName] != null)
                        {
                            value = ((JsonElement)data[field.FieldName]).GetInt32();
                        }
                    }

                    DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, fieldName.ToString(), value);
                }
            }
            return tableRecord;
        }

        public static object MapTableRecordToObject(string tableName, TableRecord record, Type objectType, List<FieldDefinition> fields)
        {
            Type tableRecordType = typeof(TableRecord);

            object obj = Activator.CreateInstance(objectType);

            for (var i = 0; i < Math.Min(fields.Count, 5); i++)
            {
                var field = fields[i];

                if (field.FieldName == "Id")
                {
                    int value = (int)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "Id");
                    DynamicClassUtility.SetFieldValue(objectType, obj, field.FieldName, value);
                }
                else if (field.FieldType == "datetime")
                {
                    DateTime value = (DateTime)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "Field" + (i + 1) + "DateTimeValue");
                    DynamicClassUtility.SetFieldValue(objectType, obj, field.FieldName, value);
                }
                else if (field.FieldType == "string")
                {
                    string value = (string)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "Field" + (i + 1) + "StringValue");
                    DynamicClassUtility.SetFieldValue(objectType, obj, field.FieldName, value);
                }
                else if (field.FieldType == "number")
                {
                    int value = (int)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "Field" + (i + 1) + "NumberValue");
                    DynamicClassUtility.SetFieldValue(objectType, obj, field.FieldName, value);
                }
            }

            return obj;
        }
    }
}
