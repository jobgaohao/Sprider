using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace Sprider
{
    public class Crawler
    {
        //download 文件根目录
        public static string basePath = @"C:/HTML/";
        //本次下载目录
        public  string rootPath = "";
        //基地址
        public static Uri baseUri;
        public static Uri uri;
        public static string baseHost = string.Empty;
        //IS CSS
        public static bool isCss=false;

        /// <summary>
        /// 工作队列
        /// </summary>
        public static Queue<string> todo = new Queue<string>();
        //已访问的队列
        public static HashSet<string> visited = new HashSet<string>();

        public Crawler(string url)
        {           
            baseUri = new Uri(url);
            uri = baseUri;
            //基域
            baseHost = baseUri.Host.Substring(baseUri.Host.IndexOf('.'));
            rootPath = basePath + "www" + baseHost;
            //抓取首地址入队
            todo.Enqueue(url);
        }

        public void DownLoad()
        {
            while (todo.Count > 0)
            {
                var currentUrl = todo.Dequeue();
                if (currentUrl.IndexOf("css")>-1)
                {
                    isCss = true;
                }
                //当前url标记为已访问过
                visited.Add(currentUrl);
                baseUri=uri;
                if (currentUrl.IndexOf(baseUri.ToString())==0 && currentUrl.LastIndexOf("/") > baseUri.ToString().Length)
                {
                  string currentUrl_temp = currentUrl.Substring(0, currentUrl.LastIndexOf("/")+1);
                  baseUri = new Uri(currentUrl_temp);
                }
                var request = WebRequest.Create(currentUrl) as HttpWebRequest;      
                try
                {
                    request.Method = "GET";
                    request.UserAgent = "Opera/9.25 (Windows NT 6.0; U; en)";
                    request.KeepAlive = true;
                    //此处GetResponse超过的原因是，当前存在太多数目的alive的http连接（大于10个），所以再次提交同样的http的request，再去GetResponse，就会超时死掉。
                    //解决办法就是，把DefaultConnectionLimit 设置为一个比较大一点的数值，此数值保证大于你当前已经存在的alive的http连接数即可。
                    System.Net.ServicePointManager.DefaultConnectionLimit = 50;
                    HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
                    if (resp.StatusCode != HttpStatusCode.OK) //如果服务器未响应，那么继续等待相应
                    { 
                        continue;
                    }
                    var sr = new StreamReader(resp.GetResponseStream());
                    //提取url，将未访问的放入todo表中
                    RefineUrl(sr.ReadToEnd());
                    sr.Close();
                    resp.Close();                    
                }
                catch (Exception ex)
                {
                    string mes = ex.ToString();
                }
                finally 
                {
                    request.Abort();
                }
            }
        }

        /// <summary>
        /// 提取Url
        /// </summary>
        /// <param name="html"></param>
        public void RefineUrl(string html)
        {
            #region  URL
            Regex reg = new Regex(@"(?is)<a[^>]*?href=(['""]?)(?<url>[^'""\s>]+)\1[^>]*>(?<text>(?:(?!</?a\b).)*)</a>");
            MatchCollection mc = reg.Matches(html);//符合条件的一个集合
            foreach (Match m in mc)
            {
                var url = m.Groups["url"].Value;
                if (url == "#"||url.IndexOf("#")>-1)
                {
                    continue;
                }

                if (url.LastIndexOf("/") == url.Length - 1)
                {
                    url =url+ "index.html";
                }
                else
                {
                    url = url.Substring(url.LastIndexOf("/") + 1);
                }
               
                //相对路径转换为绝对路径                
                Uri uri = new Uri(baseUri, url);
                //剔除外网链接(获取顶级域名){
                if (!uri.Host.EndsWith(baseHost))
                {
                    continue;
                }
               
                if (!visited.Contains(uri.ToString())&&!todo.Contains(uri.ToString()))
                {                                                          
                    todo.Enqueue(uri.ToString());
                    MakeTextToFile(uri.ToString(), url);
                }
            }
            #endregion
            #region CSS ICO
            //<link href="css/base.css" rel="stylesheet" type="text/css" />
            Regex reg1 = new Regex(@"(?is)<link [^>]*?href=(['""]?)(?<url>[^'""\s>]+)\1[^>]*>");
            MatchCollection mc1 = reg1.Matches(html);//符合条件的一个集合
            foreach (Match m in mc1)
            {               
                var url = m.Groups["url"].Value;
                if (url == "#" || url.IndexOf("#") > -1)
                {
                    continue;
                }
                //相对路径转换为绝对路径
                Uri uri = new Uri(baseUri, url);
                if (!visited.Contains(uri.ToString()) && !todo.Contains(uri.ToString()))
                {
                    if (uri.ToString().ToLower().IndexOf("ico") > 0)
                    {
                        MakeStreamFileToFile(uri.ToString(), url);
                    }
                    else if (uri.ToString().ToLower().IndexOf("css") > 0)
                    {
                        MakeTextToFile(uri.ToString(), url);

                        //CSS 里面包含图
                        if (!visited.Contains(uri.ToString()))
                        {
                            todo.Enqueue(uri.ToString());
                        }
                    }
                }                
            }
            #endregion
            #region JS
            //<script src="js/eye.js" type="text/javascript"></script>
            reg = new Regex(@"(?is)<script[^>]*?src=(['""]?)(?<url>[^'""\s>]+)\1[^>]*></script>");
            mc = reg.Matches(html);//符合条件的一个集合
            foreach (Match m in mc)
            {
                var url = m.Groups["url"].Value;
                if (url == "#" || url.IndexOf("#") > -1)
                {
                    continue;
                }
                //相对路径转换为绝对路径
                Uri uri = new Uri(baseUri, url);
                if (!visited.Contains(uri.ToString()) && !todo.Contains(uri.ToString()))
                {
                    if (uri.ToString().ToLower().IndexOf("js") > 0)
                    {
                        MakeTextToFile(uri.ToString(), url);
                    }
                }
            }
            #endregion
            #region img
              #region  页面上的图片
            //[<img src="images/logo.jpg" width="139" height="38" alt="上海岱嘉医学信息系统有限公司"/>]            
            reg = new Regex(@"(?is)<img[^>]*?src=(['""]?)(?<url>[^'""\s>]+)\1[^>]*>");
            mc = reg.Matches(html);//符合条件的一个集合
            foreach (Match m in mc)
            {
                var url = m.Groups["url"].Value;
                if (url == "#" || url.IndexOf("#") > -1)
                {
                    continue;
                }
                //相对路径转换为绝对路径
                Uri uri = new Uri(baseUri, url);
                if (!visited.Contains(uri.ToString()) && !todo.Contains(uri.ToString()))
                {
                    if (uri.ToString().ToLower().IndexOf("jpg") > 0 || uri.ToString().ToLower().IndexOf("jpeg") > 0 || uri.ToString().ToLower().IndexOf("gif") > 0 || uri.ToString().ToLower().IndexOf("png") > 0 || uri.ToString().ToLower().IndexOf("bmp") > 0)
                    {
                        MakeStreamFileToFile(uri.ToString(), url);
                    }
                }
            }
            #endregion
              #region  css 里的图片
                //[<img src="images/logo.jpg" width="139" height="38" alt="上海岱嘉医学信息系统有限公司"/>]     
                //.hotJob_dian{ background:url(../images/icon.gif) no-repeat left 6px; line-height:20px;text-indent:1em;}
               // reg = new Regex(@"(?is)<img[^>]*?src=(['""]?)(?<url>[^'""\s>]+)\1[^>]*>");
                if (isCss==true)
                {
                    reg = new Regex(@"(?is)url(?<url>[^'""\s>]+)");
                    mc = reg.Matches(html);//符合条件的一个集合
                    foreach (Match m in mc)
                    {
                        var url = m.Groups["url"].Value;
                        url=url.Trim(new char[] { '(' }).Trim(new char[] { ')' });

                        if (url == "#")
                        {
                            continue;
                        }
                        //相对路径转换为绝对路径
                        Uri uri = new Uri(baseUri, url);
                        if (uri.ToString().ToLower().IndexOf("jpg") > 0 || uri.ToString().ToLower().IndexOf("jpeg") > 0 || uri.ToString().ToLower().IndexOf("gif") > 0 || uri.ToString().ToLower().IndexOf("png") > 0 || uri.ToString().ToLower().IndexOf("bmp") > 0)
                        {
                            MakeStreamFileToFile(uri.ToString(), url);
                        }
                    }             
                }            
              #endregion

            #endregion
        }
       
        /// <summary>
        /// 把文本保存到指定目录
        /// </summary>
        /// <param name="mes">文本</param>
        /// <param name="url">保存目录</param>
        public void MakeMesToFile(string mes,string url) 
        {
            string tempPath = "www" + baseHost;
            if (url.IndexOf(tempPath)>0)
            {
                int lastIndex = url.LastIndexOf("/")-6;//http://
                tempPath = url.Substring(url.IndexOf(tempPath));
                tempPath = tempPath.Substring(0,lastIndex);
            }

            if (!Directory.Exists(basePath + tempPath))
            {
                Directory.CreateDirectory(basePath + tempPath);
            }
            string lastInfo = url.Substring(url.LastIndexOf("/")+1);
            if (lastInfo.IndexOf(".") > 0)
            {
                FileStream fs = new FileStream(basePath + tempPath + @"\" + lastInfo, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.Write(mes);
                sw.Close();
                fs.Close();
            }
            else 
            {
                FileStream fs = new FileStream(basePath + tempPath + @"\" + "index.html", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.Write(mes);
                sw.Close();
                fs.Close();
            }
        }

        /// <summary>
        /// 根据URL 获取文本保存到本地
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="uname">文本路径保存地址</param>
        public void MakeTextToFile(string url,string uname) 
        {
            var request = WebRequest.Create(url) as HttpWebRequest;
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                var sr = new StreamReader(response.GetResponseStream());
                #region 创建目录
                string tempPath = "www" + baseHost;
                if (url.IndexOf(tempPath) > 0)
                {
                    int lastIndex = url.LastIndexOf("/") - 6;//http://
                    tempPath = url.Substring(url.IndexOf(tempPath));
                    tempPath = tempPath.Substring(0, lastIndex);
                }

                if (!Directory.Exists(basePath + tempPath))
                {
                    Directory.CreateDirectory(basePath + tempPath);
                }
                #endregion
                string lastInfo = string.Empty;
                if (url.LastIndexOf("/") == url.Length-1)
                {
                    lastInfo = "index.html";
                }
                else 
                {
                    lastInfo = url.Substring(url.LastIndexOf("/") + 1);
                }
                
                FileStream fs = new FileStream(basePath + tempPath + @"/" + lastInfo, FileMode.Create);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.Write(sr.ReadToEnd());
                sw.Close();
                fs.Close();

                sr.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                string mes = ex.ToString();
                return;
            }      
        }
        
        /// <summary>
        /// 根据URL 把二进制文件保存到本地
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="uname">保存目录</param>
        public void MakeStreamFileToFile(string url,string uname) 
        {
           
            string dertory = uname;
            string fileName = url.Substring(url.LastIndexOf(@"/") + 1);
            string tempPath = "www" + baseHost;
            if (url.IndexOf(tempPath) > 0)
            {
                int lastIndex = url.LastIndexOf("/") - 6;//http://
                tempPath = url.Substring(url.IndexOf(tempPath));
                tempPath = tempPath.Substring(0, lastIndex);
            }

            if (!Directory.Exists(basePath + tempPath))
            {
                Directory.CreateDirectory(basePath + tempPath);
            }
            
           string LocalPath = basePath + tempPath + @"/" + fileName;
            HttpWebRequest mRequest = (HttpWebRequest)WebRequest.Create(url);
            mRequest.Method = "GET";
            mRequest.ContentType = "application/x-www-form-urlencoded";
            HttpWebResponse wr = (HttpWebResponse)mRequest.GetResponse();
            Stream sIn = wr.GetResponseStream();
            FileStream fs = new FileStream(LocalPath, FileMode.OpenOrCreate, FileAccess.Write);

            long length = wr.ContentLength;
            long i = 0;
            int k = 0;
            while (i < length)
            {
                byte[] buffer = new byte[1024];
                k = sIn.Read(buffer, 0, buffer.Length);
                i = i + k;
                fs.Write(buffer, 0, k);
            }

            sIn.Close();
            wr.Close();
            fs.Close();
        }


        #region 正则获取相关的数据
        /// <summary>
        /// 根据Name 获取input 里的内容
        /// </summary>
        /// <param name="mes"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetInputInfoByName(string mes, string name)
        {
            string value_temp = string.Empty;
            Regex regValue = new Regex(@"(?is)<input ?value=(['""]?)(?<showValue>[^'""\s>]+)\1 [^>]*>");
            MatchCollection mc = regValue.Matches(mes);//所有<input >标签集合
            foreach (Match item in mc)
            {
                if (item.ToString().IndexOf("name=" + name) > -1)
                {
                    value_temp += item.Groups["showValue"].Value;//获取value 值
                    break;
                }
            }
            return value_temp;
        }


        /// <summary>
        /// 提取文本框的值
        /// </summary>
        /// <param name="mes"></param>
        /// <returns></returns>
        public string InputToLable(string mes)
        {
            //System.IO.File.WriteAllText("C:\\log.txt", mes);            
            //IE6下不知道 为啥 就是不行
            ////获取所有的<input>标记
            Regex regAllInput = new Regex(@"(?is)<input [^>]*>");

            //获取 <input value=''>的标记 
            //?value=(['""]?)(?<showValue>[^'""\s>]+)\1  给value 的值取个标记 showValue，MatchCollection 时候方便获取
            //Regex regValue = new Regex(@"(?is)<input ?value=(['""]?)(?<showValue>[^'""\s>]+)\1 [^>]*>");
            Regex regValue = new Regex(@"(?is)<input [\s\S]*?value=(['""]?)(?<showValue>[^'""\s>]+)\1 [^>]*>");
            MatchCollection mc = regAllInput.Matches(mes);//所有<input >标签集合
            string value_temp = string.Empty;
            foreach (Match m in mc)
            {
                //所有<input value='' >标签集合
                MatchCollection mcItem = regValue.Matches(m.ToString());
                foreach (Match item in mcItem)
                {
                    value_temp = item.Groups["showValue"].Value;//获取value 值
                }
                mes = mes.Replace(m.ToString(), value_temp);//进行替换
            }
            //System.IO.File.AppendAllText("C:\\log.txt", mes);           
            return mes;
        }
        #endregion
    }
}
