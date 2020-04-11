using Microsoft.Owin;
using Owin;
using Serilog;

[assembly: OwinStartupAttribute(typeof(BabyStore.Startup))]
namespace BabyStore
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/log.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

            ConfigureAuth(app);  
        }
    }
}
