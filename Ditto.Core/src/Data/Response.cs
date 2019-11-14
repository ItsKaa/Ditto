using System;

namespace Ditto.Data
{
    public class Response<T> : IEquatable<Response<T>>
    {
        public bool Success { get; set; }
        public T Result { get; set; }

        public Response()
        {
            Success = false;
            Result = default(T);
        }

        public Response(T result, bool success)
        {
            Result = result;
            Success = success;
        }

        public bool Equals(Response<T> other)
        {
            return
                other != null &&
                Success == other.Success &&
                Equals(Result, other.Result);
        }
    }
}
