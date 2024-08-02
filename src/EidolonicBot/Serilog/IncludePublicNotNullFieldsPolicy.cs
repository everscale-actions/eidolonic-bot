using Serilog.Core;
using Serilog.Events;

namespace EidolonicBot.Serilog;

internal class IncludePublicNotNullFieldsPolicy : IDestructuringPolicy {
  public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result) {
    if (!value.GetType().IsClass) {
      result = null!;
      return false;
    }

    var fieldsWithValues = value.GetType()
      .GetProperties(BindingFlags.Instance | BindingFlags.Public)
      .Where(p => p.CanRead)
      .Select(f => new { name = f.Name, value = f.GetValue(value) })
      .Where(v => v.value is not null)
      .Select(f => new LogEventProperty(f.name, propertyValueFactory.CreatePropertyValue(f.value!, true)));

    result = new StructureValue(fieldsWithValues);
    return true;
  }
}
