﻿using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using FinanceBot.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinanceBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        #region Variables
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IFinancialServices _financialServices;
        #endregion  


        public MainDialog(BotStateService botStateService, BotServices botServices, IFinancialServices financialServices) : base(nameof(MainDialog))
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
            AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _botStateService));
            AddDialog(new RevenueDialog($"{nameof(MainDialog)}.revenue", _botStateService, _botServices, _financialServices));
            AddDialog(new QuarterlyRevenueDialog($"{nameof(MainDialog)}.quarterlyRevenue", _botStateService, _botServices, _financialServices));
            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(MainDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            try
            {
                var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                // Top intent tell us which cognitive service to use.
                var topIntent = recognizerResult.GetTopScoringIntent();
                
                switch (topIntent.intent)
                {
                    case "GreetingIntent":
                        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
                    case "FindRevenueIntent":
                        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.revenue", null, cancellationToken);
                    case "FindQuarterlyRevenueIntent":
                        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.quarterlyRevenue", null, cancellationToken);
                    default:
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I'm sorry I don't know what you mean."), cancellationToken);
                        break;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
