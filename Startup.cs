using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Polly;
using rnd001.Controller;
using rnd001.Customization;

namespace rnd001;

[ExcludeFromCodeCoverage]
public class Startup
{
  public IConfiguration Configuration { get; private set; }

  public ILifetimeScope AutofacContainer { get; private set; }
  private readonly IWebHostEnvironment _hostEnvironment;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  public Startup(IWebHostEnvironment hostEnvironment, IConfiguration configuration)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  {
    this._hostEnvironment = hostEnvironment;
    // In ASP.NET Core 3.x, using `Host.CreateDefaultBuilder` (as in the preceding Program.cs snippet) will
    // set up some configuration for you based on your appsettings.json and environment variables. See "Remarks" at
    // https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.host.createdefaultbuilder for details.
    this.Configuration = configuration;
  }

  // ConfigureServices is where you register dependencies. This gets
  // called by the runtime before the ConfigureContainer method, below.
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddMvc();

    services.AddMvc().AddControllersAsServices();
    // Add services to the collection. Don't build or return
    // any IServiceProvider or the ConfigureContainer method
    // won't get called. Don't create a ContainerBuilder
    // for Autofac here, and don't call builder.Populate() - that
    // happens in the AutofacServiceProviderFactory for you.
    services.AddSwaggerGen();
    services.AddOptions();
  }

  // ConfigureContainer is where you can register things directly
  // with Autofac. This runs after ConfigureServices so the things
  // here will override registrations made in ConfigureServices.
  // Don't build the container; that gets done for you by the factory.
  public void ConfigureContainer(ContainerBuilder builder)
  {
    // builder.RegisterType<RetryInterceptor>();
    // Register types and interceptors for classes with Retryable attribute

    // builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
    //        .Where(t => t.GetCustomAttribute<RetryableAttribute>() != null)
    //        .AsSelf()
    //        .EnableClassInterceptors()
    //        .InterceptedBy(typeof(RetryInterceptor));

    // builder.RegisterType<RandomGreetingService>()
    //        .As<IGreetingService>()
    //        .EnableInterfaceInterceptors()
    //        .InterceptedBy(typeof(RetryInterceptor));

      // builder.RegisterType<RandomGreetingService>().As<IGreetingService>();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
           .Where(t => t.GetCustomAttribute<RetryableAttribute>() != null)
           .As<IGreetingService>()
           .EnableInterfaceInterceptors();
          //  .InterceptedBy(typeof(RetryInterceptor));

    builder.RegisterType<RetryInterceptor>();

    builder.RegisterType<RetryService>().As<IRetryService>();
    // This will all go in the ROOT CONTAINER and is NOT TENANT SPECIFIC.
    // builder.RegisterType<GreetingService>().As<IGreetingService>();
    // builder.RegisterType<RandomGreetingService>().As<IGreetingService>();

  //  builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
  //          .Where(t => t.IsClass && t.GetCustomAttribute<RetryableAttribute>() != null)
  //         //  .As( t=> t.GetInterface(t.FullName))
  //         //  .AsSelf()
  //         // .AsClosedTypesOf
  //         // .As(candidateType =>
  //         //           candidateType.GetInterfaces()
  //         //               .Where(i => i.IsClosedTypeOf(openGenericServiceType))
  //         //               .Select(t => (Service)new TypedService(t)))
  //         //  .EnableInterfaceInterceptors();
  //          .EnableClassInterceptors();
  //         //  .InterceptedBy(typeof(RetryInterceptor));

    // builder.RegisterType<RandomGreetingService>()
    //               .AsSelf()
    //               //  .As<IGreetingService>()
    //            .EnableClassInterceptors()
    //           //  .EnableInterfaceInterceptors()
    //            .InterceptedBy(typeof(RetryInterceptor));


  // builder.RegisterType<RetryInterceptor>()
  //      .Named<IInterceptor>(RetryableAttribute.RETRYABLE);


    builder.Register((s) =>
                {
                  return Policy
                  .Handle<Exception>()
                  .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
                })
                .As<IAsyncPolicy>()
                .SingleInstance();

    builder.Register((s) =>
                {
                  return Policy
                  .Handle<Exception>()
                  .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
                })
                .As<ISyncPolicy>()
                .SingleInstance();

    // Register your own things directly with Autofac here. Don't
    // call builder.Populate(), that happens in AutofacServiceProviderFactory
    // for you.
    // builder.RegisterModule(new MyApplicationModule());
  }

  // Configure is where you add middleware. This is called after
  // ConfigureContainer. You can use IApplicationBuilder.ApplicationServices
  // here if you need to resolve things from the container.
  public void Configure(
    IApplicationBuilder app,
    ILoggerFactory loggerFactory)
  {
    // If, for some reason, you need a reference to the built container, you
    // can use the convenience extension method GetAutofacRoot.
    this.AutofacContainer = app.ApplicationServices.GetAutofacRoot();

    // loggerFactory.AddConsole(Configuration.GetSection("Logging"));
    // loggerFactory.AddDebug();
    // app.UseMvc();

    app
    .UseRouting()
    .UseSwagger()
    .UseSwaggerUI()
    .UseEndpoints(ep => { _ = ep.MapControllers(); });
  }
}