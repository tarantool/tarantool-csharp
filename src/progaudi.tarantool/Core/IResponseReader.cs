using System;
using System.IO;
using System.Threading.Tasks;
using ProGaudi.Tarantool.Client.Model;

namespace ProGaudi.Tarantool.Client.Core
{
    public interface IResponseReader : IDisposable
    {
        void BeginReading();

        Task<MemoryStream> GetResponseTask(RequestId requestId);

        bool IsConnected { get; }
    }
}