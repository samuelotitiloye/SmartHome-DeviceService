using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace DeviceService.Api.Logging
{
    public class PrettyConsoleJsonFormatter : ITextFormatter
    {
        private readonly JsonFormatter _inner = new JsonFormatter(renderMessage: true);

        public void Format(LogEvent logEvent, TextWriter output)
        {
            using var sw = new StringWriter();
            _inner.Format(logEvent, sw);

            var raw = sw.ToString();

            var parsed = JObject.Parse(raw);
            var pretty = parsed.ToString(Formatting.Indented);

            output.WriteLine(pretty);
        }
    }
}
