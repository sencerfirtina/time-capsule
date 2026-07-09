using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.DTO
{
    public class OpenMeteoResponse
    {
        public required CurrentWeatherObject current_weather { get; set; }
    }
    
    public class CurrentWeatherObject
    {
        public double temperature { get; set; }
    }
}