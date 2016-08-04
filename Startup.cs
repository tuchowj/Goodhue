using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Goodhue.Startup))]
namespace Goodhue
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
