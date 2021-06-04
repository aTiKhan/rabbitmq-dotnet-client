using System;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client.Events;

using Xunit;

namespace RabbitMQ.Client.Unit
{

    public class TestBasicPublish
    {
        [Fact]
        public void TestBasicRoundtripArray()
        {
            var cf = new ConnectionFactory();
            using(IConnection c = cf.CreateConnection())
            using(IModel m = c.CreateModel())
            {
                QueueDeclareOk q = m.QueueDeclare();
                IBasicProperties bp = m.CreateBasicProperties();
                byte[] sendBody = System.Text.Encoding.UTF8.GetBytes("hi");
                byte[] consumeBody = null;
                var consumer = new EventingBasicConsumer(m);
                var are = new AutoResetEvent(false);
                consumer.Received += async (o, a) =>
                {
                    consumeBody = a.Body.ToArray();
                    are.Set();
                    await Task.Yield();
                };
                string tag = m.BasicConsume(q.QueueName, true, consumer);

                m.BasicPublish("", q.QueueName, bp, sendBody);
                bool waitResFalse = are.WaitOne(2000);
                m.BasicCancel(tag);

                Assert.True(waitResFalse);
                Assert.Equal(sendBody, consumeBody);
            }
        }

        [Fact]
        public void TestBasicRoundtripCachedString()
        {
            var cf = new ConnectionFactory();
            using(IConnection c = cf.CreateConnection())
            using(IModel m = c.CreateModel())
            {
                CachedString exchangeName = new CachedString(string.Empty);
                CachedString queueName = new CachedString(m.QueueDeclare().QueueName);
                IBasicProperties bp = m.CreateBasicProperties();
                byte[] sendBody = System.Text.Encoding.UTF8.GetBytes("hi");
                byte[] consumeBody = null;
                var consumer = new EventingBasicConsumer(m);
                var are = new AutoResetEvent(false);
                consumer.Received += async (o, a) =>
                {
                    consumeBody = a.Body.ToArray();
                    are.Set();
                    await Task.Yield();
                };
                string tag = m.BasicConsume(queueName.Value, true, consumer);

                m.BasicPublish(exchangeName, queueName, bp, sendBody);
                bool waitResFalse = are.WaitOne(2000);
                m.BasicCancel(tag);

                Assert.True(waitResFalse);
                Assert.Equal(sendBody, consumeBody);
            }
        }

        [Fact]
        public void TestBasicRoundtripReadOnlyMemory()
        {
            var cf = new ConnectionFactory();
            using(IConnection c = cf.CreateConnection())
            using(IModel m = c.CreateModel())
            {
                QueueDeclareOk q = m.QueueDeclare();
                IBasicProperties bp = m.CreateBasicProperties();
                byte[] sendBody = System.Text.Encoding.UTF8.GetBytes("hi");
                byte[] consumeBody = null;
                var consumer = new EventingBasicConsumer(m);
                var are = new AutoResetEvent(false);
                consumer.Received += async (o, a) =>
                {
                    consumeBody = a.Body.ToArray();
                    are.Set();
                    await Task.Yield();
                };
                string tag = m.BasicConsume(q.QueueName, true, consumer);

                m.BasicPublish("", q.QueueName, bp, new ReadOnlyMemory<byte>(sendBody));
                bool waitResFalse = are.WaitOne(2000);
                m.BasicCancel(tag);

                Assert.True(waitResFalse);
                Assert.Equal(sendBody, consumeBody);
            }
        }

        [Fact]
        public void CanNotModifyPayloadAfterPublish()
        {
            var cf = new ConnectionFactory();
            using(IConnection c = cf.CreateConnection())
            using(IModel m = c.CreateModel())
            {
                QueueDeclareOk q = m.QueueDeclare();
                IBasicProperties bp = m.CreateBasicProperties();
                byte[] sendBody = new byte[1000];
                var consumer = new EventingBasicConsumer(m);
                var are = new AutoResetEvent(false);
                bool modified = true;
                consumer.Received += (o, a) =>
                {
                    if (a.Body.Span.IndexOf((byte)1) < 0)
                    {
                        modified = false;
                    }
                    are.Set();
                };
                string tag = m.BasicConsume(q.QueueName, true, consumer);

                m.BasicPublish("", q.QueueName, bp, sendBody);
                sendBody.AsSpan().Fill(1);

                Assert.True(are.WaitOne(2000));
                Assert.False(modified, "Payload was modified after the return of BasicPublish");

                m.BasicCancel(tag);
            }
        }
    }
}
