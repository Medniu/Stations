using System;
using System.Collections.Generic;
using System.Text;

namespace StationConsumer
{
    public class Station
    {
        public Guid Id { get; set; } 
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public int Status { get; set; }

    }
}
