using System;

namespace TableService.Core.Exceptions
{
    public class MyHttpException : Exception
    {
        public int HttpStatusCode { get; init; }
        public MyHttpException(int httpStatusCode, string message) : base(message) => HttpStatusCode = httpStatusCode;
    }
}
