using Hangfire;
using Hangfire.PostgreSql;
using Owin;
using System;
using System.Configuration;

namespace AutoTrade
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            string postgresqlstr = ConfigurationManager.AppSettings["postgresqlstr"].ToString();
            GlobalConfiguration.Configuration.UsePostgreSqlStorage(postgresqlstr,
                new PostgreSqlStorageOptions { QueuePollInterval = TimeSpan.FromSeconds(2) }); //队列轮询间隔


            app.UseHangfireDashboard();
            app.UseHangfireServer();
        }
    }
}
