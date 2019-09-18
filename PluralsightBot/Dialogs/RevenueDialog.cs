using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using FinanceBot.Helpers;
using FinanceBot.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FinanceBot.Models;
using System.Collections.Generic;

namespace FinanceBot.Dialogs
{
    public class RevenueDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private static HttpClient _httpClient = new HttpClient();
        #endregion  


        public RevenueDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            _httpClient.BaseAddress = new Uri("https://financialmodelingprep.com/api/v3/");
            _httpClient.Timeout = new TimeSpan(0, 0, 30);
            _httpClient.DefaultRequestHeaders.Clear();

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(RevenueDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(RevenueDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            RecognizerResult result = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            LuisResult luisResult = result.Properties["luisResult"] as LuisResult;
            if(luisResult.Entities.Count == 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Please provide a company name.")), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            EntityModel companyName = (luisResult.Entities as List<EntityModel>).Find(ent => ent.Type.Contains("company", StringComparison.OrdinalIgnoreCase));
            EntityModel year = (luisResult.Entities as List<EntityModel>).Find(ent => ent.Type.Contains("number", StringComparison.OrdinalIgnoreCase));
            var symbols = await GetSymbolsList();
            var symbol = symbols.symbolsList.Find(symbolObject => symbolObject.Name.Contains(companyName.Entity, StringComparison.OrdinalIgnoreCase) || symbolObject.SymbolId.Equals(companyName.Entity, StringComparison.OrdinalIgnoreCase));
            if(symbol!= null)
            {
                var symbolFinancialData = await GetSymbolsData(symbol.SymbolId, Int32.Parse(year?.Entity ?? DateTime.Now.Year.ToString()));
                if(symbolFinancialData != null)
                {
                    if(!symbolFinancialData.Date.Contains(year?.Entity ?? DateTime.Now.Year.ToString()))
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Financial data of {0} is not yet available for year {1}", symbol.Name, DateTime.Now.Year.ToString())), cancellationToken);
                    }
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Revenue of {0} in {1} is {2} million", symbol.Name, DateTime.Parse(symbolFinancialData.Date).Year, Double.Parse(symbolFinancialData.Revenue) / 1000000)), cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("No financial data available for {0} in the year {1}", symbol.Name, year.Entity)), cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Please enter a valid company name.")), cancellationToken);
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        public async Task<SymbolsList> GetSymbolsList()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "company/stock/list?datatype=json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SymbolsList>(content);

        }
        public async Task<FinancialData> GetSymbolsData(string symbolId, int year)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "financials/income-statement/" + symbolId);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            IncomeStatementModel incomeModel = JsonConvert.DeserializeObject<IncomeStatementModel>(content);
            var financial = incomeModel.Financials.Find(financialData => DateTime.Parse(financialData.Date).Year == year);
            if(financial == null && year == DateTime.Now.Year)
            {
                return incomeModel.Financials.Find(financialData => DateTime.Parse(financialData.Date).Year == year-1);
            }
            else
            {
                return financial;
            }
        }
    }
}
