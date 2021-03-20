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
        public static TableRecord CreateTableRecordFromTable(ApiSession apiSession, Table table, Dictionary<string, object> data, bool update = true, TableRecord oldRecord = null)
        {
            var tableName = table.TableName;
            var tableRecord = oldRecord ?? new TableRecord
            {
                TeamId = apiSession.TeamId,
                TeamName = apiSession.TeamName,
                TableName = tableName
            };

            Type tableRecordType = typeof(TableRecord);

            DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "UpdatedUserName", apiSession.UserName);
            DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "UpdatedAt", DateTime.Now);

            //if (update)
            //{
            //    tableRecord.Id = ((JsonElement)data["Id"]).GetInt32();
            //}
            //else
            //{
            //    if (oldRecord != null)
            //    {
            //        DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "CreatedUserName", oldRecord.CreatedUserName);
            //        DynamicClassUtility.SetPropertyValue(tableRecordType, tableRecord, "CreatedAt", oldRecord.CreatedAt);
            //    }
            //}

            var fields = table.ToFieldDefinitions()
                    .Where(fld => !fld.FieldName.Equals("Id") && !fld.FieldName.Equals("CreatedAt") && !fld.FieldName.Equals("CreatedUserName") && !fld.FieldName.Equals("UpdatedAt") && !fld.FieldName.Equals("UpdatedUserName"))
                    .ToList();
            for (var i = 0; i<fields.Count; i++)
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

            int id = (int)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "Id");
            DynamicClassUtility.SetFieldValue(objectType, obj, "Id", id);
            
            var fields2 = fields
                .Where(fld => !fld.FieldName.Equals("Id") && !fld.FieldName.Equals("CreatedAt") && !fld.FieldName.Equals("CreatedUserName") && !fld.FieldName.Equals("UpdatedAt") && !fld.FieldName.Equals("UpdatedUserName"))
                .ToList();

            for (var i = 0; i < fields2.Count; i++)
            {
                var field = fields2[i];

                if (field.FieldType == "datetime")
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
                    object numberObject = DynamicClassUtility.GetPropertyValue(tableRecordType, record, "Field" + (i + 1) + "NumberValue");
                    if (numberObject == null)
                    {
                        DynamicClassUtility.SetFieldValue(objectType, obj, field.FieldName, 0);
                    } else
                    {
                        DynamicClassUtility.SetFieldValue(objectType, obj, field.FieldName, (int)numberObject);
                    }

                    
                }
            }

            DateTime createdAt = (DateTime)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "CreatedAt");
            DynamicClassUtility.SetFieldValue(objectType, obj, "CreatedAt", createdAt);

            string createdUserName = (string)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "CreatedUserName");
            DynamicClassUtility.SetFieldValue(objectType, obj, "CreatedUserName", createdUserName);

            DateTime updatedAt = (DateTime)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "UpdatedAt");
            DynamicClassUtility.SetFieldValue(objectType, obj, "UpdatedAt", updatedAt);

            string updatedUserName = (string)DynamicClassUtility.GetPropertyValue(tableRecordType, record, "UpdatedUserName");
            DynamicClassUtility.SetFieldValue(objectType, obj, "UpdatedUserName", updatedUserName);

            return obj;
        }
    }
}
