using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyApp.API.Models
{
    public class BuyInfo
    {
        public string UserId { get; set; }
        public int QuantityToInvest { get; set; }

        public string CurrencyName { get; set; }

    }
}
