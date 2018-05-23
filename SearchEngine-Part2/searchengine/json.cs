using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    class json// Json Parser made by http://json2csharp.com/
    {
        public class Term //class Term
        {
            public string term { get; set; }
            public int canonical { get; set; }
            public int oskill { get; set; }
        }

        public class RootObject //class page return from API
        {
            public int http { get; set; }
            public string message { get; set; }
            public List<Term> terms { get; set; }
        }
    }
}
