using FinanceBot.Models;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceBot.Services
{
    public interface IFinancialServices
    {
        Task<SymbolsList> GetSymbolsList();
        Task<FinancialData> GetAnnualFinancialData(string symbolId, int year);

        Task<FinancialData> GetQuarterlyFinancialData(string symbolId, int year, EntityModel periodEntity);
        
    }

}
