using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;

namespace DevelopexFinal
{
    class TextSearch
    {
        int status=0; //0=stop 1=work 2=pause
        int curentEl;
        const string linkpattern = @"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
        object lockCurentEl = new object();
        int total;
        object lockTotal = new object();
        int threadsNum;
        string text;
        int linkNum;
        int linkComplete;
        object locklinkComplete = new object();
        MainWindow window;
        public List<LinkStat> links { get; set; }
        Thread[] threads;
        Thread mainThread;

        string DeleteTags(string html)
        {
            string noHTML = Regex.Replace(html, @"<[^>]+>|&nbsp;", "").Trim();
            string noHTMLNormalised = Regex.Replace(noHTML, @"\s{2,}", " ");
            return noHTMLNormalised;
        }
        void SearchThread()
        {
            while  (linkComplete < total)
            {
                bool work = false;
                LinkStat link = new LinkStat();
                lock (lockCurentEl)
                {
                    if (curentEl < total)
                    {
                        link = links[curentEl];
                        curentEl++;
                        work = true;
                    }
                }
                if (work)
                {
                    link.Status = "Loading";
                    WebClient web = new WebClient();
                    try
                    {
                        string html = web.DownloadString(link.Link);
                        string nohtml = DeleteTags(html);
                        if (nohtml.IndexOf(text) == -1)
                        {
                            link.Status = "Not fond";
                        }
                        else
                        {
                            link.Status = "Find";
                        }
                        Match m = Regex.Match(html, linkpattern);
                        while (m.Success)
                        {
                            lock (lockTotal)
                            {
                                if (total < linkNum)
                                {
                                    links.Add(new LinkStat() { Link = m.Value, Status = "Wait" });
                                    total++;
                                    m = m.NextMatch();
                                }
                                else
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        link.Status = ex.Message;
                    }
                    finally
                    {
                        lock (locklinkComplete)
                        {
                            linkComplete++;
                        }
                    }
                }

            }
        }
       public void Start(MainWindow win,string url, int thrNum, string searchText, int urlNum )
        {

            if (status == 0)
            {
                threadsNum = thrNum;
                text = searchText;
                linkNum = urlNum;
                links = new List<LinkStat>(linkNum);
                LinkStat link = new LinkStat() { Link = url, Status = "Wait" };
                links.Add(link);
                total = 1;
                curentEl = 0;
                window = win;
                mainThread = new Thread(ControlThread);
                mainThread.Start();
                threads = new Thread[threadsNum];
                for (int i = 0; i < threadsNum; i++)
                {
                    threads[i] = new Thread(SearchThread);
                    threads[i].Start();
                }
            }
            else
            {
                mainThread.Resume();
                for (int i = 0; i < threadsNum; i++)
                {
                    threads[i].Resume();
                }
            }
           

        }
        public void ControlThread()
        {
            while(linkComplete< total)
            {
                Thread.Sleep(500);
                window.Dispatcher.Invoke(delegate
                {
                    window.listView.ItemsSource = links;
                    window.listView.Items.Refresh();
                    window.Progres.Maximum = linkNum;
                    window.Progres.Value = linkComplete;
                });
            }
            window.Dispatcher.Invoke(delegate
            {
                window.listView.Items.Refresh();
                window.Progres.Maximum = linkNum;
                window.Progres.Value = linkComplete;
                window.textBlock4.Text = "Loaded";
                window.button1.IsEnabled = false;
                window.button.IsEnabled = true;
                window.button2.IsEnabled = false;
            });
            status = 0;
        }
        public void Stop()
        {
            status = 0;
            
            for(int i=0;i<threadsNum;i++)
            {
                threads[i].Abort(); 
            }
            mainThread.Abort();
        }
        public void Pause()
        {
            
                status = 2;
            
            for (int i = 0; i < threadsNum; i++)
            {
                threads[i].Suspend();
            }
            mainThread.Suspend();
        }

    }
}
