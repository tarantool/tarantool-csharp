using System;

namespace ProGaudi.Tarantool.Client.Core
{
    internal interface IRequestWriter : IDisposable
    {
        void BeginWriting();

        bool IsConnected { get; }

        void Write(ArraySegment<byte> request);
    }
}