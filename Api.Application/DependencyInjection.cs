using System.Reflection;
 using Api.Application.Common.Behaviors;
 using Microsoft.Extensions.DependencyInjection;
 using Microsoft.Extensions.Hosting;
 
 namespace Api.Application;
 
 public static class DependencyInjection
 {
     public static void AddApplicationServices(this IHostApplicationBuilder builder)
     {
         builder.Services.AddAutoMapper(cfg => 
             cfg.AddMaps(Assembly.GetExecutingAssembly()));
 
         builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
 
         builder.Services.AddMediatR(cfg => {
             cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
             cfg.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));
             cfg.AddOpenBehavior(typeof(AuthorizationBehaviour<,>));
             cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
         });
     }
 }