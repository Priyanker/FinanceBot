using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using FinanceBot.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FinanceBot.Dialogs
{
    public class QuarterlyRevenueDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IFinancialServices _financialServices;
        #endregion  


        public QuarterlyRevenueDialog(string dialogId, BotStateService botStateService, BotServices botServices, IFinancialServices financialServices) : base(dialogId)
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
            AddDialog(new WaterfallDialog($"{nameof(QuarterlyRevenueDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(QuarterlyRevenueDialog)}.mainFlow";
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
            List<EntityModel> quarterPeriod = (List<EntityModel>)_botServices.FindQuarter(luisResult.Entities);
            var symbols = await _financialServices.GetSymbolsList();
            var symbol = symbols.symbolsList.Find(symbolObject => symbolObject.Name.Contains(companyName.Entity, StringComparison.OrdinalIgnoreCase) || symbolObject.SymbolId.Equals(companyName.Entity, StringComparison.OrdinalIgnoreCase));
            if (symbol != null)
            {
                if (quarterPeriod.Exists(periodEntity => periodEntity.Entity.Equals("quarter", StringComparison.OrdinalIgnoreCase)))
                {
                    var symbolFinancialData = await _financialServices.GetQuarterlyFinancialData(symbol.SymbolId, Int32.Parse(year?.Entity ?? DateTime.Now.Year.ToString()), quarterPeriod.Find(periodEntity => !periodEntity.Entity.Equals("quarter", StringComparison.OrdinalIgnoreCase)));
                    if (symbolFinancialData != null && symbolFinancialData.Revenue != "")
                    {
                        if (!symbolFinancialData.Date.Contains(year?.Entity ?? DateTime.Now.Year.ToString()))
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Financial data of {0} is not yet available for year {1}", symbol.Name, DateTime.Now.Year.ToString())), cancellationToken);
                        }
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Revenue of {0} in the {1} quarter of {2} is {3} million", symbol.Name, quarterPeriod.Find(periodEntity => !periodEntity.Entity.Equals("quarter", StringComparison.OrdinalIgnoreCase)).Entity, DateTime.Parse(symbolFinancialData.Date).Year, Double.Parse(symbolFinancialData.Revenue) / 1000000)), cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("No financial data available for {0} in the year {1}", symbol.Name, year.Entity)), cancellationToken);
                    }
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

