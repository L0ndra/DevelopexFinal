using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DevelopexFinal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TextSearch textSearch;
        public MainWindow()
        {
            InitializeComponent();
            textSearch = new TextSearch();
            button1.IsEnabled = false;
            button2.IsEnabled = false;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            string url = textBox.Text;
            int threadNum;
            string text = textBox2.Text;
            int linkNum;

            if (url == "")
            {
                textBlock4.Text = "Havent link";
            }
            else
            if ((!Int32.TryParse(textBox1.Text, out threadNum))&&(threadNum<1))
            {
                textBlock4.Text = "Wrong Number of Threads";
            }
            else
            if (text == "")
            {
                textBlock4.Text = "Write text for search";
            }
            else
            if ((!Int32.TryParse(textBox3.Text, out linkNum))&&(linkNum<1))
            {
                textBlock4.Text = "Wrong Number of Links";
            }
            else
            {
                button.IsEnabled = false;
                button1.IsEnabled = true;
                button2.IsEnabled = true;
                textBlock4.Text = "Loading";
                textSearch.Start(this, url, threadNum, text, linkNum);

            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = true;
            button1.IsEnabled = false;
            button2.IsEnabled = false;
            textBlock4.Text = "Stoped";
            textSearch.Stop();
        }
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = true;
            button1.IsEnabled = false;
            button2.IsEnabled = false;
            textBlock4.Text = "Paused";
            textSearch.Pause();
        }
    }
}
