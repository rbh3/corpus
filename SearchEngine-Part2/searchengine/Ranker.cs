using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    class Ranker
    {
        string path;
        Parser p;
        bool isStem;
        ReadFile r;
        Dictionary<string, double> DocWeight;
        public Dictionary<string, int> Docsmaxtf;
        Dictionary<string, double> docslength;
        Indexer ind;
        double avgDL, k;
        public Ranker(string path,Parser p,ReadFile r,Indexer ind,bool isStem)
        {
            this.path = path;
            avgDL = 0;
            k = 1.3;
            this.isStem = isStem;
            this.r = r;
            this.p = p;
            this.ind = ind;
            DocWeight = new Dictionary<string, double>();
            Docsmaxtf = new Dictionary<string, int>();
            docslength = new Dictionary<string, double>();
            string file;
            //check if we can do fast loading
            if (isStem)
                file = path + @"\rankerStem";
            else
                file=path + @"\ranker";
            if (File.Exists(file))
            {
                //if File exiest loading dictionaries in order to run faster
                using (FileStream fs = new FileStream(file, FileMode.Open))
                {
                    IFormatter bf = new BinaryFormatter();
                    DocWeight = (Dictionary<string,double>)bf.Deserialize(fs);//read object
                    Docsmaxtf = (Dictionary<string, int>)bf.Deserialize(fs);//read object
                    docslength = (Dictionary<string, double>)bf.Deserialize(fs);//read object
                    avgDL = (double)bf.Deserialize(fs);
                }
            }
            else
            {
                //calculate the dictionaries in order to run- long process
                calculateWeight();
                //after calculate save it for next time
                using (FileStream fs = new FileStream(file, FileMode.Create))
                {
                    IFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, DocWeight);//write object
                    bf.Serialize(fs, Docsmaxtf);//write object
                    bf.Serialize(fs, docslength);//write object
                    bf.Serialize(fs, avgDL);//write object
                }

            }
        }
        //Calculate cosin and BM fields
        public void calculateWeight()
        {
            for (int i = 0; i < r.files.Count; i++)
            {
                Debug.WriteLine(i);
                //Run on all files again
                Dictionary<string, string> d = r.ProccessDocs(r.files[i]);
                foreach (string docNo in d.Keys)
                {
                    //Parse DOC
                    Dictionary<string, termInfo> docdic = p.Parse(d[docNo]);
                    //length dictionary
                    docslength[docNo.Trim(' ')] = docdic.Count;
                    //Wight dictionary
                    DocWeight.Add(docNo.Trim(' '), 0);
                    //max tf dic
                    Docsmaxtf.Add(docNo.Trim(' '), p.maxtf);
                    foreach (string trm in docdic.Keys)
                    {
                        if (docdic.ContainsKey(trm)&& ind.dic.ContainsKey(trm))
                        {  //For Cosin
                            double W = (((double)docdic[trm].tf /(double) p.maxtf) * (double)ind.dic[trm].idf);
                            DocWeight[docNo.Trim(' ')] += (W * W);
                        }
                    }
                    DocWeight[docNo.Trim(' ')] = Math.Sqrt(DocWeight[docNo.Trim(' ')]);
                }
            }
            //for BM Formula
            foreach(string s in docslength.Keys)
            {
                avgDL += docslength[s];
            }
            avgDL = avgDL / (double)docslength.Count;

        }
        //rate Docs of specific query, calculate cosin and bm25 and return the final list- 50 or 70 docs aprox.
        public List<string> rateDocs(Dictionary<string, Dictionary<string, int>> qTerms,int numOfTerms,int amount)
        {
            List<KeyValuePair<string, double>> temp = new List<KeyValuePair<string, double>>();
            
            foreach (string docNum in qTerms.Keys)
            {
                //cos_sim
                double upCos = 0;
                double downCos = 0;
                //BM25
                double upBM25 = 0;
                double downBM25 = 0;
                double bm25 = 0;
                double bmbcal = k * (0.25 + 0.75 * docslength[docNum] / avgDL);

                //all terms in query
                foreach (string query_term in qTerms[docNum].Keys)
                {
                    //if term exsits in query but not in dictionary
                    if (ind.dic.ContainsKey(query_term))
                    {
                        //cosin
                        double Wij=((double)qTerms[docNum][query_term] / (double)Docsmaxtf[docNum]) * (double)ind.dic[query_term].idf;
                        upCos += Wij;

                        //BM25
                        upBM25 = (double)ind.dic[query_term].idf * (double)qTerms[docNum][query_term] * (k + 1);
                        downBM25 = (double)qTerms[docNum][query_term] + bmbcal ;
                        bm25 += upBM25 / downBM25;
                    }
                }
                downCos = Math.Sqrt((double)numOfTerms)* (double)DocWeight[docNum];
                double cosin = upCos / downCos;
                //Final Formula
                double formul = 0.4 * (cosin) + 0.6 * (bm25);
                temp.Add(new KeyValuePair<string, double>(docNum, formul));
            }
            //Sort the temp
            temp.Sort((KeyValuePair<string,double> d1, KeyValuePair<string, double> d2) => d2.Value.CompareTo(d1.Value));
            List<string> ans = new List<string>();
            for (int i=0;i<temp.Count && i<amount;i++)
            {
                //list of the docs after rank
                ans.Add(temp[i].Key);
            }
            return ans;
        }
    }
}
