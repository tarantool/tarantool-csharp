using System;

namespace ProGaudi.Tarantool.Client.Pool
{
    public class NoHealthyNodeException : Exception
    {
        public NoHealthyNodeException() : base("There is no healthy node for current request")
        {
            
        }
    }
}