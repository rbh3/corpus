using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    [Serializable]
    public class PostingInfo
    {
        public termInfo term;// term info object
        public string DocNo;

        public PostingInfo(termInfo term,string docNo)
        {
            this.term = term;
            this.DocNo = docNo;
        }

        public override string ToString() { //for display
            StringBuilder sb = new StringBuilder();
            sb.Append(DocNo+",");
            sb.Append("tf: " + term.tf + ",Header: " + term.hdr +" ; ");
            return (sb.ToString());

        }




    }


}
