using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SearchEngine
{
    /// <summary>
    /// Interaction logic for Results.xaml
    /// </summary>
    public partial class Results : Window
    {
        List<string> ans;
        public static string LastFile;
        //Initilize the Windows textbox with text and the save btn
        public Results(bool save,string res,List<string> ans)
        {
            InitializeComponent();
            Txt.Text = res;
            Savebtn.IsEnabled = save;
            this.ans = ans;
            Txt.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            Txt.IsEnabled = true;
            Txt.IsReadOnly = true;
            
            
        }
        //Initilize the Windows textbox with text and the save btn
        public Results(bool save, string res)
        {
            InitializeComponent();
            Txt.Text = res;
            Savebtn.IsEnabled = save;
            Txt.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            Txt.IsEnabled = true;
            Txt.IsReadOnly = true;
        }
        //save the query file on the Treceval Format
        private void saveClick(object sender, RoutedEventArgs e)
        {
            string s = Txt.Text;
            string[] str = s.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //open save dialog
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text File (.txt)|*.txt";
            dlg.Title = "Save Query FILE";
            dlg.ShowDialog();

            // If the file name is not an empty string open it for saving.  
            if (dlg.FileName != "")
            {
                LastFile = dlg.FileName;
                StreamWriter sr = new StreamWriter(dlg.FileName);
                foreach(string docName in ans)
                {
                    sr.WriteLine(docName + " 0 42.38 mt");
                }
                sr.Close();
                //notify when finish
                System.Windows.Forms.MessageBox.Show("Saved!", "SAVED!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                return;

        }
    }
}
