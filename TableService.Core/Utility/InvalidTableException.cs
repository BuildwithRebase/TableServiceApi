using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TableService.Core.Models;

namespace TableService.Core.Utility
{
    public class InvalidTableException : Exception
    {
        public InvalidTableException()
        {

        }

        public InvalidTableException(string message)
            : base(message)
        {

        }

        public InvalidTableException(Table table)
        : base(String.Format("Invalid table definition {0}", table))
        {

        }
    }
}
