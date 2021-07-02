using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

namespace DrakesBasketballCourtServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<DataBaseHandler>();
            services.AddSignalR();
            //services.Configure<ForwardedHeadersOptions>(options =>
            //{
            //    // options.KnownProxies.Add(IPAddress.Parse("192.168.0.156"));
            //    options.KnownProxies.Add(IPAddress.Parse("0.0.0.0"));
            //});
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            //app.UseForwardedHeaders(new ForwardedHeadersOptions
            //{
            //    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            //});
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<MainHub>("/mainhub");
            });

            //app.UseFileServer();
            //StaticFileOptions option = new StaticFileOptions();
            //FileExtensionContentTypeProvider contentTypeProvider = (FileExtensionContentTypeProvider)option.ContentTypeProvider ??
            //new FileExtensionContentTypeProvider();
            //contentTypeProvider.Mappings.Add(".unityweb", "application/octet-stream");
            //option.ContentTypeProvider = contentTypeProvider;
            //app.UseStaticFiles(option);
        }
    }
}