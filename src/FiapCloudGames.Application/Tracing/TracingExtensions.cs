using System.Diagnostics;

namespace FiapCloudGames.Application.Tracing
{
    public static class TracingExtensions
    {
        public static Activity? StartApiActivity(this object source, string operationName)
        {
            var activitySourceName = source.GetType().FullName ?? "UnknownSource";
            var activitySource = new ActivitySource(activitySourceName);
            return activitySource.StartActivity(operationName);
        }
    }
}
