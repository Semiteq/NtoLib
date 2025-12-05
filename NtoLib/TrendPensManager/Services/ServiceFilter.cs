using System;
using System.Collections.Generic;
using System.Globalization;

using NtoLib.TrendPensManager.Entities;

namespace NtoLib.TrendPensManager.Services;

public sealed class ServiceFilter
{
	private static readonly Dictionary<string, ServiceType> _serviceTypeByName =
		new(StringComparer.OrdinalIgnoreCase)
		{
			{ "БКТ", ServiceType.Heaters },
			{ "БП", ServiceType.ChamberHeaters },
			{ "БУЗ", ServiceType.Shutters }
		};

	private static readonly HashSet<string> _vacuumMetersNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Вакуумметры"
	};

	private static readonly HashSet<string> _pyrometerNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Пирометр"
	};

	private static readonly HashSet<string> _turbineNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Турбины"
	};

	private static readonly HashSet<string> _cryoNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Крио"
	};

	private static readonly HashSet<string> _ionNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Ионные"
	};

	private static readonly HashSet<string> _interferometerNames = new(StringComparer.OrdinalIgnoreCase)
	{
		"Интерферометр"
	};

	public ServiceType GetServiceType(string serviceName)
	{
		if (string.IsNullOrWhiteSpace(serviceName))
		{
			return ServiceType.Other;
		}

		if (_serviceTypeByName.TryGetValue(serviceName, out var knownType))
		{
			return knownType;
		}

		return ServiceType.Other;
	}

	public bool IsServiceEnabled(string serviceName, ServiceSelectionOptions options)
	{
		if (options == null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		if (string.IsNullOrWhiteSpace(serviceName))
		{
			return false;
		}

		if (_serviceTypeByName.TryGetValue(serviceName, out var knownType))
		{
			return IsKnownServiceTypeEnabled(knownType, options);
		}

		if (_vacuumMetersNames.Contains(serviceName))
		{
			return options.AddVacuumMeters;
		}

		if (_pyrometerNames.Contains(serviceName))
		{
			return options.AddPyrometer;
		}

		if (_turbineNames.Contains(serviceName))
		{
			return options.AddTurbines;
		}

		if (_cryoNames.Contains(serviceName))
		{
			return options.AddCryo;
		}

		if (_ionNames.Contains(serviceName))
		{
			return options.AddIon;
		}

		if (_interferometerNames.Contains(serviceName))
		{
			return options.AddInterferometer;
		}

		return false;
	}

	private static bool IsKnownServiceTypeEnabled(ServiceType serviceType, ServiceSelectionOptions options)
	{
		switch (serviceType)
		{
			case ServiceType.Heaters:
				return options.AddHeaters;
			case ServiceType.ChamberHeaters:
				return options.AddChamberHeaters;
			case ServiceType.Shutters:
				return options.AddShutters;
			case ServiceType.Other:
				return options.AddVacuumMeters
					   || options.AddPyrometer
					   || options.AddTurbines
					   || options.AddCryo
					   || options.AddIon
					   || options.AddInterferometer;
			default:
				throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, string.Empty);
		}
	}
}
