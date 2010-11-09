using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using RavenMQ.Config;
using RavenMQ.Data;
using RavenMQ.Extensions;
using RavenMQ.Storage;

namespace RavenMQ.Impl
{
    public class Queues : IQueues, IUuidGenerator
    {
        private readonly InMemroyRavenConfiguration configuration;
        private readonly TransactionalStorage transactionalStorage;

        public Queues(InMemroyRavenConfiguration configuration)
        {
            this.configuration = configuration;
            transactionalStorage = new TransactionalStorage(configuration);
            try
            {
                transactionalStorage.Initialize(this);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
            transactionalStorage.Batch(actions => currentEtagBase = actions.General.GetNextIdentityValue("Raven/Etag"));
        }

        public void Enqueue(IncomingMessage incomingMessage)
        {
            var bytes = incomingMessage.Metadata.ToBytes();
            var ms = new MemoryStream(bytes.Length + incomingMessage.Data.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Write(incomingMessage.Data, 0, incomingMessage.Data.Length);

            transactionalStorage.Batch(actions => actions.Messages.Enqueue(incomingMessage.Queue, DateTime.UtcNow.Add(incomingMessage.TimeToLive), ms.ToArray()));
        }

        public IEnumerable<OutgoingMessage> Read(string queue, Guid lastMessageId)
        {
            var msgs = new List<OutgoingMessage>();
            transactionalStorage.Batch(actions=>
            {
                var outgoingMessage = actions.Messages.Dequeue(lastMessageId);
                while (outgoingMessage != null && msgs.Count < configuration.MaxPageSize)
                {
                    var buffer = outgoingMessage.Data;
                    var memoryStream = new MemoryStream(buffer);
                    outgoingMessage.Metadata = memoryStream.ToJObject();
                    outgoingMessage.Data = new byte[outgoingMessage.Data.Length - memoryStream.Position];
                    Array.Copy(buffer,memoryStream.Position, outgoingMessage.Data, 0, outgoingMessage.Data.Length);
                    msgs.Add(outgoingMessage);
                    outgoingMessage = actions.Messages.Dequeue(outgoingMessage.Id);
                }
            });
            return msgs;
        }

        public void Dispose()
        {
            if(transactionalStorage!=null)
                transactionalStorage.Dispose();
        }

        private long currentEtagBase;
        private static long sequentialUuidCounter;
        public Guid CreateSequentialUuid()
        {
            var ticksAsBytes = BitConverter.GetBytes(currentEtagBase);
            Array.Reverse(ticksAsBytes);
            var increment = Interlocked.Increment(ref sequentialUuidCounter);
            var currentAsBytes = BitConverter.GetBytes(increment);
            Array.Reverse(currentAsBytes);
            var bytes = new byte[16];
            Array.Copy(ticksAsBytes, 0, bytes, 0, ticksAsBytes.Length);
            Array.Copy(currentAsBytes, 0, bytes, 8, currentAsBytes.Length);
            return new Guid(bytes);
        }
    }
}