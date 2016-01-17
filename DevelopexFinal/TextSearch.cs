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
        int curentEl; //Наступний елемент обробки
        const string linkpattern = @"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";//Регулярний вираз для посилань
        object lockCurentEl = new object();//Об'єкт для урегулювання доступу до curentEl
        int total; //Кількість посилань доданих для обробки
        object lockTotal = new object();//Об'єкт для урегулювання доступу до total
        int threadsNum; //Кількість оброблюючих потоків 
        string text;//Текст який шукаєтся за посиланнями
        int linkNum; //Максимальна кількість ссилок 
        int linkComplete; //Кількість оброблених ссилок
        object locklinkComplete = new object();//Об'єкт для урегулювання доступу до linkComplete
        MainWindow window; //Екземпляр робочого вікна, архітектурно неправильне рішення, потрібно замінити на делігати
        public List<LinkStat> links { get; set; }//Список з посиланнями, та їх станом
        Thread[] threads; //Масив з посиланнями на робочі потоки
        Thread mainThread; //Посилання на головний потік, який займаєтся оновленням графічного інтерфейсу

        string DeleteTags(string html)//Метод видалення html тегів
        {
            string noHTML = Regex.Replace(html, @"<[^>]+>|&nbsp;", "").Trim();
            string noHTMLNormalised = Regex.Replace(noHTML, @"\s{2,}", " ");
            return noHTMLNormalised;
        }
        void SearchThread()//Метод пошуку, який виконуэтся в заданих потоках
        {
            while  (linkComplete < total) //Виконуєтся доки є можливість наявності посилань для обробки
            {
                bool work = false;
                LinkStat link = new LinkStat();
                lock (lockCurentEl)
                {
                    if (curentEl < total) //Перевірка наявності не обробленних посилань
                    {
                        link = links[curentEl];
                        curentEl++;
                        work = true;
                    }
                }
                if (work) //Пошук текста за посиланням
                {
                    link.Status = "Loading";
                    WebClient web = new WebClient();
                    try
                    {
                        //Завантаження сторінки за посиланням
                        string html = web.DownloadString(link.Link);
                        string nohtml = DeleteTags(html);
                        //Пошук текста на сторінці
                        if (nohtml.IndexOf(text) == -1)
                        {
                            link.Status = "Not fond";
                        }
                        else
                        {
                            link.Status = "Find";
                        }
                        //Пошук посилань на сторінці 
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
                        lock (locklinkComplete) //Збільшення кількості обробленних посилань
                        {
                            linkComplete++;
                        }
                    }
                }

            }
        }
       public void Start(MainWindow win,string url, int thrNum, string searchText, int urlNum )//Метод запуску потоків, або оновлення їх роботи
        {
            
            if (status == 0)//Стоврення потоків обробки, а також ініціалізація змінних
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
            {   //Відновлення роботи потоків вразі їх зупинки
                mainThread.Resume();
                for (int i = 0; i < threadsNum; i++)
                {
                    threads[i].Resume();
                }
            }
           

        }
        public void ControlThread()// Метод оновлення графічного інтерфейсу 
        {
            while(linkComplete< total)
            {   
                Thread.Sleep(500);
                //Оновлення інтерфейсу за допомогою диспатчера вікна, для покращення архітектури необхідно замінити на делегат 
                window.Dispatcher.Invoke(delegate 
                {
                    window.listView.ItemsSource = links;
                    window.listView.Items.Refresh();
                    window.Progres.Maximum = linkNum;
                    window.Progres.Value = linkComplete;
                });
            }
            //Оновлення інтерфейсу після завершення обробки
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
        public void Stop()//Завершення роботи потоків
        {
            status = 0;
            
            for(int i=0;i<threadsNum;i++)
            {
                threads[i].Abort(); 
            }
            mainThread.Abort();
        }
        public void Pause()//Призупинення роботи потоків
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
