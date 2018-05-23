using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static SearchEngine.json;

namespace SearchEngine
{
    class Searcher
    {
        String query;
        Parser p;
        Indexer ind;
        Dictionary<string, Dictionary<string,int>> TermQuery;
        Ranker r;
        private int numOfqTerms;
        string path;

        public Searcher(Parser p,Indexer ind,Ranker r,string path)
        {
            this.p = p;
            this.ind = ind;
            this.r = r;
            this.path = path;
        }
        //recive text parse it and return the lines from the posting files
        public void ParseQuery(string q)
        {
            query = q;
            //dictionary of <DocNumber, Term, TF>
            TermQuery = new Dictionary<string, Dictionary<string, int>>();
            //return all the Terms of the query, after parse
            List<string> qTerms= p.Parse(query).Keys.ToList();
            for (int i = 0; i < qTerms.Count; i++)
            {
                //if the terms is on my posting list
                if (ind.dic.ContainsKey(qTerms[i]))
                {
                    //bring the line of the term from the Posting file
                    string line = ind.postingList(qTerms[i][0], ind.dic[qTerms[i]].Pointer);
                    //make the line to an Object
                    string term = line.Substring(0, line.IndexOf("["));
                    line = line.Substring(line.IndexOf(":") + 1);
                    string[] sp = line.Split(new char[] { ',', ';' });
                    for (int j = 0; j < sp.Length - 3; j += 3)
                    { 
                        if (!TermQuery.ContainsKey(sp[j]))
                        {
                            TermQuery.Add(sp[j], new Dictionary<string, int>());
                            TermQuery[sp[j]].Add(term, Int32.Parse(sp[j + 1]));
                        }
                        else
                            TermQuery[sp[j]].Add(term, Int32.Parse(sp[j + 1]));
                    }
                }
            }
           numOfqTerms= qTerms.Count;
        }
        //return list of rated Docs
        public List<string> returnDocs(string s,int amount)
        {
            //Parse Query
            ParseQuery(s);
            //rate the Docs and return them
            return r.rateDocs(TermQuery, numOfqTerms,amount);
        }
        //return 5 sentences summary to a specific doc
        public string FiveSents(string DocNo)
        {
            StringBuilder sb = new StringBuilder();
            //open the Docs - pathes file
            StreamReader sr = new StreamReader(path+@"\docsMap.txt");
            string docpath=null;
            string docText=null;
            for (int i=0;!sr.EndOfStream;i++)
            {
                string text = sr.ReadLine();
                string[] val = text.Split('@');
                if (val.Length > 1)
                {
                    //if Doc name is equal to the parameter
                    if (val[0].Equals(DocNo))
                    {
                        //return the path of the file of the Doc
                        docpath = val[1];
                        break;
                    }
                }
            }
            if (docpath == null)
                return null;
            else
            {
                //read the file
                string content = File.ReadAllText(path+"\\"+docpath);
                //split by name
                string[] values = content.Split(new string[] { "<DOC>", "</DOC>" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string str in values)
                {
                    if (str.IndexOf("<TEXT>") != -1)
                    {
                        string[] docno = str.Split(new string[] { "<DOCNO>", "</DOCNO>" }, StringSplitOptions.RemoveEmptyEntries);
                        //looking for the doc that equal to the parameter
                        if (docno[1].Trim(' ').Equals(DocNo))
                        {
                            //read text
                            string[] Text = str.Split(new string[] { "<TEXT>", "</TEXT>" }, StringSplitOptions.RemoveEmptyEntries);
                            docText = Text[1];
                            break;
                        }
                    }

                }
            }
            if (docText == null)
                return null;
            //split the docs into sentences
            List<string> sents = DocToSents(docText);
            //parse the doc text
            Dictionary<string, termInfo> termsInDoc = p.Parse(docText);
            int DocMaxTF = p.maxtf;
            
            Dictionary<int, double> sentsRankDic = new Dictionary<int, double>();
            
            double[] sentsRankArr = new double[sents.Count];
            for (int i = 0; i < sents.Count; i++)
            {
                //sentsTerms will hold the terms of sents[i] sentence
                List<string> sentTerms = p.Parse(sents[i]).Keys.ToList();

                foreach (string term in sentTerms)
                {
                    //formula of ranking a sentence
                    if (termsInDoc.ContainsKey(term))
                        sentsRankArr[i] += 0.05 * (Convert.ToInt32(termsInDoc[term].hdr)) + 0.95 * ((termsInDoc[term].tf) / DocMaxTF);
                }
                //add the rank
                sentsRankDic.Add(i, sentsRankArr[i]);

            }
            //sort by rank
            List<KeyValuePair<int, double>> sortedPairsList = sentsRankDic.ToList();
            sortedPairsList.Sort(
                delegate (KeyValuePair<int, double> pair1,
                KeyValuePair<int, double> pair2)
                {
                    return pair2.Value.CompareTo(pair1.Value);
                }
            );
            //By now we should have the 5 most popular sentences at the top of sortedPairsList
            List<KeyValuePair<int, int>> FiveSortedPairsList = new List<KeyValuePair<int, int>>();
            for (int i = 0; i< sortedPairsList.Count && i < 5 ; i++)
                FiveSortedPairsList.Add(new KeyValuePair<int, int>(sortedPairsList[i].Key, i + 1));

            //Sorting by key in order to return them by the order of appearance 
            FiveSortedPairsList.Sort((pair1, pair2) => pair1.Key.CompareTo(pair2.Key));


            //print to screen
            for (int i = 0; i < FiveSortedPairsList.Count && i < 5; i++)
            {

                sb.AppendLine((i + 1) + ". Score: " + FiveSortedPairsList[i].Value);
                sb.AppendLine(sents[FiveSortedPairsList[i].Key]);
                sb.AppendLine();
            }
            return sb.ToString();
        }
        //split doc into sentences
        private List<string> DocToSents(string Text)
        {
            List<string> result = new List<string>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];
                if (c != '.')
                {
                    //while its not the end- append
                    sb.Append(c);
                }
                else
                {
                    //check that we r not out of bound
                    if (i < Text.Length - 1)
                    {
                        //check the char of the next position
                        char a = Text[i + 1];
                        //if its still not the end-append
                        if (Char.IsDigit(Text[i + 1]) || Char.IsLetter(Text[i + 1]) || Text[i + 1].Equals(',') || (Text[i - 2].Equals(' ') && char.IsLetter(Text[i - 1])))
                        {
                            sb.Append(c);
                        }
                        else//end of sentence
                        {
                            result.Add(sb.ToString().TrimStart(' ', '\r', '\n'));
                            sb.Clear();

                        }
                    }
                }
            }
            if (sb.Length > 0)// if the string builder isn't empty
            {
                if (!sb.ToString().TrimStart(' ', '\r', '\n').Equals(""))
                    result.Add(sb.ToString().TrimStart(' ', '\r', '\n'));
            }
            sb.Clear();
            return result;//return list of sentences
        }
        //adding synonyms from wikipedia to the query
        public string WikiExp(string q)
        {
            try
            {
                string ans = q;
                WebClient client = new WebClient();
                //Connect to wikipedia page
                using (Stream stream = client.OpenRead("http://wikisynonyms.ipeirotis.com/api/"+q))
                using (StreamReader reader = new StreamReader(stream))
                {//Parse it using JSON Parser
                    JsonSerializer ser = new JsonSerializer();
                    RootObject result = ser.Deserialize<RootObject>(new JsonTextReader(reader));
                    if (result == null)//if not found
                        return q;
                    foreach (Term page in result.terms)// if found add the term to the query
                        if(!page.term.Contains(q))
                            ans += " " + page.term;
                }
                return ans;
            }catch (Exception) { return q; }// if connection isn't good, return without expanding
        }
    }
}
