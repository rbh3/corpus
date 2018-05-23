using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SearchEngine
{
    class Parser
    {
        string stpWrdsPth;
        Dictionary<string, termInfo> termPerDoc;
        Dictionary<string, string> month;
        Dictionary<string, string> prefix;
        HashSet<string> stop_words;
        Dictionary<string, string> stemmers;
        Stemmer stem;
        bool toStem;
        public string maxterm="";
        public int maxtf=0;
        public Parser(string p,bool isstem)
        {
            this.stem = new Stemmer();
            this.toStem = isstem;
            stemmers = new Dictionary<string, string>();
            stpWrdsPth = p;
            uploadStpWrds();
            month = new Dictionary<string, string>();
            prefix = new Dictionary<string, string>();
            // for prefix rule
            prefix.Add("dr", "doctor"); prefix.Add("mr", "mister"); prefix.Add("mrs", "misses");
            prefix.Add("dr.", "doctor"); prefix.Add("mr.", "mister"); prefix.Add("mrs.", "misses");
            //month to its num
            month.Add("JAN", "01"); month.Add("FEB", "02"); month.Add("MAR", "03"); month.Add("APR", "04");
            month.Add("MAY", "05"); month.Add("JUN", "06"); month.Add("JUL", "07"); month.Add("AUG", "08");
            month.Add("SEP", "09"); month.Add("OCT", "10"); month.Add("NOV", "11"); month.Add("DEC", "12");
            month.Add("Jan", "01"); month.Add("Feb", "01"); month.Add("Mar", "03"); month.Add("Apr", "04");
            month.Add("May", "05"); month.Add("Jun", "06"); month.Add("Jul", "07"); month.Add("Aug", "08");
            month.Add("Sep", "09"); month.Add("Oct", "10"); month.Add("Nov", "11"); month.Add("Dec", "12");
            month.Add("January", "01"); month.Add("February", "02"); month.Add("March", "03"); month.Add("April", "04");
            month.Add("June", "06"); month.Add("July", "07"); month.Add("August", "08"); month.Add("September", "09");
            month.Add("October", "10"); month.Add("November", "11"); month.Add("December", "12");
            month.Add("JANUARY", "01"); month.Add("FEBUARY", "02"); month.Add("MARCH", "03"); month.Add("APRIL", "04");
            month.Add("JUNE", "06"); month.Add("JULY", "07"); month.Add("AUGUST", "08"); month.Add("SEPTEMBER", "09");
            month.Add("OCTOBER", "10"); month.Add("NOVEMBER", "11"); month.Add("DECEMBER", "12");
        }
        //the main function of parsing
        public Dictionary<string, termInfo> Parse(string s)
        {
            maxterm = "";
            maxtf = 0;
            termPerDoc = new Dictionary<string, termInfo>();
            List<string> terms=mySplit(s.Replace(",", ""));//char char
            //string[] terms=s.Replace(",","").Split(new string[] {"`","'",";", "|"," "," <",">","\n","!","?","(",")",":","--","[","]", @"\","/","<P>","</P>","\""}, StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<terms.Count; i++)
            {
                terms[i] = terms[i].TrimEnd('.');
                if (terms[i].ToLower().Equals(terms[i]) && stop_words.Contains(terms[i]))
                    continue;//if you are a stopword continue
                //Date Case
                else if (month.ContainsKey(terms[i]))
                {
                    string mnt = terms[i];
                    i++;
                    int num = 0;
                    if (i < terms.Count && int.TryParse(terms[i].TrimEnd('.'), out num))
                    {
                        i++;
                        int num2 = 0;
                        if (i < terms.Count && int.TryParse(terms[i].TrimEnd('.'), out num2))
                        {
                            if (terms[i].TrimEnd('.').Length == 4)
                            {
                                AddDate(num, month[mnt], num2, i);//1-iv
                                continue;
                            }

                        }
                        else
                        {
                            i--;
                            if (terms[i].TrimEnd('.').Length == 4)
                            {
                                AddDate(0, month[mnt], num, i);// 3
                                continue;
                            }
                            else
                            {
                                if (num <= 31 && num > 0)
                                {
                                    AddDate(num, month[mnt], 0, i);//2-ii
                                    continue;
                                }
                                else
                                {
                                    //not a date
                                    Numbers("" + num, i);
                                    strToLowerCase(mnt, i);
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        i--;
                        strToLowerCase(mnt,i);//not a date
                        continue;
                    }
                }
                else
                {
                    int day = 0;
                    if (int.TryParse(terms[i].TrimEnd('.'), out day))
                    {
                        i++;
                        if (i < terms.Count && month.ContainsKey(terms[i]))
                        {
                            string mnt = month[terms[i]];
                            i++;
                            int num3 = 0;
                            if (i < terms.Count && int.TryParse(terms[i].TrimEnd('.'), out num3))
                            {
                                if (terms[i].Length == 4)
                                {
                                    AddDate(day, mnt, num3,i);//1-ii
                                    continue;
                                }
                                else if (terms[i].Length == 2)
                                {
                                    num3 = num3 + 1900;
                                    AddDate(day, mnt, num3,i);//1-iii
                                    continue;
                                }
                                else
                                {
                                    AddDate(day, mnt, 0,i);//2-i
                                    i--;
                                    continue;
                                }
                            }
                            else
                            {
                                AddDate(day, mnt, 0,i);//2-i
                                i--;
                                continue;
                            }
                        }
                        else
                        {
                            //percentage case
                            if (i < terms.Count && (terms[i].Equals("percent") || terms[i].Equals("percentage")))
                            {
                                Percent("" + day, i);
                                continue;
                            }
                            else
                            {
                                i--;
                                Numbers(terms[i], i);//Num case
                                continue;
                            }
                        }
                    }
                    else
                    if ((terms[i].Length == 4 || terms[i].Length == 3) && terms[i].Contains("th"))
                    {
                        string temp = terms[i];
                        string subtem = temp.Substring(temp.Length - 2);
                        int num3 = 0;
                        if (subtem.Equals("th") && int.TryParse((temp.Substring(0, temp.Length - 2)), out num3))//date with th
                        {
                            i++;
                            if (i < terms.Count && month.ContainsKey(terms[i]))
                            {
                                string mnt = terms[i];
                                i++;
                                int num4 = 0;
                                if (i < terms.Count && int.TryParse(terms[i].TrimEnd('.'), out num4))
                                {
                                    if (terms[i].Length == 4)
                                    {
                                        AddDate(num3, month[mnt], num4, i);//1-i
                                        continue;
                                    }
                                    else
                                    {
                                        i--;
                                        i--;
                                        strToLowerCase(terms[i], i);//not a date
                                        continue;
                                    }
                                }
                                else
                                { i--; i--; }
                            }
                            else
                            {
                                i--;
                                strToLowerCase(terms[i], i);//not a date
                                continue;
                            }
                        }
                        else//STOP WORD CASE
                        {
                            strToLowerCase(terms[i], i);
                            continue;
                        }
                    }
                    else
                    {
                        //UPPER CASE
                        string upper = null;
                        if (!terms[i].ToLower().Equals(terms[i]))
                        {
                            string word = terms[i].ToLower() ;
                            if (prefix.ContainsKey(word))// prefix case - our law
                            {
                                word = prefix[word];
                            }

                            upper = word + " ";
                            strToLowerCase(word,i);
                            i++;
                            while (i < terms.Count && !terms[i].ToLower().Equals(terms[i]))
                            {
                                string word2 = terms[i].ToLower();
                                if (prefix.ContainsKey(word2))
                                {
                                    word2 = prefix[word2];
                                }
                                upper += word2 + " ";
                                strToLowerCase(word2,i);
                                i++;
                            }
                            if (upper.Remove(upper.Length - 1).Contains(" "))
                                strToLowerCase(upper.Remove(upper.Length - 1),i);
                            i--;
                            continue;
                        }
                        else//percant case %
                        if (terms[i].Contains("%"))
                            Percent(terms[i],i);
                        else
                            strToLowerCase(terms[i],i); //any other case         
                    }
                }
            }
            return termPerDoc;
        }
        public void AddDate(int day,string month,int year,int fa)// add a date to dictionary
        {
            string str = "";
            if (day == 0)
            {
                str = month + "/" + year;
            }
            else
            if (year == 0)
            {
                str = day + "/" + month;
            }
            else
                str = day + "/" + month + "/" + year;

            if (stop_words.Contains(str))
                return;
            if (!termPerDoc.ContainsKey(str))
                termPerDoc.Add(str, new termInfo(1, fa));
            else
            {
                termPerDoc[str].tf++;
                if(termPerDoc[str].tf>maxtf)
                {
                    maxtf = termPerDoc[str].tf;
                    maxterm = str;
                }
            }
        }
        public void Numbers(string s,int fa)
        {
            double doub = 0;//round up a number
            if (s.Contains(".") && Double.TryParse(s, out doub))
            {
                doub = System.Math.Round(doub, 2);
                string str = "" + doub;
                if (!termPerDoc.ContainsKey(str))
                    termPerDoc.Add(str, new termInfo(1, fa));
                else
                {
                    termPerDoc[str].tf++;
                    if (termPerDoc[str].tf > maxtf)
                    {
                        maxtf = termPerDoc[str].tf;
                        maxterm = str;
                    }
                }

            }
            else
            {
                if (stop_words.Contains(s))
                    return;
                if (!termPerDoc.ContainsKey(s))
                    termPerDoc.Add(s, new termInfo(1, fa));
                else
                {
                    termPerDoc[s].tf++;
                    if (termPerDoc[s].tf > maxtf)
                    {
                        maxtf = termPerDoc[s].tf;
                        maxterm = s;
                    }
                }
            }
        }
        public void Percent(string num,int fa)//percentage state
        {
            string str = "";
            if (num.Contains("%"))
            {
                num=num.Replace("%", "");
                double doub = 0;
                if (Double.TryParse(num, out doub))
                {
                    doub = System.Math.Round(doub, 2);
                    str = "" + doub+" percent";
                }
            }
            else
            {
                str = num + " percent";
            }
            if (stop_words.Contains(str))
                return;
            if (!termPerDoc.ContainsKey(str))
                termPerDoc.Add(str, new termInfo(1, fa));
            else
            {
                termPerDoc[str].tf++;
                if (termPerDoc[str].tf > maxtf)
                {
                    maxtf = termPerDoc[str].tf;
                    maxterm = str;
                }
            }
        }
        public void strToLowerCase(string s,int fa)
        {
            string str = s.ToLower().Trim('-').TrimStart(new char[] { ' ' }).TrimEnd(' ').Replace("/","") ;
            if (str.Length != 0)
            { 
                str = str.Trim(' ');
                if (stop_words.Contains(str))
                    return;
                double doub = 0;
                if (s.Contains(".") && Double.TryParse(s, out doub))
                {
                    doub = System.Math.Round(doub, 2);
                    string stri = "" + doub;
                    if (!termPerDoc.ContainsKey(stri))
                        termPerDoc.Add(stri, new termInfo(1, fa));
                    else
                    {
                        termPerDoc[stri].tf++;
                        if (termPerDoc[stri].tf > maxtf)
                        {
                            maxtf = termPerDoc[stri].tf;
                            maxterm = stri;
                        }
                    }
                    return;
                }
                if (str.Contains("-"))
                {
                    splitslash(str,fa);
                    return;
                }
                if (prefix.ContainsKey(str))
                    str = prefix[str];
                if(toStem)
                {
                    if(stemmers.ContainsKey(str))
                    {
                        str = stemmers[str];
                    }
                    else
                    {
                        stemmers.Add(str, stem.stemTerm(str));
                        str = stemmers[str];
                    }
                }
                
                if (!termPerDoc.ContainsKey(str))
                    termPerDoc.Add(str, new termInfo(1, fa));
                else
                {
                    termPerDoc[str].tf++;
                    if (termPerDoc[str].tf > maxtf)
                    {
                        maxtf = termPerDoc[str].tf;
                        maxterm = str;
                    }
                }
            }
                
        }
        public void splitslash(string s,int fa)//splite to two words- our law
        {
            int index = s.IndexOf("-");
            string start = s.Substring(0, index).TrimStart(' ') ;
            string end = s.Substring(index + 1).TrimStart(' ');
            if (!stop_words.Contains(start))
            {
                if (!termPerDoc.ContainsKey(start))
                    termPerDoc.Add(start, new termInfo(1, fa));
                else
                {
                    termPerDoc[start].tf++;
                    if (termPerDoc[start].tf > maxtf)
                    {
                        maxtf = termPerDoc[start].tf;
                        maxterm = start;
                    }
                }
            }
            if (!stop_words.Contains(end))
            {
                if (!termPerDoc.ContainsKey(end))
                    termPerDoc.Add(end, new termInfo(1, fa));
                else
                {
                    termPerDoc[end].tf++;
                    if (termPerDoc[end].tf > maxtf)
                    {
                        maxtf = termPerDoc[end].tf;
                        maxterm = end;
                    }
                }
            }
            string temp= start+" " + end;
            if (!stop_words.Contains(temp))
            {
                if (!termPerDoc.ContainsKey(temp))
                    termPerDoc.Add(temp, new termInfo(1, fa));
                else
                {
                    termPerDoc[temp].tf++;
                    if (termPerDoc[temp].tf > maxtf)
                    {
                        maxtf = termPerDoc[temp].tf;
                        maxterm = temp;
                    }
                }
            }
        }
        private void uploadStpWrds()//uplode stop words
        {
            string content = File.ReadAllText(stpWrdsPth);
            string[] stpWrds = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            stop_words = new HashSet<string>(stpWrds);

        }
        public void clearSteam()
        {
            stemmers.Clear();
        }//clean stemmer dictionary
        private List<string> mySplit(string input)//char char base split
        {
            List<string> result = new List<string>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!(Char.IsDigit(c) || Char.IsLetter(c) || (sb.Length > 0 && c == '.') || (sb.Length > 0 && c == '$') || (sb.Length > 0 && c == '-') || (sb.Length > 0 && c == ',') || c == '<' || (c == '/' && sb.Length > 0) || c == '%'))
                {
                    if (sb.Length > 0 && sb[0] == '<')
                        sb.Length = 0;
                    if (sb.Length > 1)
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    { // case of 1 char - only numbers should be insert
                        if (sb.Length == 1)
                        {
                            if (Char.IsDigit(sb[0]))
                                result.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                }
                else // append the char into the whole word
                    sb.Append(c);
            }
            if (sb.Length > 0)
            {
                result.Add(sb.ToString());
            }
            sb.Clear();
            return result;
        }
    }
}





