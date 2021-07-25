using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using MoneyApp.API.Models;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace MoneyApp.API.Controllers
{ 
    [Route("[controller]")]
    [ApiController]
    public class QuoteController : ControllerBase
    {
        private readonly ILogger<QuoteController> _logger;

        public QuoteController(ILogger<QuoteController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{currencyName}") ]
        public async Task<IActionResult> Get(string currencyName)
        {
            Dictionary <string,string> currencyUrl = new  Dictionary<string,string> ();
            currencyUrl.Add("dolar", "https://www.bancoprovincia.com.ar/Principal/Dolar");
            currencyUrl.Add("real", "");
            //currencyUrl.Add("dolarcan", "url for dolarcan");

            if (!currencyUrl.ContainsKey(currencyName))
            {
                _logger.LogWarning("Currency " + currencyName + " not allowed");
                return BadRequest("Currency " + currencyName + " not allowed");
            }
           // if (currencyName == "dolarcan" && currencyUrl["dolarcan"] == string.Empty)
           //     return BadRequest("Currency dolarcan not implemented yet");

            string[] quote;
            string quoteUrl;

            if (currencyUrl[currencyName] == string.Empty)
                quoteUrl = currencyUrl["dolar"];
            else
                quoteUrl = currencyUrl[currencyName];

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
            catch (Exception)
            {
                _logger.LogError("Error while calling Quote service");
                return StatusCode(500);
            }

            QuoteResponse resp = new QuoteResponse();
            try
            {
                resp.BuyPrice = Convert.ToDecimal(quote[0], new CultureInfo("en-US"));
                resp.SalePrice = Convert.ToDecimal(quote[1], new CultureInfo("en-US"));
            }
            catch (Exception)
            {
                _logger.LogError("Error while making calculations");
                return StatusCode(500);
            }
           
            if (currencyName == "real" && currencyUrl["real"] == string.Empty)
            {
                resp.BuyPrice = resp.BuyPrice/4;
                resp.SalePrice = resp.SalePrice/4;
            }

            resp.TimeInfo = quote[2];

            return Ok(resp);
        }
    }
}
