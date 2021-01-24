using System;

namespace Lab7.Models
{
    public class Flow
    {
        public Flow(string info)
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            Number = -1;
            Info = info;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public string Info { get; set; }
    }
}
