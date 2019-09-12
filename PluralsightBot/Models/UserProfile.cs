using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceBot.Models
{
    // Defines a state property used to track information about the user.
    public class UserProfile
    {
        public string Name { get; set; }

        public string Description { get; set; }
        public DateTime CallbackTime { get; set; }

        public string PhoneNumber { get; set; }

        public string Bug { get; set; }
    }
}
