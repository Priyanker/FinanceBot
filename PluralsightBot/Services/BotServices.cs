using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace FinanceBot.Services
{
    public class BotServices
    {
        public BotServices(IConfiguration configuration)
        {
            // Read the setting for cognitive services (LUIS, QnA) from the appsettings.json
            Dispatch = new LuisRecognizer(new LuisApplication(
                configuration["LuisAppId"],
                configuration["LuisAPIKey"],
                $"https://{configuration["LuisAPIHostName"]}.api.cognitive.microsoft.com"),
                new LuisPredictionOptions { IncludeAllIntents = true, IncludeInstanceData = true },
                true);
        }

        public LuisRecognizer Dispatch { get; private set; }
        public EntityModel FindCompanyName(IList<EntityModel> entities)
        {
            return (entities as List<EntityModel>).Find(ent => ent.Type.Contains("company", StringComparison.OrdinalIgnoreCase));
        }
        public EntityModel FindYear(IList<EntityModel> entities)
        {
            return (entities as List<EntityModel>).Find(ent => ent.Type.Contains("number", StringComparison.OrdinalIgnoreCase));
        }
        public IList<EntityModel> FindQuarter(IList<EntityModel> entities)
        {
            return (entities as List<EntityModel>).FindAll(ent => ent.Type.Equals("period", StringComparison.OrdinalIgnoreCase));
        }
        public EntityModel FindCurrencySymbol(IList<EntityModel> entities)
        {
            return (entities as List<EntityModel>).Find(ent => ent.Type.Contains("builtin.currency", StringComparison.OrdinalIgnoreCase));
        }

        
    }
}
