using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    }
}
