using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace SearchEngine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isStem = false, isSummarize = false, isWiki = false;
        private string pathopen, pathclose, queryFile;
        private ReadFile r;
        private Parser p;
        Dictionary<string, string> d;
        private Indexer ind;
        Searcher searcher;
        Ranker rank;
        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine("DONE");
        }
        private void StartEngine()
        {
            //Delete Exists Directory
            if (Directory.Exists(pathclose + @"\AfterPost"))
                Directory.Delete(pathclose + @"\AfterPost", true);
            if (Directory.Exists(pathclose + @"\tempPost"))
                Directory.Delete(pathclose + @"\tempPost", true);
          /*  if (Directory.Exists(pathclose + @"\CacheDic"))
                Directory.Delete(pathclose + @"\CacheDic", true);*/
            //initlizing reader,parser,indexer
            r = new ReadFile(pathopen);
            p = new Parser(pathopen + @"\stop_words.txt", isStem);
            ind = new Indexer(pathclose, isStem);

            //For program timing
            DateTime start = DateTime.Now;


            for (int i = 0; i < r.files.Count; i++)
            {
                Debug.WriteLine(i);
                //return dictionary<DocNO, TEXT>
                d = r.ProccessDocs(r.files[i]);

                foreach (string s in d.Keys)
                {
                    //Parse DOC
                    Dictionary<string, termInfo> docdic = p.Parse(d[s]);
                    //make temp Post file
                    ind.tempPosting(docdic, s.Trim(' '), p.maxterm, p.maxtf);
                }
            }
            StreamWriter sw = new StreamWriter(pathopen + @"\docsMap.txt");
            sw.WriteLine(r.sb.ToString());
            sw.Close();
            ind.tempPost(); //Write the last dictionary 
            p.clearSteam(); // cleans the stemmers dictionary
            ind.mergefile(); // merge and split
            ind.writeTextDic();// WriteDic For show
            TimeSpan ts3 = DateTime.Now - start;
            //The requested popup of this run
            System.Windows.Forms.MessageBox.Show("Number Of Docs Indexed : " + ind.TotalDoc + "\nTime Of Running : " + ts3.TotalSeconds + "\nIndex Size[bytes] : " + ind.PostSize + "\nCache Size[bytes] : " + ind.cacheSize, "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CorpusFolderBrowse_click(object sender, RoutedEventArgs e)
        {
            //Folder Choose
            var dlg = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            Debug.WriteLine(dlg.SelectedPath);
            //change the source path
            pathopen = dlg.SelectedPath;
            //change the textbox to the path
            Corpustextbox.Text = pathopen;
        }

        private void PostingSavingLocation_Click(object sender, RoutedEventArgs e)
        {
            //Folder Choose
            var dlg = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            Debug.WriteLine(dlg.SelectedPath);
            //change the dest path
            pathclose = dlg.SelectedPath;
            //change the textbox to the path
            Postingtextbox.Text = pathclose;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //check if stem
            isStem = stemmcheckBox.IsChecked.Value;
            Debug.WriteLine(isStem);
            if (ind != null)//if Loaded Stem File wont change the setting till reset
            {
                System.Windows.Forms.MessageBox.Show("U must reset and load from scratch in order to \n change this setting", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isSumCheck.IsChecked = false;
                isStem = false;
            }
        }

        private void Corpustextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //change the path if the textbox is written
            pathopen = Corpustextbox.Text;
            Debug.WriteLine(pathopen);
        }

        private void Postingtextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //change the path if the textbox is written
            pathclose = Postingtextbox.Text;
            Debug.WriteLine(pathclose);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            //restart gui
            Corpustextbox.IsEnabled = true;
            CorpusFolderBrowse.IsEnabled = true;
            pathopen = "";
            Corpustextbox.Clear();
            if (ind != null)
                ind.swrite.Close();
            if (Directory.Exists(pathclose + "\\AfterPost"))
                Directory.Delete(pathclose + "\\AfterPost", true);
            ind = null;
            r = null;
            p = null;

            //DELETE DICTIONERY
            string s;
            if (isStem)
                s = "\\CacheDic\\dicStem.txt";
            else
                s = "\\CacheDic\\dic.txt";
            if (File.Exists(pathclose + s))
                File.Delete(pathclose + s);
            showDic.IsEnabled = false;
            showcatch.IsEnabled = false;

            //Garbage Collector
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //notification
            System.Windows.Forms.MessageBox.Show("The Memory is clean!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        }

        private void showcatch_Click(object sender, RoutedEventArgs e)
        {
            //open notepad with the catch file
            string s;
            if (isStem)//choose stemming
                s = "\\CacheDic\\cacheStem.txt";
            else
                s = "\\CacheDic\\cache.txt";
            Process.Start(@"notepad++.exe", (pathclose + s));
        }

        private void showDic_Click(object sender, RoutedEventArgs e)
        {
            //open notepad with the dictionary file
            string s;
            if (isStem)//choose stemming
                s = "\\CacheDic\\dicStem.txt";
            else
                s = "\\CacheDic\\dic.txt";
            Process.Start(@"notepad++.exe", (pathclose + s));
        }

        private void SaveDic_Click(object sender, RoutedEventArgs e)
        {
            if (ind == null || ind.dic.Count <= 0)
            {
                //case if there is no dictionary
                System.Windows.Forms.MessageBox.Show("There is no dictionary in the propgram's memory, Load one or start proccess one", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            /*
            //save new file
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Dictionary File|*.dicx";
            dlg.Title = "Save a Dictionary File";
            dlg.ShowDialog();
            */
            //Folder Dialog
            var dlg = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            // If the file name is not an empty string open it for saving.  
            if (dlg.SelectedPath != "")
            {
                //save dic- specific name
                string dic;
                if (isStem)
                {
                    dic = pathclose + @"\CacheDic\dicStem.dicx";
                }
                else
                {
                    dic = pathclose + @"\CacheDic\dic.dicx";
                }
                using (FileStream fs = new FileStream(dic, FileMode.Create))
                {
                    IFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, ind.dic); //write object
                }
                //notify when finished
                System.Windows.Forms.MessageBox.Show("Dictionary Saved!", "SAVED!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                return;
        }

        private void LoadDic_Click(object sender, RoutedEventArgs e)
        {
            //open new file
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Dictionary File|*.dicx";
            dlg.Title = "Open a Dictionary File";
            dlg.ShowDialog();
            if (dlg.FileName != "")// If the file name is not an empty string ,open it.  
            {
                if (ind == null)//if its not after a full run
                {
                    pathclose = @".\";
                    ind = new Indexer(pathclose, isStem);
                }
                using (FileStream fs = new FileStream(dlg.FileName, FileMode.Open))
                {
                    IFormatter bf = new BinaryFormatter();
                    ind.dic = (Dictionary<string, DicRecord>)bf.Deserialize(fs);//read object
                }
                ind.writeTextDic();
                showDic.IsEnabled = true;
                //notify when finished
                System.Windows.Forms.MessageBox.Show("Dictionary Loaded!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                return;

        }

        private void LoadCache_Click(object sender, RoutedEventArgs e)
        {
            //open new file
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Cache File|*.chex";
            dlg.Title = "Open a cache File";
            dlg.ShowDialog();
            if (dlg.FileName != "")// If the file name is not an empty string, open it.
            {
                if (ind == null)//if its not after a full run
                {
                    pathclose = @".\";
                    ind = new Indexer(pathclose, isStem);
                }
                using (FileStream fs = new FileStream(dlg.FileName, FileMode.Open))
                {
                    IFormatter bf = new BinaryFormatter();
                    ind.cache = (Dictionary<string, List<PostingInfo>>)bf.Deserialize(fs);//read object
                }
                ind.writeTextChache();
                showcatch.IsEnabled = true;
                //notify when finished
                System.Windows.Forms.MessageBox.Show("Cache Loaded!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
        }

        private void SaveCache_Click(object sender, RoutedEventArgs e)
        {
            //case if there is no cache
            if (ind == null || ind.dic.Count <= 0)
            {
                System.Windows.Forms.MessageBox.Show("There is no cache in the propgram's memory, Load one or start proccess one", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            /*
            //open save dialog
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Cache File|*.chex";
            dlg.Title = "Save a Cache File";
            dlg.ShowDialog();
            */
            //Folder Chooser
            var dlg = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            // If the file name is not an empty string open it for saving.  
            if (dlg.SelectedPath != "")
            {//save cache fixed name
                string cache;
                if (isStem)
                {
                    cache = pathclose + @"\CacheDic\cacheStem.chex";
                }
                else
                {
                    cache = pathclose + @"\CacheDic\cache.chex";
                }
                using (FileStream fs = new FileStream(cache, FileMode.Create))
                {
                    IFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, ind.cache);//write object
                }
                //notify when finish
                System.Windows.Forms.MessageBox.Show("Cache Saved!", "SAVED!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                return;
        }

        private void runQuery_Click(object sender, RoutedEventArgs e)
        {
            //Run a Query
            DateTime start = DateTime.Now;//for timing
            string query = QueryTextBox.Text;//take the text from the box
            List<string> ans;
            //case of summery a doc, Summary should be selected
            if (queryFile == null && isSummarize == true && isWiki == false)
            {
                //return the Five sentences and thier score
                string res = searcher.FiveSents(query);
                if (res != null)
                {
                    //init new window
                    Results frm2 = new Results(false, res);
                    frm2.ShowDialog();
                }
                else
                {
                    //case of Doc not found
                    System.Windows.Forms.MessageBox.Show("Document Not Found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            //case of simple query
            else if (queryFile == null && isSummarize == false && isWiki == false)
            {
                //return max 50 relevant docs
                List<string> res = searcher.returnDocs(query,50);
                ans = new List<string>();
                StringBuilder sb = new StringBuilder();
                foreach (string s in res)
                {
                    //for saving in trecEval format
                    ans.Add("111 0 " + s);
                    sb.AppendLine(s);
                }
                TimeSpan ts3 = DateTime.Now - start;// end timing
                string summery = "Number Of Docs relevant: " + ans.Count + " , Running Time: " + ts3 + "\r\n";//For abigile
                //init new windows
                Results frm2 = new Results(true,summery+  sb.ToString(), ans);
                frm2.ShowDialog();
            }
            // case of file query
            else if (queryFile != null && isSummarize == false && isWiki==false)
            {
                ans = new List<string>();
                StringBuilder sb = new StringBuilder();
                string content = File.ReadAllText(queryFile);
                //splite all the queries in the doc
                string[] values = content.Split(new string[] { "<top>"}, StringSplitOptions.RemoveEmptyEntries);
                for(int i=0;i<values.Length;i++)
                {
                    //add the description of the query to the query
                    int num = values[i].IndexOf(":");
                    string qnum = values[i].Substring(num + 2,3);
                    int title = values[i].IndexOf("<title>");
                    int desc = values[i].IndexOf("<desc>");
                    int description = values[i].IndexOf("Description:");
                    int nerr = values[i].IndexOf("<narr>");
                    string exp = values[i].Substring(description + 11, nerr - (description + 11));
                    string q = values[i].Substring(title + 8, desc - (title+8));
                    q = q.TrimEnd(' ', '\r', '\n');
                    q += exp;
                    //send the new query to the searcher and return docs after rank
                    List<string> temp = searcher.returnDocs(q,50);
                    //printing the num of the query
                    sb.AppendLine("*****" + qnum + "*****");
                    foreach (string s in temp)
                    {
                        ans.Add(qnum+" 0 " + s);
                        sb.AppendLine(s);
                    }
                }
                TimeSpan ts3 = DateTime.Now - start;//stop timing
                string summery = "Number Of Docs relevant: " + ans.Count + " , Running Time: " + ts3 + "\r\n";
                Results frm2 = new Results(true, summery+sb.ToString(), ans);//init new window
                frm2.ShowDialog();
            }
            //case of wikipedia expend
            else if (queryFile == null &&  isSummarize== false && isWiki == true)
            {
                //for API request, must start with upper letter
                char[] c = query.ToCharArray();
                c[0] = char.ToUpper(c[0]);
                string myquery = new string(c);
                if(myquery.Contains(" "))
                {
                    System.Windows.Forms.MessageBox.Show("Wiki expand is working only on one word!", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                //expend the query and then runk the relevant docs, return max 70
                List<string> res = searcher.returnDocs(searcher.WikiExp(myquery), 70);
                ans = new List<string>();
                StringBuilder sb = new StringBuilder();
                foreach (string s in res)
                {
                    //for printing the save file in TrecEval format
                    ans.Add("111 0 " + s);
                    sb.AppendLine(s);
                }
                TimeSpan ts3 = DateTime.Now - start;//end timing
                string summery = "Number Of Docs relevant: " + ans.Count + " , Running Time: " + ts3 + "\r\n";
                //init new window
                Results frm2 = new Results(true, summery + sb.ToString(), ans);
                frm2.ShowDialog();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Invalid Search", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            Reset_mini();
        }
        //reset the load and gui of part 2
        private void Reset_Part2_Click(object sender, RoutedEventArgs e)
        {
            //reset GUI
            r = null;
            p = null;
            ind = null;
            showcatch.IsEnabled = false;
            showDic.IsEnabled = false;
            runQuery.IsEnabled = false;
            QueryTextBox.Clear();
            isWikiExp.IsChecked = false;
            isSumCheck.IsChecked = false;
            stemmcheckBox.IsChecked = false;
            //Delete Last File
            try
            {
                if (File.Exists(Results.LastFile))
                    File.Delete(Results.LastFile);
            }
            catch(IOException)
            {
                Debug.WriteLine("CANT DELETE!!!!!");
            }
            //Garbage Collector
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //notification
            System.Windows.Forms.MessageBox.Show("The Memory is clean!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }


        private void Reset_mini()
        {
            //reset GUI
            QueryTextBox.Clear();
            QueryTextBox.IsReadOnly = false;
            isWikiExp.IsChecked = false;
            isSumCheck.IsChecked = false;
            queryFile = null;
            
        }
        //Load all files of part 2-corpus, stop words, cache, dictionary ,posting and rank
        private void Load2_click(object sender, RoutedEventArgs e)
        {
            //Folder Chooser
            var dlg = new FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
            //change the source path
            if (dlg.SelectedPath != "")
            {
                pathopen = dlg.SelectedPath;
                pathclose = dlg.SelectedPath;
                //init all the first part objects
                ind = new Indexer(pathclose, isStem);
                p = new Parser(pathopen + @"\stop_words.txt", isStem);
                r = new ReadFile(pathopen + @"\corpus\");
                string dic;
                string cache;
                if (isStem)//check if stem
                {
                    dic = pathclose + @"\CacheDic\dicStem.dicx";
                    cache = pathclose + @"\CacheDic\cacheStem.chex";
                }
                else
                {
                    dic = pathclose + @"\CacheDic\dic.dicx";
                    cache = pathclose + @"\CacheDic\cache.chex";
                }
                try
                {
                    //load dic
                    using (FileStream fs = new FileStream(dic, FileMode.Open))
                    {
                        IFormatter bf = new BinaryFormatter();
                        ind.dic = (Dictionary<string, DicRecord>)bf.Deserialize(fs);//read object
                    }

                    //load cache
                    using (FileStream fs = new FileStream(cache, FileMode.Open))
                    {
                        IFormatter bf = new BinaryFormatter();
                        ind.cache = (Dictionary<string, List<PostingInfo>>)bf.Deserialize(fs);//read object
                    }
                }
                catch (IOException)
                {
                    //cant find load and cache files in currect folder
                    System.Windows.Forms.MessageBox.Show("Files Missing, can't Load", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;

                }
                //for vieiwing
                ind.writeTextChache();
                showcatch.IsEnabled = true;
                ind.writeTextDic();
                showDic.IsEnabled = true;

                //new ranker and load the dictionaries of the class if the file exists in the selected folder
                rank = new Ranker(pathclose,p, r, ind, isStem);
                searcher = new Searcher(p, ind, rank,pathopen);
                //open the run btn
                runQuery.IsEnabled = true;
                //notify when finished
                System.Windows.Forms.MessageBox.Show("Ready To search!!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //if We wand wikipedia Expend
        private void isWiki_Checked(object sender, RoutedEventArgs e)
        {
            isWiki = isWikiExp.IsChecked.Value;
        }
        //if we want to summerize object
        private void isSummerize_checked(object sender, RoutedEventArgs e)
        {
            isSummarize = isSumCheck.IsChecked.Value;
        }
        //browse for query file btn
        private void browse_query_Click(object sender, RoutedEventArgs e)
        {
            //choose specific file
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "text File|*.txt";
            dlg.Title = "Open a query File";
            dlg.ShowDialog();

            if (dlg.FileName != "")
            {
                //change the dest path
                queryFile = dlg.FileName;
                //change the textbox to the path
                QueryTextBox.Text = queryFile;
                QueryTextBox.IsReadOnly = true;
            }
            
        }
        //start the Engine-PART A
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            //lock the gui for running
            if (!string.IsNullOrWhiteSpace(Corpustextbox.Text) && !string.IsNullOrWhiteSpace(Postingtextbox.Text))
            {
                Corpustextbox.IsEnabled = false;
                PostingSavingLocation.IsEnabled = false;
                CorpusFolderBrowse.IsEnabled = false;
                Postingtextbox.IsEnabled = false;
                StartEngine();// start full run
                showcatch.IsEnabled = true;
                showDic.IsEnabled = true;
            }
            else//if one of the boxes is empty
            {
                if (string.IsNullOrWhiteSpace(Corpustextbox.Text) && string.IsNullOrWhiteSpace(Postingtextbox.Text))
                {
                    System.Windows.Forms.MessageBox.Show("You MUST choose pathes before you can start", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (string.IsNullOrWhiteSpace(Postingtextbox.Text))
                {
                    System.Windows.Forms.MessageBox.Show("Posting save path MUST be filled", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                    System.Windows.Forms.MessageBox.Show("Corpus path MUST be filled", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
    }
}
