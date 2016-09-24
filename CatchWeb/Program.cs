using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using JHelper.DB;
using Newtonsoft.Json;

namespace CatchWeb
{
    class Program
    {
        public static bool StoreToMsSql = false;
        static void Main(string[] args)
        {
            SqlConnection cn = null;
            try
            {
                //if (args.Length == 0) throw new Exception("没有输入文件");
//                var inputtxt = args[0];
                var inputtxt = @"F:\GitHub\CatchWeb\CatchWeb\bin\Debug\网页抓取.json";
                var inputtxts = File.ReadAllText(inputtxt);
                var cwdsl = JsonConvert.DeserializeObject<CatchWebDsl>(inputtxts);
                if (cwdsl.MsSqlServerConfig != null && cwdsl.MsSqlServerConfig.ConnectionString != "")
                {
                    StoreToMsSql = true;
                    DbHelper.GetDatabase("con", cwdsl.MsSqlServerConfig.ConnectionString);
                    DbHelper.TestConnection();
                }

                var mainhtml = get_html(cwdsl.CatchWebSite.Url, cwdsl.CatchWebSite.Encode, cwdsl.CatchWebSite.Proxyurl, cwdsl.CatchWebSite.Proxyuser, cwdsl.CatchWebSite.Proxypw);
                var inserts = new Dictionary<string, string>();
                foreach (var catchWebContentModel in cwdsl.CatchWebSite.CatchWebContents)
                {
                    CQ dom = mainhtml;
                    foreach (var webContentPartsModel in catchWebContentModel.WebContentParts)
                    {
                        var text = dom[webContentPartsModel.Query].Text();
                        Console.WriteLine("标题:" + webContentPartsModel.Title + " 内容:" + text);
                        if (StoreToMsSql && catchWebContentModel.SqlTableName.Length > 0)
                        {
                            inserts.Add(webContentPartsModel.Title, text);
                        }
                    }
                    if (StoreToMsSql && catchWebContentModel.SqlTableName.Length > 0)
                    {
                        var count = 0;
                        if (catchWebContentModel.RemoveDuplicate.Length > 0)
                        {
                            count = Convert.ToInt32(DbHelper.ExecuteScalar(string.Format("Select count(*) FROM {1} WHERE {0} = '{2}'",
                                catchWebContentModel.RemoveDuplicate, catchWebContentModel.SqlTableName,
                                inserts[catchWebContentModel.RemoveDuplicate])));
                        }
                        if (count == 0)
                        {
                            DbHelper.InsertDictionary(inserts, catchWebContentModel.SqlTableName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.ReadLine();
            }
        }

        public static string get_html(string url, string encode = "", string proxyurl = "", string proxyuser = "", string proxypw = "")
        {
            string urlStr = url; //設定要獲取的地址 
            var hwr = (HttpWebRequest)WebRequest.Create(urlStr); //建立HttpWebRequest對象 
            hwr.Timeout = 60000; //定義服務器超時時間 
            hwr.ContentType = "application/x-www-form-urlencoded;";
            if (proxyurl != "")
            {
                WebProxy proxy = new WebProxy(); //定義一個網關對象 
                proxy.Address = new Uri(proxyurl); //網關服務器:端口 
                proxy.Credentials = new NetworkCredential(proxyuser, proxypw); //用戶名,密碼 
                hwr.UseDefaultCredentials = true; //啟用網關認証 
                hwr.Proxy = proxy; //設置網關 
            }
            if (encode == "")
            {
                encode = "UTF-8";
            }
            HttpWebResponse hwrs = (HttpWebResponse)hwr.GetResponse(); //取得回應 

            //判断HTTP响应状态 
            if (hwrs.StatusCode != HttpStatusCode.OK)
            {
                hwrs.Close();
                throw new Exception("get_html访问失败！");
            }
            else
            {
                Stream s = hwrs.GetResponseStream(); //得到回應的流對象 
                StreamReader sr = new StreamReader(s, Encoding.GetEncoding(encode)); //以UTF-8編碼讀取流 
                return sr.ReadToEnd();
            }
        }
    }


    public class CatchWebDsl
    {
        public CatchWebSiteModel CatchWebSite = new CatchWebSiteModel();
        public MsSqlServerConfigModel MsSqlServerConfig;
    }

    public class CatchWebSiteModel
    {
        public string Url = "";
        public string Encode = "";
        public string Proxyurl = "";
        public string Proxyuser = "";
        public string Proxypw = "";
        public List<CatchWebContentModel> CatchWebContents = new List<CatchWebContentModel>();
    }

    public class CatchWebContentModel
    {
        public string SqlTableName = "";
        public string RemoveDuplicate = "";
        public List<WebContentPartsModel> WebContentParts = new List<WebContentPartsModel>();
    }

    public class WebContentPartsModel
    {
        public string Title = "";
        public string Query = "";
    }

    public class MsSqlServerConfigModel
    {
        public string ConnectionString = "";
    }
}
