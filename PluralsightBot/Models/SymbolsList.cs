using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceBot.Models
{
    public class SymbolsList
    {
        public List<Symbol> symbolsList { get; set; }
    }
    public class Symbol
    {
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("symbol", Required = Newtonsoft.Json.Required.Default, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string SymbolId { get; set; }
        public double Price { get; set; }
    }
}
