using FinanceBot.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FinanceBot.Services
{
    public class FinancialServices: IFinancialServices
    {
        #region Variables
        private static HttpClient _httpClient = new HttpClient();
        private IMemoryCache _memoryCache;
        #endregion 

        public FinancialServices(IMemoryCache memoryCache)
        {
            _httpClient.BaseAddress = new Uri("https://financialmodelingprep.com/api/v3/");
            _httpClient.Timeout = new TimeSpan(0, 0, 30);
            _httpClient.DefaultRequestHeaders.Clear();
            _memoryCache = memoryCache;
        }
        public async Task<SymbolsList> GetSymbolsList()
        {
            SymbolsList symbolsList;
            bool isSymbolListExist = _memoryCache.TryGetValue("cachesymbols", out symbolsList);
            if (!isSymbolListExist)
            {
                symbolsList = await GetListedSymbols();
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _memoryCache.Set("cachesymbols", symbolsList, cacheEntryOptions);
            }
            return symbolsList;
           
        }
        public async Task<FinancialData> GetAnnualFinancialData(string symbolId, int year)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "financials/income-statement/" + symbolId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            IncomeStatementModel incomeModel = JsonConvert.DeserializeObject<IncomeStatementModel>(content);
            var financial = incomeModel.Financials.Find(financialData => DateTime.Parse(financialData.Date).Year == year);
            if (financial == null && year == DateTime.Now.Year)
            {
                return incomeModel.Financials.Find(financialData => DateTime.Parse(financialData.Date).Year == year - 1);
            }
            else
            {
                return financial;
            }
        }
        public async Task<FinancialData> GetQuarterlyFinancialData(string symbolId, int year, EntityModel periodEntity)
        {
            var queryString = new Dictionary<string, string>()
            {
                { "period", "quarter" }
            };
            var periods = new Dictionary<int, string[]>()
            {
                { 1, new string[] { "1st", "first", "I" } },
                { 2, new string[] { "2nd", "second", "II" } },
                { 3, new string[] { "3rd", "third", "III" } },
                { 4, new string[] { "4th", "fourth", "IV" } },
            };
            int period = 1;
            foreach(KeyValuePair<int, string[]> entry in periods)
            {
                if(entry.Value.Contains(periodEntity.Entity))
                {
                    period = entry.Key;
                }
            }
            var requestUri = QueryHelpers.AddQueryString("financials/income-statement/" + symbolId, queryString);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            IncomeStatementModel incomeModel = JsonConvert.DeserializeObject<IncomeStatementModel>(content);
            var financial = incomeModel.Financials.Find(financialData => DateTime.Parse(financialData.Date).Year == year && GetQuarter(DateTime.Parse(financialData.Date).Month) == period);
            if (financial == null && year == DateTime.Now.Year)
            {
                return incomeModel.Financials.Find(financialData => DateTime.Parse(financialData.Date).Year == year - 1 && GetQuarter(DateTime.Parse(financialData.Date).Month) == period);
            }
            else
            {
                return financial;
            }
        }
        private async Task<SymbolsList> GetListedSymbols()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "company/stock/list?datatype=json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SymbolsList>(content);

        }
        private int GetQuarter(int month)
        {
            if(month >=1 && month <=3)
            {
                return 1;
            }
            else if(month >=4 && month <=6)
            {
                return 2;
            }
            else if(month >= 7 && month <= 10)
            {
                return 3; 
            }
            else
            {
                return 4;
            }
        }
    }
}
