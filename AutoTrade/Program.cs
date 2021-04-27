using Hangfire;
using Microsoft.Owin.Hosting;
using System;
using System.Configuration;
using System.IO;
using Topshelf;
using RestSharp;
using Newtonsoft.Json;
using Topshelf.Logging;
using Hangfire.Storage;

namespace AutoTrade
{
    class Program
    {
        static void Main(string[] args)
        {
            string hangfireDashboard = ConfigurationManager.AppSettings["hangfireDashboard"].ToString();
            using (WebApp.Start<Startup>(hangfireDashboard))
            {
                var hf = HostFactory.Run(r =>
                {   //注入 TownCrier服务
                    r.UseLog4Net("log4net.config");
                    r.Service<TownCrier>(s =>
                    {
                        s.ConstructUsing(n => new TownCrier());
                        s.WhenStarted(w => w.Start());
                        s.WhenStopped(w => w.Stop());
                    });
                    r.RunAsLocalSystem();//使用本地系统开始运行


                    r.SetDescription("自动对账服务,每天固定时间运行，自动采集账单数据");
                    r.SetDisplayName("AutoTrade");
                    r.SetServiceName("AutoTrade");

                    Console.WriteLine($"{DateTime.Now} 服务启动,仪表盘地址:{hangfireDashboard}/hangfire");
                });
                int exitCode = (int)Convert.ChangeType(hf, hf.GetTypeCode());
                Environment.ExitCode = exitCode;//服务退出代码
            }
        }
    }
    /// <summary>
    /// 😉
    /// </summary>
    public class TownCrier
    {
        private static readonly LogWriter logger = HostLogger.Get<TownCrier>();

        string baseApi = ConfigurationManager.AppSettings["baseApi"].ToString();
        public TownCrier() { }
        public void Start()
        {
            logger.Info("服务启动");
            using (var server = new BackgroundJobServer())
            {
                try
                {
                    RecurringJob.AddOrUpdate("下载银行卡数据", () => DownloadBankData(), "0 0 0/1 * * ?");
                    RecurringJob.AddOrUpdate("下载扫码数据", () => DownloadQrCode(), "0 0 0/3 * * ?");
                    RecurringJob.AddOrUpdate("下载HIS数据数据", () => DownloadHisData(), "0 0 0/5 * * ?");

                    RecurringJob.AddOrUpdate("定时16", () => DownloadBankData(), "0 0 16 */1 * ?");
                    RecurringJob.AddOrUpdate("定时15", () => DownloadQrCode(), "0 0 15 */1 * ?");
                }
                catch (Exception ex) { logger.Info($"异常:{ex.Message},详细:{ex.StackTrace}"); }

            }
        }

        public void Stop()
        {
            //logger.Info("服务停止");
            Console.WriteLine("服务停止");
        }


        /*
         * 1.从FTP下载对账文件(银行、扫码) FtpService 
         *
         */

        public string Requester(string source, string data, Method method = Method.POST)
        {
            IRestClient restClient = new RestClient(baseApi);
            RestRequest request = new RestRequest(source, method);
            request.AddParameter("hospitalId", 0);
            request.AddParameter("searchDate", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
            IRestResponse response = restClient.Execute(request);
            return response.Content;
        }

        public string DownloadBankData()
        {
            logger.Info($"{DateTime.Now},银行卡");
            string source = "/Admin/FtpService/DownLoadBankCardTradeData";
            string result = Requester(source, "");
            logger.Info(result);
            return "";
        }

        public string DownloadQrCode()
        {
            logger.Info($"{DateTime.Now},扫码");
            string source = "/Admin/FtpService/DownLoadQrCodeTradeData";
            string result = Requester(source, "");
            logger.Info(result);
            return "";
        }

        public string DownloadHisData()
        {
            logger.Info($"{DateTime.Now},HIS");
            string source = "/Admin/TradeStatData/DownLoadHisTradeData";
            string result = Requester(source, "");
            logger.Info(result);
            return "";
        }


    }
}
