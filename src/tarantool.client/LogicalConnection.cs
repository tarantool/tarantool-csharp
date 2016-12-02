﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ProGaudi.MsgPack.Light;

using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Model.Headers;
using ProGaudi.Tarantool.Client.Model.Requests;
using ProGaudi.Tarantool.Client.Model.Responses;
using ProGaudi.Tarantool.Client.Utils;

namespace ProGaudi.Tarantool.Client
{
    internal class LogicalConnection : ILogicalConnection
    {
        private readonly MsgPackContext _msgPackContext;

        private readonly ClientOptions _clientOptions;

        private INetworkStreamPhysicalConnection _physicalConnection;

        private IResponseReader _responseReader;

        private long _currentRequestId;

        private readonly ILog _logWriter;

        public Func<GreetingsResponse, Task> _greetingFunc { get; set; }

        public LogicalConnection(ClientOptions options)
        {
            _clientOptions = options;
            _msgPackContext = options.MsgPackContext;
            _logWriter = options.LogWriter;
        }

        public void Dispose()
        {
            _responseReader?.Dispose();
            _physicalConnection?.Dispose();
        }

        public async Task Connect()
        {
            _physicalConnection = new NetworkStreamPhysicalConnection();
            await _physicalConnection.Connect(_clientOptions);

            var greetingsResponseBytes = new byte[128];
            var readCount = await _physicalConnection.ReadAsync(greetingsResponseBytes, 0, greetingsResponseBytes.Length);
            if (readCount != greetingsResponseBytes.Length)
            {
                throw ExceptionHelper.UnexpectedGreetingBytesCount(readCount);
            }

            var greetings = new GreetingsResponse(greetingsResponseBytes);

            _clientOptions.LogWriter?.WriteLine($"Greetings received, salt is {Convert.ToBase64String(greetings.Salt)} .");

            _responseReader = new ResponseReader(_clientOptions, _physicalConnection);
            _responseReader.BeginReading();

            _clientOptions.LogWriter?.WriteLine("Server responses reading started.");

            await this._greetingFunc(greetings);
        }

        // TODO: reconnection func here with dropping/disposing old _physicalConnection and _responseReader

        public async Task SendRequestWithEmptyResponse<TRequest>(TRequest request)
            where TRequest : IRequest
        {
            await SendRequestImpl<TRequest, EmptyResponse>(request);
        }

        public async Task<DataResponse<TResponse[]>> SendRequest<TRequest, TResponse>(TRequest request)
            where TRequest : IRequest
        {
            return await SendRequestImpl<TRequest, DataResponse<TResponse[]>>(request);
        }

        public static byte[] ReadFully(Stream input)
        {
            input.Position = 0;
            var buffer = new byte[16*1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private async Task<TResponse> SendRequestImpl<TRequest, TResponse>(TRequest request)
            where TRequest : IRequest
        {
            // TODO: detect disconnects and reconnect here

            var bodyBuffer = MsgPackSerializer.Serialize(request, _msgPackContext);

            var requestId = GetRequestId();
            var responseTask = _responseReader.GetResponseTask(requestId);

            long headerLength;
            var headerBuffer = CreateAndSerializeBuffer(request, requestId, bodyBuffer, out headerLength);

            try
            {
                lock (_physicalConnection)
                {
                    _logWriter?.WriteLine($"Begin sending request header buffer, requestId: {requestId}, code: {request.Code}, length: {headerBuffer.Length}");
                    _physicalConnection.Write(headerBuffer, 0, Constants.PacketSizeBufferSize + (int)headerLength);

                    _logWriter?.WriteLine($"Begin sending request body buffer, length: {bodyBuffer.Length}");
                    _physicalConnection.Write(bodyBuffer, 0, bodyBuffer.Length);
                }
            }
            catch (Exception ex)
            {
                _responseReader.FaultedState();
                _logWriter?.WriteLine($"Request with requestId {requestId} failed, header:\n{ToReadableString(headerBuffer)} \n body: \n{ToReadableString(bodyBuffer)}");
                throw;
            }

            try
            {
                var responseStream = await responseTask;
                _logWriter?.WriteLine($"Response with requestId {requestId} is recieved, length: {responseStream.Length}.");

                return MsgPackSerializer.Deserialize<TResponse>(responseStream, _msgPackContext);
            }
            catch (ArgumentException)
            {
                _logWriter?.WriteLine($"Response with requestId {requestId} failed, header:\n{ToReadableString(headerBuffer)} \n body: \n{ToReadableString(bodyBuffer)}");
                throw;
            }
        }

        private static string ToReadableString(byte[] bytes)
        {
            return string.Join(" ", bytes.Select(b => b.ToString("X2")));
        }

        private byte[] CreateAndSerializeBuffer<TRequest>(
            TRequest request,
            RequestId requestId,
            byte[] serializedRequest,
            out long headerLength) where TRequest : IRequest
        {
            var packetSizeBuffer = new byte[Constants.PacketSizeBufferSize + Constants.MaxHeaderLength];
            var stream = new MemoryStream(packetSizeBuffer);

            var requestHeader = new RequestHeader(request.Code, requestId);
            stream.Seek(Constants.PacketSizeBufferSize, SeekOrigin.Begin);
            MsgPackSerializer.Serialize(requestHeader, stream, _msgPackContext);

            headerLength = stream.Position - Constants.PacketSizeBufferSize;
            var packetLength = new PacketSize((uint) (headerLength + serializedRequest.Length));
            stream.Seek(0, SeekOrigin.Begin);
            MsgPackSerializer.Serialize(packetLength, stream, _msgPackContext);
            return packetSizeBuffer;
        }

        private RequestId GetRequestId()
        {
            var requestId = Interlocked.Increment(ref _currentRequestId);
            return (RequestId) (ulong) requestId;
        }
    }
}