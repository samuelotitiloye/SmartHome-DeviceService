using Serilog.Events;
using Serilog.Formatting;
using System.IO;
using System.Text.Json;
using System.Text;

namespace DeviceService.Api.Logging
{
    public class PrettyJsonFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            var dictionary = new Dictionary<string, object?>()
            {
                ["Timestamp"] = logEvent.Timestamp.UtcDateTime,
                ["Level"] = logEvent.Level.ToString(),
                ["Message"] = logEvent.RenderMessage(),
                ["Exception"] = logEvent.Exception?.ToString()
            };

            // add serilog properties (TraceId, SpadId, CorrelationId)
            foreach (var prop in logEvent.Properties)
            {
                dictionary[prop.Key] = Simplify(prop.Value);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(
                dictionary,
                new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

        output.WriteLine(json);
        }
        // turn serilog scalar/structure values to normal JSON friendly objects
        private static object? Simplify(LogEventPropertyValue value)
        {
            return value switch
            {
                ScalarValue scalar => scalar.Value,
                SequenceValue seq => seq.Elements.Select(Simplify).ToArray(),
                StructureValue str => str.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value)),
                _ => value.ToString()
            };
        }
    }
}
