using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceBot.Models
{
    public class IncomeStatementModel
    {
        public string Symbol { get; set; }
        public List<FinancialData> Financials { get; set; }
    }
}
