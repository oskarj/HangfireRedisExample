using Hangfire;
using Hangfire.Annotations;
using Hangfire.Pro.Redis;
using Hangfire.Throttling;
using HangfireRedisExample.Hangfire;

namespace HangfireRedisExample.Extensions
{
    public static class HangfireExtensions
    {
        public const string SendEmailSlidingWindowId = "send_email_sliding";

        public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetValue<string>("RedisConnectionString");
            services.AddHangfireRedis(redisConnectionString);

            string[] queues =
            {
                "Default",
                "MySpecialQueue"
            };
            services.AddHangfireServer(x =>
            {
                x.WorkerCount = 1;
                x.Queues = queues;
                x.StopTimeout = TimeSpan.FromSeconds(10);
            });

            return services;
        }

        public static IServiceCollection AddHangfireRedis(this IServiceCollection services, string redisConnectionString)
        {
            var redisStorageOptions = new RedisStorageOptions()
            {
                Prefix = "__Hangfire:example"
            };
            redisConnectionString = $"{redisConnectionString},abortConnect=false,connectTimeout=30000,responseTimeout=30000";
            services.AddHangfire(x =>
                SetupConfiguration(x, gc => gc.UseRedisStorage(redisConnectionString, redisStorageOptions)));

            return services;
        }

        private static void SetupConfiguration(IGlobalConfiguration globalConfiguration, Action<IGlobalConfiguration> gloAction)
        {
            globalConfiguration.UseThrottling();
            globalConfiguration.UseSimpleAssemblyNameTypeSerializer();
            globalConfiguration.UseRecommendedSerializerSettings();
            globalConfiguration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
            gloAction(globalConfiguration);
        }

        public static IEndpointConventionBuilder MapHangfireDashboard(
            [NotNull] this IEndpointRouteBuilder endpoints,
            bool useReleaseModeRoutePrefix)
        {
            var hangfireUrl = useReleaseModeRoutePrefix ? "/siteadmin/hangfire" : "/hangfire";
            var options = new DashboardOptions();
            return endpoints.MapHangfireDashboard(hangfireUrl, options);
        }

        public static IApplicationBuilder UseHangfire(this IApplicationBuilder app)
        {
            GlobalJobFilters.Filters.Add(new LogFailureAttribute(app.ApplicationServices));

            var throttlingManager = new ThrottlingManager(); //Fetch from ServiceProvider??
            throttlingManager.RemoveSlidingWindowIfExists(SendEmailSlidingWindowId);
            //This will send 10 every second. Our current limit in AWS SES is 14 messages/second.
            throttlingManager.AddOrUpdateSlidingWindow(SendEmailSlidingWindowId, new SlidingWindowOptions(
                limit: 10,
                interval: TimeSpan.FromSeconds(1),
                buckets: 1));

            var recurringJobManager = app.ApplicationServices.GetService<IRecurringJobManager>();

            recurringJobManager.AddOrUpdate<ExampleJob>("My example job",
                job => job.Run(), Cron.Daily(23));

            return app;
        }
    }
}
