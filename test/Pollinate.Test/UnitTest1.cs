using System;
using Xunit;
using Pollinate.Speak;
using Akka.Actor;
using LiteDB;
using System.Net.Http;
using System.Net.Http.Headers;

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
                _actorSystem.EventStream.Subscribe(_monitorActor, typeof(DeliveryEvent));
        }

        [Fact]
        public void Given_message_then_store_for_delivery()
        {
              Props deliveryActorProps = Props.Create(() => new DeliveryActor(new SampleSender()));

            IActorRef deliveryActor = _actorSystem.ActorOf(deliveryActorProps, "delivery");

              Props storeAndForwardActorProps = Props.Create(() => new StoreAndForwardActor(new StubStore(true), deliveryActor));

                IActorRef storeAndForwardActor = _actorSystem.ActorOf(storeAndForwardActorProps, "storeandforward");

                storeAndForwardActor.Tell(new SampleMessage());
        }

        [Fact]
        public void Given_message_unable_to_store_then_raise_error()
        {
              Props deliveryActorProps = Props.Create(() => new DeliveryActor(new SampleSender()));

            IActorRef deliveryActor = _actorSystem.ActorOf(deliveryActorProps, "delivery");
              Props storeAndForwardActorProps = Props.Create(() => new StoreAndForwardActor(new StubStore(false), deliveryActor));

                IActorRef storeAndForwardActor = _actorSystem.ActorOf(storeAndForwardActorProps, "storeandforward");

                storeAndForwardActor.Tell(new SampleMessage());

                Assert.Equal(1, _monitorActor.Ask<HowManyStorageFailures>(new HowManyStorageFailures()).Result.Count);
        }

        [Fact]
        public void Given_stored_message_Then_should_send()
        {
              Props deliveryActorProps = Props.Create(() => new DeliveryActor(new SampleSender()));

            IActorRef deliveryActor = _actorSystem.ActorOf(deliveryActorProps, "delivery");

              Props storeAndForwardActorProps = Props.Create(() => new StoreAndForwardActor(new StubStore(true), deliveryActor));

                IActorRef storeAndForwardActor = _actorSystem.ActorOf(storeAndForwardActorProps, "storeandforward");

                storeAndForwardActor.Tell(new SampleMessage());

                // don't like this but...
                System.Threading.Thread.Sleep(1000);

                Assert.Equal(1, _monitorActor.Ask<HowManyDeliveries>(new HowManyDeliveries()).Result.Sent);

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

    public class SampleSender : ISend
    {
        public HttpResponseMessage Send(object message)
        {
/*            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:60095");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return httpClient
                    .PostAsync("api/someendpoint", new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(message), System.Text.Encoding.UTF8, "application/json"))
                    .Result;
*/
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        }

    }
}
