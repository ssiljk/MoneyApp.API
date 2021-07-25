using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyApp.API.Context;
using MoneyApp.API.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MoneyApp.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BuyController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<BuyController> _logger;
        private readonly IConfiguration _configuration;

        public BuyController(DataContext context,
                             ILogger<BuyController> logger,
                             IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }
        [HttpPost]

        public async Task<IActionResult> Post([FromBody] BuyInfo buyInfo)
        {
            Dictionary<string, string> currencyUrl = new Dictionary<string, string>();
            var valuesSection = _configuration.GetSection("MoneySettings:MoneyValues");
            foreach (IConfigurationSection section in valuesSection.GetChildren())
            {
                currencyUrl.Add(section.GetValue<string>("currency"), section.GetValue<string>("currencyUrl"));
            }

            if (!currencyUrl.ContainsKey(buyInfo.CurrencyName))
            {
                _logger.LogWarning("Currency " + buyInfo.CurrencyName + " not allowed");
                return BadRequest("Currency " + buyInfo.CurrencyName + " not allowed");
            }

            if (buyInfo.QuantityToInvest <= 0)
            {
                _logger.LogError("Quantity to invest must be a positive number");
                return BadRequest("Quantity to invest must be a positive number");
            }


            string[] quote;
            string quoteUrl;

            if ((currencyUrl[buyInfo.CurrencyName] == string.Empty) && (buyInfo.CurrencyName == "real"))
            {
                quoteUrl = currencyUrl["dolar"];
            }
            else
            {
                quoteUrl = currencyUrl[buyInfo.CurrencyName];
            }

            if(quoteUrl == string.Empty)
            {
                _logger.LogError("Error URL not defined for currency " + buyInfo.CurrencyName);
                return StatusCode(500);
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(quoteUrl))
                    {
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        quote = JsonConvert.DeserializeObject<string[]>(apiResponse);

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while calling Quote service" + ex.ToString());
                return StatusCode(500);
            }

            QuoteResponse resp = new QuoteResponse();
            try
            {
                resp.BuyPrice = Convert.ToDecimal(quote[0], new CultureInfo("en-US"));
                resp.SalePrice = Convert.ToDecimal(quote[1], new CultureInfo("en-US"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while making calculations" + ex.ToString());
                return StatusCode(500);
            }

            if (buyInfo.CurrencyName == "real" && currencyUrl["real"] == string.Empty)
            {
                resp.BuyPrice = resp.BuyPrice / 4;
                resp.SalePrice = resp.SalePrice / 4;
            }

            decimal totalAmountByUser;
            resp.TimeInfo = quote[2];
            
            totalAmountByUser = (decimal)_context.Transactions.Where(t => t.UserId == buyInfo.UserId).
                                                              Where(c => c.CurrencyName == buyInfo.CurrencyName).
                                                              Where(date => date.TransactionDate > DateTime.Today.AddDays((DateTime.Today.Day - 1) * -1)).
                                                              Select(t => t.Amount).Sum();
           

            if (resp.SalePrice == 0)
            {
                _logger.LogError("Division by 0 exception");
                return StatusCode(500);
            }

            var totalAmountFinal = totalAmountByUser + buyInfo.QuantityToInvest / resp.SalePrice;

            if (((totalAmountFinal > 200) && buyInfo.CurrencyName =="dolar") || ((totalAmountFinal > 300) && buyInfo.CurrencyName == "real"))
            {
                _logger.LogWarning("Total over established monthly limit for userId " + buyInfo.UserId);
                return BadRequest("Cannot process transaction because is over established monthly limit for userid");
            }

                      
            var transaction = new Transaction();
            transaction.UserId = buyInfo.UserId;
            transaction.CurrencyName = buyInfo.CurrencyName;
            transaction.Amount = buyInfo.QuantityToInvest / resp.SalePrice;
            transaction.TransactionDate = DateTime.Now;
            _context.Transactions.Add(transaction);

            if((await _context.SaveChangesAsync()) > 0)
            {
                return Ok(transaction);
            }
            else
            {
                _logger.LogError("Error while trying to save changes");
                return StatusCode(500);
            }
           
        }
    }
    
}
