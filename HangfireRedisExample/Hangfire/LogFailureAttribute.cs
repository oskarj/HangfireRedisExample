using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace HangfireRedisExample.Hangfire
{
    public class LogFailureAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private readonly IServiceProvider _serviceProvider;
        public LogFailureAttribute(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private ILogger Logger
        {
            get
            {
                using var scope = _serviceProvider.CreateScope();
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger<LogFailureAttribute>();
            }
        }
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState is FailedState failedState)
            {
                if (failedState.Exception is AggregateException aggregateException)
                {
                    var flatException = aggregateException.Flatten();
                    foreach (var aggregateExceptionInnerException in flatException.InnerExceptions)
                    {
                        Logger?.LogError(
                            $"Background job #{context.BackgroundJob.Id} was failed with an exception. Message: '{aggregateExceptionInnerException.Message}'",
                            aggregateExceptionInnerException);
                    }
                }
                else
                {
                    Logger?.LogError(
                        $"Background job #{context.BackgroundJob.Id} was failed with an exception. Message: '{failedState.Exception?.Message}'",
                        failedState.Exception);
                }
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            if (context.NewState is SucceededState state)
            {
                Logger?.LogInformation("Background job #{0} successfully executed", context.BackgroundJob.Id);
            }
        }
    }
}
