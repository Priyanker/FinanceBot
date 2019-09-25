using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using FinanceBot.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceBot.Dialogs
{
    public class RevenueDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IFinancialServices _financialServices;
        #endregion  


        public RevenueDialog(string dialogId, BotStateService botStateService, BotServices botServices, IFinancialServices financialServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            _financialServices = financialServices ?? throw new System.ArgumentNullException(nameof(financialServices));
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
            if (luisResult.Entities.Count == 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Please provide a company name.")), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
            EntityModel companyName = _botServices.FindCompanyName(luisResult.Entities);
            EntityModel year = _botServices.FindYear(luisResult.Entities);
            EntityModel money = _botServices.FindCurrencySymbol(luisResult.Entities);
            var symbols = await _financialServices.GetSymbolsList();
            var symbol = symbols.symbolsList.Find(symbolObject => symbolObject.Name.Contains(companyName.Entity, StringComparison.OrdinalIgnoreCase) || symbolObject.SymbolId.Equals(companyName.Entity, StringComparison.OrdinalIgnoreCase));
            if (symbol != null)
            {
                var symbolFinancialData = await _financialServices.GetAnnualFinancialData(symbol.SymbolId, Int32.Parse(year?.Entity ?? DateTime.Now.Year.ToString()));
                if (symbolFinancialData != null)
                {
                    if (!symbolFinancialData.Date.Contains(year?.Entity ?? DateTime.Now.Year.ToString()))
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Financial data of {0} is not yet available for year {1}", symbol.Name, DateTime.Now.Year.ToString())), cancellationToken);
                    }
                    if (money!=null && money?.Entity != "usd")
                    {
                        // TODO : Shekar
                        // Check the currency code (money.Entity) return by LUIS is present in valid curency symbol list
                        //If present make a call to currency converter api with inputs as symbolFinancialData.Revenue convert from USD to money.Entity value.
                        //Else let user know the currency symbol is invalid and respond with USD default
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Forex conversion in {0} progress. Happy to help you with the USD !. Revenue of {1} in {2} is {3} million USD", money.Entity, symbol.Name, DateTime.Parse(symbolFinancialData.Date).Year, Double.Parse(symbolFinancialData.Revenue) / 1000000)), cancellationToken);

                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Revenue of {0} in {1} is {2} million", symbol.Name, DateTime.Parse(symbolFinancialData.Date).Year, Double.Parse(symbolFinancialData.Revenue) / 1000000)), cancellationToken);

                    }
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
    }
}
