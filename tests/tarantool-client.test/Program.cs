﻿using System.Linq;
using iproto;
using iproto.Data;
using iproto.Data.Packets;
using iproto.Data.UpdateOperations;

namespace tarantool_client.test
{
    public class Program
    {
        private const int spaceId = 514;
        public static void Main(string[] args)
        {
            var tarantoolClient = new Connection();
            tarantoolClient.Connect("192.168.99.100", 3301);
            var response = tarantoolClient.Login("operator", "operator");

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
            {
                System.Console.WriteLine("An error occured in login process:");
                System.Console.WriteLine(response.ErrorMessage);
                System.Console.WriteLine("Exiting...");
                return;
            }

            var schema = tarantoolClient.GetSchema();
            var tester = schema.GetSpace("tester");
            var firstIndex = tester.Indices.First(index => index.Type == IndexType.Tree);

            SendPacketMethodTest(tarantoolClient);
            SpaceMethodsTest(tester);
            IndexMethodsTest(firstIndex);
        }

        private static void IndexMethodsTest(Index index)
        {
            var insertResponse = index.Insert(Tuple.Create(2, "Music"));
            var deleteResponse = index.Delete<Tuple<int, string, double>, Tuple<int>>(Tuple.Create(2));
            insertResponse = index.Insert(Tuple.Create(2, "Music"));
            var selectResponse = index.Select<Tuple<int, string>, Tuple<int>>(Tuple.Create(2));
            var replaceResponse = index.Replace(Tuple.Create(2, "Car", -245.3));
            var updateResponse = index.Update<Tuple<int, string, double>, Tuple<int>, int>(Tuple.Create(2),
                UpdateOperation<int>.CreateAddition(3, 100));
            var upsertResponse = index.Upsert<Tuple<int, string>, Tuple<int>, int>(Tuple.Create(5),
                UpdateOperation<int>.CreateAddition(2, 2));
            upsertResponse = index.Upsert<Tuple<int, string>, Tuple<int>, int>(Tuple.Create(5),
                UpdateOperation<int>.CreateAddition(2, -2));

            var min2 = index.Min<Tuple<int,int>, Tuple<int>>(Tuple.Create(3));
            var min = index.Min<Tuple<int, string, double>>();

            var max= index.Max<Tuple<int, int>>();
            var max2 = index.Max<Tuple<int, string, double>, Tuple<int>>(Tuple.Create(4));

        }

        private static void SpaceMethodsTest(Space tester)
        {
            var insertResponse = tester.Insert(Tuple.Create(2, "Music"));
            var deleteResponse = tester.Delete(Tuple.Create(2));
            insertResponse = tester.Insert(Tuple.Create(2, "Music"));
            var selectResponse = tester.Select<Tuple<int>, Tuple<int, string>>(Tuple.Create(2));
            var replaceResponse = tester.Replace(Tuple.Create(2, "Car", -24.5));
            var updateResponse = tester.Update(Tuple.Create(2), UpdateOperation<int>.CreateAddition(1, 2));
            var upsertResponse = tester.Upsert(Tuple.Create(5, 20), UpdateOperation<int>.CreateAddition(1, 2));
        }

        private static void SendPacketMethodTest(Connection tarantoolClient)
        {
            var insertRequest = new InsertReplacePacket<Tuple<int, string>>(CommandCode.Insert, spaceId,
                Tuple.Create(2, "Music"));
            var insertResponse = tarantoolClient.SendPacket(insertRequest);

            var deleteRequest = new DeletePacket<Tuple<int>>(spaceId, 0, Tuple.Create(2));
            var deleteResponse = tarantoolClient.SendPacket(deleteRequest);

            insertResponse = tarantoolClient.SendPacket(insertRequest);

            var selectRequest = new SelectPacket<Tuple<int>>(spaceId, 0, 100, 0, Iterator.All, Tuple.Create(2));
            var selectResponse = tarantoolClient.SendPacket(selectRequest);

            var replaceRequest = new InsertReplacePacket<Tuple<int, string, int>>(CommandCode.Replace, spaceId,
                Tuple.Create(2, "Orange", 5));
            var replaceResponse = tarantoolClient.SendPacket(replaceRequest);

            var udateRequest = new UpdatePacket<Tuple<int>, int>(spaceId, 0, Tuple.Create(2),
                UpdateOperation<int>.CreateAddition(1, 2));
            var updateResponse = tarantoolClient.SendPacket(udateRequest);

            selectResponse = tarantoolClient.SendPacket(selectRequest);

            var upsertRequest = new UpsertPacket<Tuple<int, int>, int>(spaceId, Tuple.Create(5, 20),
                UpdateOperation<int>.CreateAddition(10, 1));
            var upsertResponse = tarantoolClient.SendPacket(upsertRequest);

            selectResponse = tarantoolClient.SendPacket(selectRequest);

            var callRequest = new CallPacket<Tuple<float>>("math.sqrt", Tuple.Create(1.3f));
            var callResponse = tarantoolClient.SendPacket(callRequest);

            var evalRequest = new EvalPacket<Tuple<int, int, int>>("return ...", Tuple.Create(1, 2, 3));
            var evalResponse = tarantoolClient.SendPacket(evalRequest);
        }
    }
}
