using AutoMapper;
using domain_mapping;
using infra_http.Auth.OAuth;
using infra_http.Auth.OAuth.Contracts;
using infra_http.Client;
using infra_http.Client.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using infra_configuration.Clients;
using infra_http.Middleware.Request;
using infra_http.Middleware.Request.Contracts;
using http_infra.Middleware.Response;
using dal_villa.Context;
using Microsoft.EntityFrameworkCore;
using domain_service.Aggregation;

namespace villa_api
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }


    public void ConfigureServices(IServiceCollection services)
    {

      services.AddControllers();

      services.AddDbContext<VillaContext>(opts =>
      {
        opts.UseNpgsql(Environment.GetEnvironmentVariable("PG_CONN"));
      });


      services.AddAutoMapper(typeof(MappingConfiguration));
      services.AddMemoryCache();
      services.AddScoped<AggregationService>();

      ConfigureTinkServices(services);

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      // if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

      app.UseMiddleware<ExceptionHandler>();

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });


    }

    void ConfigureTinkServices(IServiceCollection services)
    { 

      // TODO move to a client based config @villa-configuration
      services.AddHttpClient<IOAuthClient, TinkOAuthClient>(client =>
      {
        client.BaseAddress = new Uri(@"https://api.tink.com/api/v1/");
      });

      services.AddHttpClient<IAggregationProviderClient, TinkClient>(client =>
      {
        client.BaseAddress = new Uri(@"https://api.tink.com/api/v1/");
      })
      .AddHttpMessageHandler<IExternalOAuthHandler>();

      services.AddTransient<IExternalOAuthHandler, TinkOAuthHandler>();

      services.Configure<TinkSettings>(Configuration.GetSection("TinkSettings"));

    }

  }

}