using System;

namespace HomeKitAccessory.Net
{
    class HttpException : Exception
        {
            public int StatusCode {get; private set;}

            public HttpException(int statusCode, string message, Exception innerException)
                : base(message, innerException)
            {
                StatusCode = statusCode;
            }

            public HttpException(int statusCode, string message)
                : base(message)
            {
                StatusCode = statusCode;
            }
        }
}