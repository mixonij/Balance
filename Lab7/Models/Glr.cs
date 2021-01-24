using System.Collections.Generic;

namespace Lab7.Models
{
    public class Glr
    {
        public List<Flow> FlowErrors { get; set; }

        public List<Variable> FlowCorrections { get; set; }

        public double TestValue { get; set; }
    }
}
