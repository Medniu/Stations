using System;
using System.Text;
using System.Threading;
using System.Configuration;
using System.Collections.Specialized;
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
                HostName = ConfigurationManager.AppSettings["hostName"],
                Port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]),
                VirtualHost = ConfigurationManager.AppSettings["virtualHost"],
                Password = ConfigurationManager.AppSettings["password"],
                UserName = ConfigurationManager.AppSettings["userName"]
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                Thread thread = new Thread(() => Status(connection, station));
                thread.Start(); // запускаем поток

                string message = $"{station.Id} {station.Latitude} {station.Longitude}";
                var body = Encoding.UTF8.GetBytes(message.Replace(",", "."));

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
                    Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message);               
                    var splittedMessage = message.Split(" ");

                    station.Status = 1;
                    int chargeValue = 0;

                    for (; chargeValue <= Convert.ToInt32(splittedMessage[1]); chargeValue += 5) {

                        var callbackMessage = Encoding.UTF8.GetBytes($"{splittedMessage[0]} {chargeValue}");
                        channel.BasicPublish(exchange: "InboundExchange",
                                     routingKey: "Charging",
                                     basicProperties: null,
                                     body: callbackMessage);
                        Thread.Sleep(500);
                    }
                    station.Status = 0;
                };
                channel.BasicConsume(queue: $"{station.Id}",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
                channel.QueueDelete($"{station.Id}");
            }
        }
        public static void Status(IConnection connection, Station station)
        {
            using (var channel = connection.CreateModel())
            {                             
                var body2 = Encoding.UTF8.GetBytes($"{station.Id} {station.Status}");
                for (; ; )
                {
                    channel.BasicPublish(exchange: "InboundExchange",
                                         routingKey: "Status",
                                         basicProperties: null,
                                         body: body2);
                    Thread.Sleep(500);
                }
            }           
        }
    }
}
