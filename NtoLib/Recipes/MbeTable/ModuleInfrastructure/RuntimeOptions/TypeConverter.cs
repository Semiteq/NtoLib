using System;
using System.ComponentModel;
using System.Globalization;

namespace NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;

public class WordOrderConverter : TypeConverter
{
	public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
	{
		return true;
	}

	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
	{
		return true;
	}

	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
	{
		return new StandardValuesCollection(new[]
		{
			WordOrder.LowHigh,
			WordOrder.HighLow
		});
	}

	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	}

	public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
	{
		if (value is string stringValue)
		{
			return Enum.Parse(typeof(WordOrder), stringValue);
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
		Type destinationType)
	{
		if (destinationType == typeof(string) && value is WordOrder)
		{
			return value.ToString();
		}

		return base.ConvertTo(context, culture, value, destinationType);
	}
}
