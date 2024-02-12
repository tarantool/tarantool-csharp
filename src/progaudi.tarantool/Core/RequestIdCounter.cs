using System.Threading;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Core
{
    public class RequestIdCounter
    {
        private long _currentRequestId;

        public RequestId GetRequestId()
        {
            var requestId = Interlocked.Increment(ref _currentRequestId);
            return (RequestId)(ulong)requestId;
        }
    }
}
