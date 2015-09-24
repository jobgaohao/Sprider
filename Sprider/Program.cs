using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprider
{
    class Program
    {
        static void Main(string[] args)
        {

            #region Trim()
            /*
            var s = "(../images/head_nav_bg.jpg)";
            var r = s.Trim(new char[] { '(' }).Trim(new char[] { ')' });
            Console.WriteLine(r);
            Console.ReadLine();
            **/ 
            #endregion
            string url=string.Empty;
            Console.WriteLine("请输入要下载的网站：");
            if (args.Length!=0)
            {
                url = args[0];
            }
            else
            {
              url= Console.ReadLine();   
            }
                   
            var crawler = new Crawler(url);
            Console.WriteLine("开始下载");
            crawler.DownLoad();
            //show 一下我们爬到的链接地址
            foreach (var item in Crawler.visited)
            {
                Console.WriteLine(item);
            }
            Console.ReadLine();            
        }

    }
}
