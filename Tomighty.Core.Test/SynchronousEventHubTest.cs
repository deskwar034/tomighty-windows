using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tomighty
{
    [TestFixture]
    public class SynchronousEventHubTest
    {
        [Test]
        public void SubscribeAndPublishFromDifferentTasksShouldNotThrow()
        {
            var hub = new SynchronousEventHub();
            hub.Subscribe<string>(_ => { });

            var publishTask = Task.Run(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    hub.Publish("tick");
                }
            });

            var subscribeTask = Task.Run(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    hub.Subscribe<string>(_ => { });
                }
            });

            Assert.DoesNotThrow(() => Task.WaitAll(publishTask, subscribeTask));
        }
    }
}
