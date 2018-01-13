using System;

namespace Ditto.Data.Exceptions
{
    public class ApiException : Exception
    {
        public string Service { get; private set; }
        //public string Error { get; private set; }

        public ApiException(string service)
        {
            Service = service;
            //Error = error;
        }

        public override string ToString()
        {
            return $"The service {Service} has thrown an exception: {GetType().FullName}\n{StackTrace}";
        }
    }
}
