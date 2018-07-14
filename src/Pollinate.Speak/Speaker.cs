using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Pollinate.Speak
{
    public class RetryEnvelope
    {
        public object Message{get;set;}

        public int RemainingRetries{get;set;}

        public RetryEnvelope(object message, int remainingRetries)
        {
            Message = message;
            RemainingRetries = remainingRetries;
        }
    }

    public class StorageEvent
    {
        public bool WriteEvent{get;set;}
    }

    public class HowManyStorageFailures
    {
        public long Count{get;set;}
    }

    public class DeliveryEvent
    {
        public bool Sent{get;set;}
    }

    public class HowManyDeliveries
    {
        public long Sent{get;set;}
    }

    public interface IStore
    {
        bool Store(object message);
    }

    public interface ISend
    {
        HttpResponseMessage Send(object message);
    }

    public class StoreAndForwardActor : UntypedActor
    {
        IStore _storage;
        IActorRef _deliveryActor;

        public StoreAndForwardActor(IStore storage, IActorRef deliveryActor)
        {
            _storage = storage;
            _deliveryActor = deliveryActor;
        }

        protected override void OnReceive(object message)
        {

            bool storedSuccessfully = _storage.Store(message);

            Context.System.EventStream.Publish(new StorageEvent{ WriteEvent=storedSuccessfully });
            
            if(storedSuccessfully)
            {
                _deliveryActor.Tell(message);
            }
        }
    }    

    public class DeliveryActor : UntypedActor
    {
        ISend _sender;

        public DeliveryActor(ISend sender)
        {
            _sender = sender;
        }

        protected override void OnReceive(object message)
        {
            var retriedMessage = message as RetryEnvelope;

            var outboundMessage = retriedMessage == null ? message : retriedMessage.Message;

            if (outboundMessage != null)
            {
                var response = _sender.Send(outboundMessage);

                if (response.IsSuccessStatusCode)
                {
                    Context.System.EventStream.Publish(new DeliveryEvent{ Sent=true });
                }
                else
                {
                    Context.System.EventStream.Publish(new DeliveryEvent{ Sent=false });
                    if(retriedMessage == null)
                    {
                        Console.WriteLine("start retry");
                        Self.Tell(new RetryEnvelope(outboundMessage, 5));
                    }
                    else
                    {
                        Self.Tell(new RetryEnvelope(retriedMessage.Message, retriedMessage.RemainingRetries - 1));
                    }
                }
            }
        }
    }

    public class MonitorActor : UntypedActor
    {
        List<StorageEvent> _storageEvents;
        List<DeliveryEvent> _deliveryEvents;

        public MonitorActor()
        {
            _storageEvents = new List<StorageEvent>();
            _deliveryEvents = new List<DeliveryEvent>();
        }

        protected override void OnReceive(object message)
        {
            var storageEvent = message as StorageEvent;

            var deliveryEvent = message as DeliveryEvent;

            var storageEventQuery = message as HowManyStorageFailures;

            var deliveryEventQuery = message as HowManyDeliveries;

            if (storageEvent != null)
            {
                _storageEvents.Add(storageEvent);
            }
            else if(storageEventQuery != null)
            {
               Sender.Tell(new HowManyStorageFailures{ Count=_storageEvents.Where(e => e.WriteEvent == false).Count()}); 
            }
            else if (deliveryEvent != null)
            {
                _deliveryEvents.Add(deliveryEvent);
            }
            else if(deliveryEventQuery != null)
            {
               Sender.Tell(new HowManyDeliveries{ Sent=_deliveryEvents.Where(e => e.Sent).Count()}); 
            }
        }
    }
}
