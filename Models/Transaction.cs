using System;
using System.Collections.Generic;

#nullable disable

namespace MoneyApp.API.Models
{
    public partial class Transaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public decimal? Amount { get; set; }
        public string CurrencyName { get; set; }
        public DateTime? TransactionDate { get; set; }
    }
}
