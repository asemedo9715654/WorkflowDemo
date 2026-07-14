using System.ComponentModel.DataAnnotations;

namespace PedidoWorkflow.Validation;

public sealed class DecimalGreaterThanAttribute : ValidationAttribute
{
    private readonly decimal _minimumValue;

    public DecimalGreaterThanAttribute(string minimumValue)
    {
        if (!decimal.TryParse(minimumValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _minimumValue))
        {
            throw new ArgumentException("Valor minimo invalido.", nameof(minimumValue));
        }
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        return value switch
        {
            decimal decimalValue => decimalValue > _minimumValue,
            _ => false
        };
    }
}
