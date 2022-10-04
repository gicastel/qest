using System.Collections.Generic;

namespace qest.Models
{
    public class TestStep
    {
        public string Name { get; set; }
        public TestCommand Command { get; set; }
        public ResultGroup Results { get; set; }
        public List<Assert>? Asserts { get; set; }
    }
}
