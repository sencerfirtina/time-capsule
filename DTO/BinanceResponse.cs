using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.DTO
{
    public class BinanceResponse
    {
        public required string symbol { get; set; }
        public required string price { get; set; }
    }
}