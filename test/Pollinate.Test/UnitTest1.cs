using System;
using Xunit;
using Pollinate.Speak;
using Akka.Actor;
using LiteDB;


namespace Pollinate.Test
{
    public class When_speaking
    {
        ActorSystem _actorSystem;
        IActorRef _monitorActor;

        public When_speaking()
        {
             _actorSystem = ActorSystem.Create("ApplicationEvents");

              Props monitorActorProps = Props.Create(() => new MonitorActor());

              _monitorActor = _actorSystem.ActorOf(monitorActorProps, "monitor");

                _actorSystem.EventStream.Subscribe(_monitorActor, typeof(StorageEvent));
        }

        [Fact]
        public void Given_message_then_store_for_delivery()
        {
              Props storeAndForwardActorProps = Props.Create(() => new StoreAndForwardActor(new StubStore(true)));

                IActorRef storeAndForwardActor = _actorSystem.ActorOf(storeAndForwardActorProps, "storeandforward");

                storeAndForwardActor.Tell(new SampleMessage());
        }

        [Fact]
        public void Given_message_unable_to_store_then_raise_error()
        {
              Props storeAndForwardActorProps = Props.Create(() => new StoreAndForwardActor(new StubStore(false)));

                IActorRef storeAndForwardActor = _actorSystem.ActorOf(storeAndForwardActorProps, "storeandforward");

                storeAndForwardActor.Tell(new SampleMessage());

                Assert.Equal(_monitorActor.Ask<HowManyStorageFailures>(new HowManyStorageFailures()).Result.Count, 1);
        }
  }

    public class SampleMessage
    {

    }

    public class StubStore : IStore
    {
        bool _response;

        public StubStore(bool response)
        {
            _response = response;
        }

        public bool Store(object message)
        {
            return _response;
        }
    }
}
