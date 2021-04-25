using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace StationConsumer
{
    class Program
    {
        static void Main(string[] args)
        {

            Station station = new Station
            {
                Id = Guid.NewGuid(),
                Latitude = GpsSensor.GetLatitudeCoordinate(),
                Longitude = GpsSensor.GetLongitudeCoordinate(),
                Status = 0
            };

            var factory = new ConnectionFactory() {
                HostName = "20.52.186.170",
                Port = 5672,
                VirtualHost = "/",
                Password = "admin",
                UserName = "admin"
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                string message = $"{station.Id} {station.Latitude} {station.Longitude}";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "InboundExchange",
                                     routingKey: "Registration",
                                     basicProperties: null,
                                     body: body);


                channel.QueueDeclare(queue: $"{station.Id}",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

                channel.QueueBind(queue: $"{station.Id}",
                        exchange: "OutboundExchange",
                        routingKey: $"{station.Id}");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;
                    Console.WriteLine(" [x] Received '{0}':'{1}'",
                                      routingKey, message);
                };
                channel.BasicConsume(queue: $"{station.Id}",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
                channel.QueueDelete($"{station.Id}");
            }
        }
    }
}
