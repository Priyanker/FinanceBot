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
            var result = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            var luisResult = result.Properties["luisResult"] as LuisResult;
            if(luisResult.Entities.Count == 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Please provide a company name.")), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            var entity = luisResult.Entities[0];
            
            var symbols = await GetSymbolsList();
            var symbol = symbols.symbolsList.Find(symbolObject => symbolObject.Name.Contains(entity.Entity, StringComparison.OrdinalIgnoreCase));
            if(symbol!= null)
            {
                var symbolFinancialData = await GetSymbolsData(symbol.SymbolId);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Revenue of {0} is {1}", symbol.Name, symbolFinancialData.Financials[0].Revenue)), cancellationToken);
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
        public async Task<IncomeStatementModel> GetSymbolsData(string symbolId)
        {
            string url = "https://financialmodelingprep.com/api/v3/financials/income-statement/" + symbolId;
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IncomeStatementModel>(content);

        }
    }
}
