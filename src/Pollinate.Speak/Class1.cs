using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;

namespace Pollinate.Speak
{
    public class HowManyStorageFailures
    {
        public long Count{get;set;}
    }

    public class StorageEvent
    {
        public bool WriteEvent{get;set;}
    }

    public interface IStore
    {
        bool Store(object message);
    }

    public class StoreAndForwardActor : UntypedActor
    {
        IStore _storage;

        public StoreAndForwardActor(IStore storage)
        {

            _storage = storage;
        }

        protected override void OnReceive(object message)
        {

            bool storedSuccessfully = _storage.Store(message);

            Context.System.EventStream.Publish(new StorageEvent{ WriteEvent=storedSuccessfully });
        }
    }    

    public class MonitorActor : UntypedActor
    {
        List<StorageEvent> _storageEvents = new List<StorageEvent>();

        protected override void OnReceive(object message)
        {
        
            var storageEvent = message as StorageEvent;

            var storageEventQuery = message as HowManyStorageFailures;

            if (storageEvent != null)
            {
                _storageEvents.Add(storageEvent);
            }
            else if(storageEventQuery != null)
            {
               Sender.Tell(new HowManyStorageFailures{ Count=_storageEvents.Where(e => e.WriteEvent == false).Count()}); 
            }

        }

    }
}
