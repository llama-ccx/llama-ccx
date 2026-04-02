using System;

namespace Llama.Core.License
{
    public class User
    {
        public string user_name { get; set; }
        public string mac_address { get; set; }
        public DateTime expiring_date { get; set; }
    }
}
