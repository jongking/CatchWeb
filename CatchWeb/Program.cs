using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Output;
using JHelper.DB;
using Newtonsoft.Json;

namespace CatchWeb
{
    class Program
    {
        public static bool StoreToMsSql = false;
        public static int SleepTime = 0;
        static void Main(string[] args)
        {
            SqlConnection cn = null;
            try
            {
                if (args.Length == 0) throw new Exception("没有输入文件");
                var inputtxt = args[0];
                //var inputtxt = @"F:\Github\CatchWeb\CatchWeb\网页抓取.json";
                CsQuery.Config.HtmlEncoder = new HtmlEncoderMinimum();
                var inputtxts = File.ReadAllText(inputtxt);
                var cwdsl = JsonConvert.DeserializeObject<CatchWebDsl>(inputtxts);
                if (cwdsl.MsSqlServerConfig != null && cwdsl.MsSqlServerConfig.ConnectionString != "")
                {
                    StoreToMsSql = true;
                    DbHelper.GetDatabase("con", cwdsl.MsSqlServerConfig.ConnectionString);
                    DbHelper.TestConnection();
                }
                SleepTime = cwdsl.SleepTime;

                var catchWebSite = cwdsl.CatchWebSite;
                CatchWebSiteFun(catchWebSite);
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

        public static void CatchWebSiteFun(CatchWebSiteModel catchWebSite, string param0 = "", Dictionary<string, object> parentInsert = null)
        {
            var url = catchWebSite.Url;
            var start = 0;
            var end = 1;
            var indexof = catchWebSite.Param1.IndexOf("[TO]", StringComparison.Ordinal);
            if (catchWebSite.Param1.Length > 0)
            {
                if (indexof > -1)
                {
                    start = Convert.ToInt32(catchWebSite.Param1.Substring(0, indexof));
                    end = Convert.ToInt32(catchWebSite.Param1.Substring(indexof + 4));
                }
                else
                {
                    url = string.Format(url, param0, catchWebSite.Param1);
                }
            }

            for (int i = start; i < end; i++)
            {
                if (param0!= "" || catchWebSite.Param1 != "")
                {
                    url = string.Format(url, param0, i);
                }
                var mainhtml = get_html(url, catchWebSite.Encode, catchWebSite.Proxyurl, catchWebSite.Proxyuser, catchWebSite.Proxypw);
                if (mainhtml == null)
                {
                    continue;
                }
                foreach (var catchWebContentModel in catchWebSite.CatchWebContents)
                {
                    var inserts = new List<Dictionary<string, object>>();
                    CQ dom = mainhtml;
                    var joinByList = new List<string>();
                    var notonece = false;
                    var removeDuplicateArr = new List<int>();
                    foreach (var webContentPartsModel in catchWebContentModel.WebContentParts)
                    {
                        var cq = dom[webContentPartsModel.Query];
                        for (int j = 0; j < cq.Length; j++)
                        {
                            if (removeDuplicateArr.Contains(j))
                            {
                                continue;
                            }

                            var query = cq.Eq(j);
                            if (webContentPartsModel.FindQuery.Length > 0)
                            {
                                query = query.Find(webContentPartsModel.FindQuery);
                            }
                            var text = "";
                            if (webContentPartsModel.Attr == "")
                            {
                                if (!webContentPartsModel.IsHtml)
                                {
                                    text = query.Text();
                                }
                                else
                                {
                                    text = query.Html();
                                }
                            }
                            else
                            {
                                text = query.Attr(webContentPartsModel.Attr);
                            }
                            if (webContentPartsModel.LastStartWith.Length > 0)
                            {
                                var last = text.LastIndexOf(webContentPartsModel.LastStartWith, StringComparison.Ordinal);
                                if (last > -1)
                                {
                                    text = text.Substring(last + webContentPartsModel.LastStartWith.Length);
                                }
                            }
                            if (webContentPartsModel.BeforeAdd.Length > 0)
                            {
                                text = webContentPartsModel.BeforeAdd + text;
                            }
                            if (webContentPartsModel.IsTrim)
                            {
                                text = text.Trim();
                            }
                            if (webContentPartsModel.JoinBy.Length > 0)
                            {
                                joinByList.Add(text);
                                if (j + 1 < cq.Length)
                                {
                                    continue;
                                }
                                else
                                {
                                    text = string.Join(webContentPartsModel.JoinBy, joinByList);
                                    joinByList.Clear();
                                }
                            }

                            object insertvalue;
                            if (webContentPartsModel.IsImage)
                            {
                                insertvalue = get_image(text, catchWebSite.Proxyurl, catchWebSite.Proxyuser,
                                    catchWebSite.Proxypw);
                            }
                            else if (webContentPartsModel.IsDate)
                            {
                                insertvalue = Convert.ToDateTime(text);
                            }
                            else
                            {
                                insertvalue = text;
                            }


                            if (StoreToMsSql && catchWebContentModel.SqlTableName.Length > 0 && webContentPartsModel.Title == catchWebContentModel.BeforeRemoveDuplicate)
                            {
                                var count = Convert.ToInt32(DbHelper.ExecuteScalar(string.Format("Select count(*) FROM {1} WHERE {0} = '{2}'",
                                    catchWebContentModel.BeforeRemoveDuplicate, catchWebContentModel.SqlTableName,
                                    text)));
                                if (count > 0)
                                {
                                    //Console.WriteLine("跳过" + webContentPartsModel.Title + "等于" + text + "的项目");
                                    removeDuplicateArr.Add(j);
                                    if (!notonece)
                                    {
                                        inserts.Add(new Dictionary<string, object>());
                                    }
                                }
                            }
                            if (notonece)
                            {
                                if (webContentPartsModel.JoinBy.Length == 0)
                                {
                                    inserts[j].Add(webContentPartsModel.Title, insertvalue);
                                }
                                else
                                {
                                    inserts[0].Add(webContentPartsModel.Title, insertvalue);
                                }
                            }
                            else
                            {
                                var d = new Dictionary<string, object>();
                                if (catchWebContentModel.Param0Title.Length > 0)
                                {
                                    d.Add(catchWebContentModel.Param0Title, param0);
                                }
                                d.Add(webContentPartsModel.Title, insertvalue);
                                inserts.Add(d);
                            }
                            //Console.WriteLine("标题:" + webContentPartsModel.Title + " 内容:" + text);
                            foreach (var catchWebSiteModel in webContentPartsModel.CatchWebSites)
                            {
                                Thread.Sleep(SleepTime);
                                CatchWebSiteFun(catchWebSiteModel, text, inserts[j]);
                            }
                        }
                        notonece = true;
                    }
                    if (StoreToMsSql && catchWebContentModel.SqlTableName.Length > 0)
                    {
                        for (int j = 0; j < inserts.Count; j++)
                        {
                            if (removeDuplicateArr.Contains(j))
                            {
                                continue;
                            }

                            var count = 0;
                            if (catchWebContentModel.AfterRemoveDuplicate.Length > 0)
                            {
                                count = Convert.ToInt32(DbHelper.ExecuteScalar(string.Format("Select count(*) FROM {1} WHERE {0} = '{2}'",
                                    catchWebContentModel.AfterRemoveDuplicate, catchWebContentModel.SqlTableName,
                                    inserts[j][catchWebContentModel.AfterRemoveDuplicate])));
                            }
                            if (count == 0)
                            {
                                DbHelper.InsertDictionary(inserts[j], catchWebContentModel.SqlTableName);
                            }
                        }
                    }
                    if (catchWebContentModel.IsInsertToParent && parentInsert != null && inserts.Any())
                    {
                        foreach (var childinsert in inserts[0])
                        {
                            parentInsert.Add(childinsert.Key, childinsert.Value);
                        }
                    }
                }

                if (parentInsert == null)
                {
                    Console.WriteLine("Complete:" + i + "/" + end);
                }
                else
                {
                    Console.Write(".");
                }
            }
        }

        public static string get_html(string url, string encode = "", string proxyurl = "", string proxyuser = "", string proxypw = "")
        {
            try
            {
                string urlStr = url; //設定要獲取的地址 
                var hwr = (HttpWebRequest) WebRequest.Create(urlStr); //建立HttpWebRequest對象 
                hwr.Timeout = 5000; //定義服務器超時時間 
                hwr.Method = "GET";
                hwr.ContentType = "application/x-www-form-urlencoded;";
                hwr.UserAgent =
                    "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
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
                HttpWebResponse hwrs = (HttpWebResponse) hwr.GetResponse(); //取得回應 

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
                    var str = sr.ReadToEnd();
                    s.Close();
                    sr.Close();
                    return str;
                }
            }
            catch
            {
                return null;
            }
        }

        public static byte[] get_image(string url, string proxyurl = "", string proxyuser = "", string proxypw = "")
        {
            try
            {
                string urlStr = url; //設定要獲取的地址 
                var hwr = (HttpWebRequest)WebRequest.Create(urlStr); //建立HttpWebRequest對象 
                hwr.Timeout = 60000; //定義服務器超時時間 
                hwr.ContentType = "application/x-www-form-urlencoded;";
                hwr.UserAgent =
                    "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";
                if (proxyurl != "")
                {
                    WebProxy proxy = new WebProxy(); //定義一個網關對象 
                    proxy.Address = new Uri(proxyurl); //網關服務器:端口 
                    proxy.Credentials = new NetworkCredential(proxyuser, proxypw); //用戶名,密碼 
                    hwr.UseDefaultCredentials = true; //啟用網關認証 
                    hwr.Proxy = proxy; //設置網關 
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
                    List<byte> bytes = new List<byte>();
                    int rb;
                    while ((rb = s.ReadByte()) != -1)
                    {
                        bytes.Add((byte)rb);
                    }
                    s.Dispose();
                    return bytes.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受     
        }
    }


    public class CatchWebDsl
    {
        public CatchWebSiteModel CatchWebSite = new CatchWebSiteModel();
        public MsSqlServerConfigModel MsSqlServerConfig;
        public int SleepTime = 3000;
    }

    public class CatchWebSiteModel
    {
        public string Url = "";
        public string Param1 = "";
        public string Encode = "";
        public string Proxyurl = "";
        public string Proxyuser = "";
        public string Proxypw = "";
        public List<CatchWebContentModel> CatchWebContents = new List<CatchWebContentModel>();
    }

    public class CatchWebContentModel
    {
        public string SqlTableName = "";
        public string BeforeRemoveDuplicate = "";
        public string AfterRemoveDuplicate = "";
        public string Param0Title = "";
        public bool IsInsertToParent = false;
        public List<WebContentPartsModel> WebContentParts = new List<WebContentPartsModel>();
    }

    public class WebContentPartsModel
    {
        public string Title = "";
        public string Query = "";
        public string FindQuery = "";
        public string Attr = "";
        public string BeforeAdd = "";
        public string LastStartWith = "";
        public string JoinBy = "";
        public bool IsTrim = false;
        public bool IsImage = false;
        public bool IsDate = false;
        public bool IsHtml = false;
        public List<CatchWebSiteModel> CatchWebSites = new List<CatchWebSiteModel>();
    }

    public class MsSqlServerConfigModel
    {
        public string ConnectionString = "";
    }
}
