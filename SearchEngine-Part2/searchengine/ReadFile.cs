using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SearchEngine
{
    class ReadFile
    {
        string path;
        public List<string> files { get; }
        public StringBuilder sb;
        public ReadFile(string path)
        {
            this.path = path;
            sb = new StringBuilder();
            files = new List<string>();
            if (Directory.Exists(path+@"\corpus\"))
                ProcessDirectory(path + @"\corpus\");
        }
        //put all the files in array from all the folders
        private void ProcessDirectory(string targetDirectory)
        { 
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                if(!fileName.Equals(path+@"\stop_words.txt"))
                      this.files.Add(fileName);
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }
        //seperate each file to its doc and each text of the doc
        public Dictionary<string,string> ProccessDocs(string s)
        {
             Dictionary<string, string> Docs = new Dictionary<string, string>();
             string content = File.ReadAllText(s);
             string[] values = content.Split(new string[] { "<DOC>", "</DOC>" }, StringSplitOptions.RemoveEmptyEntries);
             foreach(string str in values)
             {
                
                if(str.IndexOf("<TEXT>")!=-1)
                {
                    string[] docno = str.Split(new string[] { "<DOCNO>", "</DOCNO>" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    int ind = s.IndexOf("corpus");
                    string path = s.Substring(ind);
                    sb.AppendLine(docno[1].Trim(' ') + "@" + path);

                    string[] Text = str.Split(new string[] { "<TEXT>", "</TEXT>" }, StringSplitOptions.RemoveEmptyEntries);
                    Docs.Add(docno[1], Text[1]);
                }
                   
             }
            return Docs;
        }
    }
}
