using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyApp.API.Models
{
    public class QuoteResponse
    {
       public decimal SalePrice { get; set; }
       public decimal BuyPrice { get; set; }

        public string TimeInfo { get; set; }

    }
}
