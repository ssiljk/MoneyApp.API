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
using Microsoft.Extensions.Configuration;

namespace MoneyApp.API.Controllers
{ 
    [Route("[controller]")]
    [ApiController]
    public class QuoteController : ControllerBase
    {
        private readonly ILogger<QuoteController> _logger;
        private readonly IConfiguration _configuration;

        public QuoteController(ILogger<QuoteController> logger,
                               IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("{currencyName}") ]
        public async Task<IActionResult> Get(string currencyName)
        {
            Dictionary<string, string> currencyUrl = new Dictionary<string, string>();
            var valuesSection = _configuration.GetSection("MoneySettings:MoneyValues");
            foreach (IConfigurationSection section in valuesSection.GetChildren())
            {
                currencyUrl.Add(section.GetValue<string>("currency"), section.GetValue<string>("currencyUrl"));
            }
           
            if (!currencyUrl.ContainsKey(currencyName))
            {
                _logger.LogWarning("Currency " + currencyName + " not allowed");
                return BadRequest("Currency " + currencyName + " not allowed");
            }
         
            string[] quote;
            string quoteUrl;

            if ((currencyUrl[currencyName] == string.Empty) && (currencyName == "real"))
            {
                quoteUrl = currencyUrl["dolar"];
            }
            else
            {
                quoteUrl = currencyUrl[currencyName];
            }

            if (quoteUrl == string.Empty)
            {
                _logger.LogError("Error URL not defined for currency " + currencyName);
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
