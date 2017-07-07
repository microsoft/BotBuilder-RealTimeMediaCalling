namespace Microsoft.Bot.Builder.RealTimeMediaCalling
{
    public interface IRealTimeMediaLogger
    {
        void LogInformation(string message);

        void LogError(string message);

        void LogWarning(string message);

        void LogInformation(string format, params object[] args);

        void LogWarning(string format, params object[] args);

        void LogError(string format, params object[] args);
    }
}