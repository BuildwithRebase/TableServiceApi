using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace TableService.Core.Utility
{
    public static class DynamicClassUtility
    {

        private static AssemblyBuilder DefineAssemblyBuilder(string name)
        {
            AssemblyName assemName = new AssemblyName();
            assemName.Name = "DynamicAssembly";
            AssemblyBuilder assemBuilder =
                           AssemblyBuilder.DefineDynamicAssembly(assemName, AssemblyBuilderAccess.Run);

            return assemBuilder;
        }

        private static TypeBuilder DefineTypeBuilder(AssemblyBuilder assemBuilder, string className)
        {
            // Create a dynamic module in Dynamic Assembly.
            ModuleBuilder modBuilder = assemBuilder.DefineDynamicModule("DynamicModule");
            // Define a public class named "DynamicClass" in the assembly.
            TypeBuilder tb = modBuilder.DefineType(className, TypeAttributes.Public);

            return tb;
        }

        public static Type CreateType(string className, List<FieldDefinition> fields)
        {
            AssemblyBuilder assembBuilder = DefineAssemblyBuilder("DynamicAssembly");
            TypeBuilder tb = DefineTypeBuilder(assembBuilder, className);

            foreach (var field in fields)
            {
                if (field.FieldType == "string")
                {
                    tb.DefineField(field.FieldName, typeof(string), FieldAttributes.Public);
                } else if (field.FieldType == "datetime")
                {
                    tb.DefineField(field.FieldName, typeof(DateTime), FieldAttributes.Public);
                } else if (field.FieldType == "number")
                {
                    tb.DefineField(field.FieldName, typeof(int), FieldAttributes.Public);
                } else
                {
                    throw new InvalidTableException("Invalid field type: " + field.FieldName + ", " + field.FieldType);
                }
            }

            return tb.CreateType();
        }

        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            return type.GetField(fieldName);
        }

        public static void SetFieldValue(Type type, object obj, string fieldName, object value)
        {
            GetFieldInfo(type, fieldName).SetValue(obj, value);
        }

        public static object GetFieldValue(Type type, object obj, string fieldName)
        {
            return GetFieldInfo(type, fieldName).GetValue(obj);
        }

        public static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            return type.GetProperty(propertyName);
        }

        public static void SetPropertyValue(Type type, object obj, string propertyName, object value)
        {
            GetPropertyInfo(type, propertyName).SetValue(obj, value);
        }

        public static object GetPropertyValue(Type type, object obj, string propertyName)
        {
            return GetPropertyInfo(type, propertyName).GetValue(obj);
        }

    }
}
