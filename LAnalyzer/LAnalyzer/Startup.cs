using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(LAnalyzer.Startup))]
namespace LAnalyzer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
