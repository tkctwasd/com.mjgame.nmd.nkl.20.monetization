namespace A
{
    using System.Collections.Generic;

    public interface ILogEventService
    {
        void Log(LogEventParameter logEventParameter);
    }
    public class LogEventParameter
    {
        public string Name { get; }
        public Dictionary<string, object> Params { get; private set; } = new Dictionary<string, object>();
        public LogEventParameter(string name)
        {
            this.Name = name;
        }

        public static LogEventParameter Create(string name)
        {
            return new LogEventParameter(name);
        }

        public LogEventParameter AddParam(string key, object value)
        {
            if (Params.ContainsKey(key))
            {
                Params[key] = value;
            }
            else
            {
                Params.Add(key, value);
            }
            return this;
        }


    }

}