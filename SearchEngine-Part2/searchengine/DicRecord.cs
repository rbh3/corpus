using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    [Serializable]
    class DicRecord// use dictionary details on the term
    {
        public int TotalFreq;
        public int Pointer;
        public bool isCache;
        public int df;
        public double idf;
    }
}
