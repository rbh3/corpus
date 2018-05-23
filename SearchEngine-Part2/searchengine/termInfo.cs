using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    [Serializable]
    public class termInfo // use for details on the term
    {
        public int tf { get; set; } 
        public bool hdr { get; set; }

        public termInfo(int t,int i)
        {
            this.tf = t;
            hdr = (i < 25);//if in header
        }

        public termInfo(int t, bool b)
        {
            this.tf = t;
            hdr =b;
        }


    }
}
