using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SearchEngine
{
    class Indexer
    {
        string path;
        static int fileIndex = 0;
        Dictionary<string, List<PostingInfo>> temp;
        public Dictionary<string, DicRecord> dic;
        public Dictionary<string, List<PostingInfo>> cache;
        int docCounter;
        public int TotalDoc = 0;
        public long PostSize = 0;
        public long cacheSize = 0;
        StringBuilder sb = new StringBuilder();
        public StreamWriter swrite,swriteCache;
        bool isStem;
        string After;

        public Indexer(string path,bool stem)
        {
            docCounter = 0;
            this.path = path;
            this.isStem = stem;
            if (isStem)
                After = path + "\\AfterPostStem";
            else
                After = path + "\\AfterPost";
            dic = new Dictionary<string, DicRecord>();
            temp = new Dictionary<string, List<PostingInfo>>();
            if(!Directory.Exists(After))
                Directory.CreateDirectory(After);
            try
            {
                if (File.Exists(After + "\\DocsInfo.txt"))
                    File.Delete(After + "\\DocsInfo.txt");
               
            }
            catch(Exception e)
            { }
            string s;
            if (isStem)
                s = "\\CacheDic\\cacheStem.txt";
            else
                s = "\\CacheDic\\cache.txt";
            if (!Directory.Exists(path + "\\CacheDic"))
                Directory.CreateDirectory(path + "\\CacheDic");
            if (File.Exists(path + s))
                File.Delete(path + s);
            
            swriteCache = new StreamWriter(path + s, true);
            swriteCache.WriteLine("************************CACHE************************");
            cache = new Dictionary<string, List<PostingInfo>>();
        }
        //make all the necessery arragments for the temp posting file
        public void tempPosting(Dictionary<string, termInfo> d, string docNo, string maxterm, int maxtf)
        {
            swrite = new StreamWriter(After + "\\DocsInfo.txt", true);
            sb.AppendLine(docNo + ":" + d.Count() + ";" + maxterm + ";" + maxtf + ";");
            foreach (string s in d.Keys)
            {
                //update dictionary
                if (dic.ContainsKey(s))
                {
                    dic[s].TotalFreq+=d[s].tf;
                    dic[s].df++;
                }
                else
                {
                    dic.Add(s, new DicRecord());
                    dic[s].TotalFreq+=d[s].tf;
                    dic[s].df++;
                }
                if (temp.ContainsKey(s))
                    temp[s].Add(new PostingInfo(d[s], docNo));
                else
                {
                    List<PostingInfo> l = new List<PostingInfo>();
                    l.Add(new PostingInfo(d[s], docNo));
                    temp.Add(s, l);
                }
            }
            //write the temp file to the disk
            if (docCounter > 12000)
            {
                tempPost();
                docCounter = 0;
                temp.Clear();
            }
            docCounter++;
            TotalDoc++;
        }
        //writing the temp to the disk
        public void tempPost()
        {
            swrite.Write(sb.ToString());
            swrite.Flush();
            sb.Clear();
            Directory.CreateDirectory(path + "\\tempPost");
            StreamWriter sw = new StreamWriter(path + "\\tempPost\\temppost" + fileIndex + ".txt");
            fileIndex++;
            List<string> sortedterms = new List<string>(temp.Keys);
            sortedterms.Sort();
            for (int i = 0; i < sortedterms.Count; i++)
            {
                //dic[sortedterms[i]].df += temp[sortedterms[i]].Count;
                sw.Write(sortedterms[i] + "[" + temp[sortedterms[i]].Count + "]:");
                foreach (PostingInfo post in temp[sortedterms[i]])
                {
                    sw.Write(post.DocNo + "," + post.term.tf + "," + post.term.hdr + ";");
                }
                sw.WriteLine();
            }
            sw.Close();
        }
        //merge sort wrapper
        public void mergefile()
        {
            swrite.Close();
            string[] fileEntries = Directory.GetFiles(path + "\\tempPost");
            Directory.CreateDirectory(After);
            while (fileEntries.Length > 2)
            {
                for (int i = 0; i < fileEntries.Length - 1; i += 2)
                {
                    mergeSort(fileEntries[i], fileEntries[i + 1],false);
                }
                fileEntries = Directory.GetFiles(path + "\\tempPost");
            }
            mergeSort(fileEntries[0], fileEntries[1], true);
            fileIndex--;
            //calculate IDF
            foreach(string s in dic.Keys)
            {
                dic[s].idf= Math.Log((TotalDoc/ dic[s].df), 2);
            }
            splitDocs2();//splite the huge docs into small ones
        }
        bool IsEnglishLetter(char c)
        {
            return (c >= 'a' && c <= 'z');
        }
        //merge sort line by line
        private void mergeSort(string s1, string s2,bool isFinal)
        {
            StreamReader f1 = new StreamReader(s1);
            StreamReader f2 = new StreamReader(s2);
            String line1 = f1.ReadLine();
            String line2 = f2.ReadLine();
            StreamWriter sw = new StreamWriter(path + "\\tempPost\\temppost" + fileIndex + ".txt");
            fileIndex++;

            while (line1 != null && line2 != null)//both files have lines
            {
                if (line2 != null && line2.Length == 0)
                    break;
                if (line1 != null && line1.Length == 0)
                    break;

                int comp = line1.Substring(0, line1.IndexOf('[')).CompareTo(line2.Substring(0, line2.IndexOf('[')));
                switch (comp)
                {
                    case int n when (n > 0):
                        {
                            if (!isFinal)
                            {
                                sw.WriteLine(line2);
                                line2 = f2.ReadLine();
                            }
                            else
                            {
                                int strt = line2.IndexOf('[');
                                string trm = line2.Substring(0, strt);
                                if (dic[trm].TotalFreq <= 3)
                                {
                                    dic.Remove(trm);
                                    line2 = f2.ReadLine();
                                    break;
                                }
                                else
                                {
                                    sw.WriteLine(line2);
                                    line2 = f2.ReadLine();
                                }
                            }
                            break;
                        }
                    case int n when (n < 0):      
                            if (!isFinal)
                            {
                                sw.WriteLine(line1);
                                line1 = f1.ReadLine();
                            }
                            else
                            {
                                int strt = line1.IndexOf('[');
                                string trm = line1.Substring(0, strt);
                                if (dic[trm].TotalFreq <= 3)
                                {
                                    dic.Remove(trm);
                                    line1 = f1.ReadLine();
                                    break;
                                }
                                else
                                {
                                    sw.WriteLine(line1);
                                    line1 = f1.ReadLine();
                                }
                            }
                            break;

                    case int n when (n == 0): //same term
                        {
                            int ls1 = line1.IndexOf('[') + 1;
                            int lin1 = line1.IndexOf(']');
                            int ls2 = line2.IndexOf('[') + 1;
                            int lin2 = line2.IndexOf(']');
                            string num11 = line1.Substring(ls1, (lin1 - ls1));
                            int num1 = Int32.Parse(num11);
                            int num2 = Int32.Parse(line2.Substring(ls2, (lin2 - ls2)));
                            string trm = line1.Substring(0, line1.IndexOf("["));
                            if(isFinal && dic[trm].TotalFreq<=3)
                            {
                                dic.Remove(trm);
                                line1 = f1.ReadLine();
                                line2 = f2.ReadLine();
                                break;
                            }
                            StringBuilder sb = new StringBuilder(line1.Substring(0, line1.IndexOf("[") + 1));
                            sb.Append((num1 + num2) + "]" + line1.Substring(line1.IndexOf(":")) + line2.Substring(line2.IndexOf(':') + 1));
                            sw.WriteLine(sb.ToString());
                            line1 = f1.ReadLine();
                            line2 = f2.ReadLine();
                            break;
                        }
                }
            }
            while (line2 != null)//only file 2 have lines
            {
                if (line2 != null && line2.Length == 0)
                    break;
                if (!isFinal)
                {
                    sw.WriteLine(line2);
                    line2 = f2.ReadLine();
                    
                }
                else
                {
                    int strt = line2.IndexOf('[');
                    string trm = line2.Substring(0, strt);
                    if (dic[trm].TotalFreq <= 3)
                    {
                        dic.Remove(trm);
                        line2 = f2.ReadLine();
                    }
                    else
                    {
                        sw.WriteLine(line2);
                        line2 = f2.ReadLine();
                    }
                }
            }
            while (line1 != null)//only file 1 have lines
            {
                if (line1 != null && line1.Length == 0)
                    break;
                if (!isFinal)
                {
                    sw.WriteLine(line1);
                    line1 = f1.ReadLine();

                }
                else
                {
                    int strt = line1.IndexOf('[');
                    string trm = line1.Substring(0, strt);
                    if (dic[trm].TotalFreq <= 3)
                    {
                        dic.Remove(trm);
                        line1 = f1.ReadLine();
                    }
                    else
                    {
                        sw.WriteLine(line1);
                        line1 = f1.ReadLine();
                    }
                }
            }
            f1.Close();
            f2.Close();
            File.Delete(s1);
            File.Delete(s2);
            sw.Flush();
            sw.Close();
        }
        private void splitDocs()//splite into 6 after final post files-OLDD
        {
            var list = dic.ToList();
            list.Sort((pair1, pair2) => pair2.Value.TotalFreq.CompareTo(pair1.Value.TotalFreq));
            for (int i = 0; i < list.Count && i<10000; i++)
            {
                dic[list[i].Key].isCache = true;
            }
            StreamReader f1 = new StreamReader(path + "\\tempPost\\tempPost" + fileIndex + ".txt");

            PostSize =new FileInfo(path + "\\tempPost\\tempPost" + fileIndex + ".txt").Length;
            String line1 = f1.ReadLine();
            StreamWriter sw0 = new StreamWriter(After+"\\AfterPostSigns.txt");
            StreamWriter sw1 = new StreamWriter(After + "\\AfterPostAtoE.txt");
            StreamWriter sw2 = new StreamWriter(After + "\\AfterPostFtoJ.txt");
            StreamWriter sw3 = new StreamWriter(After + "\\AfterPostKtoO.txt");
            StreamWriter sw4 = new StreamWriter(After + "\\AfterPostPtoT.txt");
            StreamWriter sw5 = new StreamWriter(After + "\\AfterPostUtoZ.txt");
            int count0 = 1, count1 = 1, count2 = 1, count3 = 1, count4 = 1, count5 = 1;
            while (line1 != null)
            {
                while ((line1 != null && line1.Length < 1) || line1 != null && (line1.IndexOf("[") - 1) < 0)
                {
                    line1 = f1.ReadLine();
                }
                if (line1 == null)
                    break;
                if (line1[0] >= 'a' && line1[0] <= 'e')
                {
                    sw1.WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count0;
                    count0++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                    continue;
                }
                if (line1[0] >= 'f' && line1[0] <= 'j')
                {
                    sw2.WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count1;
                    count1++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                    continue;
                }
                if (line1[0] >= 'k' && line1[0] <= 'o')
                {
                    sw3.WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count2;
                    count2++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                    continue;
                }
                if (line1[0] >= 'p' && line1[0] <= 't')
                {
                    sw4.WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count3;
                    count3++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                    continue;
                }
                if (line1[0] >= 'u' && line1[0] <= 'z')
                {
                    sw5.WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count4;
                    count4++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                    continue;
                }
                if (!IsEnglishLetter(line1[0]))
                {
                    sw0.WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string sam = line1.Substring(0, ind);
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count5;
                    count5++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                    continue;
                }
            }
            sw0.Close(); sw1.Close(); sw2.Close(); sw3.Close(); sw4.Close(); sw5.Close();
            f1.Close();
            swriteCache.Flush();
            swriteCache.Close();
            File.Delete(path + "\\tempPost\\tempPost" + fileIndex + ".txt");
            Directory.Delete(path + "\\tempPost");
            string s;
            if (isStem)
                s = "\\CacheDic\\cacheStem.txt";
            else
                s = "\\CacheDic\\cache.txt";
            cacheSize = new FileInfo(path + s).Length;

        }
                
        //get the line from disk
        public string postingList(char c, int row)
        {
            StreamReader[] streams = new StreamReader[27];
            for (int i = 0; i < streams.Length - 1; i++)
                streams[i] = new StreamReader(After + "\\AfterPost" + ((char)i + 65) + ".txt");
            streams[26] = new StreamReader(After + "\\AfterPostSigns.txt");
            string line = "";
            if (Char.IsLetter(c))
            {
                for (int i = 0; i < row; i++)
                {
                    line = streams[(int)c - 97].ReadLine();
                }
            }
            else
            {
                for (int i = 0; i < row; i++)
                {
                    line = streams[26].ReadLine();
                }
            }
            return line;
            /*The OLD CODE!!!*/
          /*  if (c >= 'a' && c <= 'e')
            {
                StreamReader sr1 = new StreamReader(After + "\\AfterPostAtoE.txt");
                for (int i = 0; i < row; i++)
                {
                    line = sr1.ReadLine();
                }
                sr1.Close();
            }
            if (c >= 'f' && c <= 'j')
            {
                StreamReader sr1 = new StreamReader(After + "\\AfterPostFtoJ.txt");
                for (int i = 0; i < row; i++)
                {
                    line = sr1.ReadLine();
                }
                sr1.Close();
                return line;
            }
            if (c >= 'k' && c <= 'o')
            {
                StreamReader sr1 = new StreamReader(After + "\\AfterPostKtoO.txt");
                for (int i = 0; i < row; i++)
                {
                    line = sr1.ReadLine();
                }
                sr1.Close();
                return line;
            }
            if (c >= 'p' && c <= 't')
            {
                StreamReader sr1 = new StreamReader(After + "\\AfterPostPtoT.txt");
                for (int i = 0; i < row; i++)
                {
                    line = sr1.ReadLine();
                }
                sr1.Close();
                return line;
            }
            if (c >= 'u' && c <= 'z')
            {
                StreamReader sr1 = new StreamReader(After + "\\AfterPostUtoZ.txt");
                for (int i = 0; i < row; i++)
                {
                    line = sr1.ReadLine();
                }
                sr1.Close();
                return line;
            }
            if (!IsEnglishLetter(c))
            {
                StreamReader sr1 = new StreamReader(After + "\\AfterPostSigns.txt");
                for (int i = 0; i < row; i++)
                {
                    line = sr1.ReadLine();
                }
                sr1.Close();
                return line;

            }
            return line;*/
        }
        
        //add the files to the cache
        public void AddToCatch(string line)
        {
            string term = line.Substring(0, line.IndexOf("["));
            line = line.Substring(line.IndexOf(":") + 1);
            string[] sp = line.Split(new char[] { ',', ';' });

            //make a list of posting info that gonna be the value of the cache
            List<PostingInfo> l = new List<PostingInfo>();
            cache.Add(term, new List<PostingInfo>());
            swriteCache.Write(term+": ");
            for (int j = 0; j < sp.Length - 3; j += 3)
            {

                bool b = false;
                if (sp[j + 2].Equals("True"))
                    b = true;
                l.Add(new PostingInfo(new termInfo(Int32.Parse(sp[j + 1]), b), sp[j]));
            }
            l.Sort((PostingInfo p1, PostingInfo p2) => p2.term.tf.CompareTo(p1.term.tf));
            int count = 0;
            for (int x = 0; x < l.Count; x++)
            {//take only 25 files to the cache
                if (count < 25)
                {
                    cache[term].Add(l[x]);
                    count++;
                    swriteCache.Write(l[x].ToString());
                }
                else
                {
                    swriteCache.WriteLine();
                    break;
                }
            }

        }
        //write the dictionary to txt for viewing
        public void writeTextDic()
        {
            string s;
            if (isStem)
                s = "\\CacheDic\\dicStem.txt";
            else
                s = "\\CacheDic\\dic.txt";
            if (File.Exists(path + s))
                File.Delete(path + s);
            dic.Remove("");
            StreamWriter swriteDic = new StreamWriter(path + s);
            swriteDic.WriteLine("******************** Dictionary ********************");
            List<string> sortedterms = new List<string>(dic.Keys);
            sortedterms.Sort();
            for (int i = 0; i < sortedterms.Count; i++)
            {
                swriteDic.WriteLine(sortedterms[i] + " : " + dic[sortedterms[i]].TotalFreq);
            }
            swriteDic.Flush();
            swriteDic.Close();
        }
        //write the cache to txt for viewing
        public void writeTextChache()
        {
            List<string> sortedterms = new List<string>(cache.Keys);
            sortedterms.Sort();
            for (int i = 0; i < sortedterms.Count; i++)
            {
                swriteCache.Write(sortedterms[i] + " : ");
                foreach (PostingInfo post in cache[sortedterms[i]])
                {
                    swriteCache.Write(post.DocNo + "," + post.term.tf + "," + post.term.hdr + ";");
                }
                swriteCache.WriteLine();
            }
            swriteCache.Flush();
            swriteCache.Close();
        }
        /*NEW*/
        private void splitDocs2()//splite into Alphabetical after final post files-NEW
        {
            var list = dic.ToList();
            list.Sort((pair1, pair2) => pair2.Value.TotalFreq.CompareTo(pair1.Value.TotalFreq));
            for (int i = 0; i < list.Count && i < 10000; i++)
            {
                dic[list[i].Key].isCache = true;
            }
            StreamReader f1 = new StreamReader(path + "\\tempPost\\tempPost" + fileIndex + ".txt");

            PostSize = new FileInfo(path + "\\tempPost\\tempPost" + fileIndex + ".txt").Length;
            String line1 = f1.ReadLine();
            //open 27 streams, A-Z and Signs
            StreamWriter[] streams = new StreamWriter[27];
            for (int i = 0; i < streams.Length - 1; i++)//naming the files
                streams[i] = new StreamWriter(After + "\\AfterPost" + ((char)i + 65) + ".txt");
            streams[26] = new StreamWriter(After + "\\AfterPostSigns.txt");
            //counter for each file
            int[] count = new int[27];
            for (int i = 0; i < count.Length; i++)
                count[i] = 1;
            //while not EOF
            while (line1 != null)
            {
                while ((line1 != null && line1.Length < 1) || line1 != null && (line1.IndexOf("[") - 1) < 0)
                {
                    line1 = f1.ReadLine();
                }
                if (line1 == null)
                    break;
                if (char.IsLetter(line1[0]))//if letter- case A-Z
                {
                    //open the currect stream- using ASCII
                    streams[((int)line1[0]) - 97].WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count[((int)line1[0]) - 97];
                    count[((int)line1[0]) - 97]++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                }
                else// Signs and Number case
                {
                    streams[26].WriteLine(line1);
                    int ind = line1.IndexOf("[");
                    string term = line1.Substring(0, ind);
                    dic[term].Pointer = count[26];
                    count[26]++;
                    if (dic[term].isCache)
                        AddToCatch(line1);
                    line1 = f1.ReadLine();
                }
            
            }
            //close all streams
            for (int i = 0; i < streams.Length; i++)
                streams[i].Close();
            f1.Close();
            swriteCache.Flush();
            swriteCache.Close();
            //Delete the last Temp Posting File
            File.Delete(path + "\\tempPost\\tempPost" + fileIndex + ".txt");
            Directory.Delete(path + "\\tempPost");
            //check the cache Size
            string s;
            if (isStem)
                s = "\\CacheDic\\cacheStem.txt";
            else
                s = "\\CacheDic\\cache.txt";
            cacheSize = new FileInfo(path + s).Length;

        }
    }
}
