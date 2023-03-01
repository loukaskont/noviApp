using System;

namespace noviApp
{
    public class IP_Details
    {
        public int Id { get; set; }
        public String IP { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Country country { get; set; }
    }
}
