using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

using Device.ATR.Devices;
using Device.ATR.Model;
using Device.ATR.Model.Spectrometer;

using FB;

using InSAT.Library.Interop;

namespace NtoLib.Spectrometer;

[Serializable]
[ComVisible(true)]
[Guid("F191FFCB-A4F9-4A50-9922-9E77EA7C4C46")]
[CatID(CatIDs.CATID_OTHER)]
[DisplayName("Спектрометр ATP2000P")]
public class SpectrometerFB : StaticFBBase
{
	private const int ConnectionOKID = 1;

	private const int WaveLengthID = 10;
	private const int DeltaID = 11;

	private const int IntegrationTimeID = 12;
	private const int IntervalID = 13;
	private const int AverageID = 14;

	private const int ComNumberID = 15;

	private const int GetDarkID = 16;

	private const int IntensityID = 20;
	private int _average = 1;
	[NonSerialized] private readonly WavelengthCalibrationCoeff _calibrationCoeff = new();

	private int _comNumber = 0;
	[NonSerialized] private readonly DeviceService _connection = new();

	private bool _connectionOK = false;
	[NonSerialized] private Spectrum _darkSpectrum = new();

	private int _dataSize = 0;

	private int _integrationTime = 10;
	private double _intensity = 1;
	private int _interval = 0;

	[NonSerialized] private Dictionary<double, double> _normalizedSpectrum = new();

	[NonSerialized] private Spectrum _spectrum = new();
	[NonSerialized] private readonly AcquireParameter _spectrumParameter = new();

	private double _waveLength = 670;
	private int _width = 200;

	protected override void UpdateData()
	{
		base.UpdateData();

		SetPinValue(ConnectionOKID, _connectionOK);

		UpdateSpectrumParameters();

		var newComNumber = GetPinValue<int>(ComNumberID);
		if (_comNumber != newComNumber)
		{
			_comNumber = newComNumber;
			_connection.Close();
			_connectionOK = false;
		}

		if (!_connectionOK)
		{
			TryOpenConnection();
		}

		if (GetPinValue<bool>(GetDarkID))
		{
			TryAcquireDarkSpectrum();
			SetPinValue(GetDarkID, false);
		}

		if (_connectionOK)
		{
			TryAcquireSpectrum();
		}

		_waveLength = GetPinValue<int>(WaveLengthID);
		_width = GetPinValue<int>(DeltaID);
	}

	/// <summary>
	/// Обновление параметров получения спектра (время интегрирования, интервал, усреднение).
	/// </summary>
	private void UpdateSpectrumParameters()
	{
		var newIntegrationTime = GetPinValue<int>(IntegrationTimeID);
		var newInterval = GetPinValue<int>(IntervalID);
		var newAverage = GetPinValue<int>(AverageID);

		if (_integrationTime != newIntegrationTime ||
			_interval != newInterval ||
			_average != newAverage)
		{
			_integrationTime = newIntegrationTime;
			_interval = newInterval;
			_average = newAverage;

			_spectrumParameter.IntegrationTime = _integrationTime;
			_spectrumParameter.Interval = _interval;
			_spectrumParameter.Average = _average;
		}
	}

	/// <summary>
	/// Попытка установить соединение со спектрометром.
	/// </summary>
	private void TryOpenConnection()
	{
		try
		{
			// Первое подключение
			// Пытаемся открыть соединение с указанным COM-портом
			_connectionOK = _connection.Open($"COM{_comNumber}").GetAwaiter().GetResult();
			_dataSize = _connection.DeviceInfo.CcdSize;
		}
		catch (Exception ex)
		{
			_connectionOK = false;
			HandleException(ex);
		}
	}

	/// <summary>
	/// Попытка получить темновой спектр.
	/// </summary>
	private void TryAcquireDarkSpectrum()
	{
		try
		{
			_darkSpectrum = _connection.AcquireDark(_spectrumParameter).GetAwaiter().GetResult();
		}
		catch (AlertException ex)
		{
			// Ошибка при получении спектра
			_connectionOK = false;
			HandleException(ex);
		}
		catch (UnauthorizedAccessException ex)
		{
			// COM-порт занят
			_connectionOK = false;
			HandleException(ex);
		}
		catch (Exception ex)
		{
			_connectionOK = false;
			HandleException(ex);
		}
	}

	/// <summary>
	/// Попытка получить спектр.
	/// </summary>
	private void TryAcquireSpectrum()
	{
		try
		{
			_spectrum = _connection.Acquire(_spectrumParameter).GetAwaiter().GetResult();

			if (_spectrum != null)
			{
				var segmentData = GetSegmentData();
				_intensity = segmentData.Average();
				SetPinValue(IntensityID, _intensity);
			}
		}
		catch (AlertException ex)
		{
			// Ошибка при получении спектра
			_connectionOK = false;
			HandleException(ex);
		}
		catch (UnauthorizedAccessException ex)
		{
			// COM-порт занят
			_connectionOK = false;
			HandleException(ex);
		}
		catch (Exception ex)
		{
			_connectionOK = false;
			HandleException(ex);
		}
	}

	/// <summary>
	/// Получает сегмент данных спектра в указанном диапазоне длин волн и вычитает темновой спектр.
	/// </summary>
	/// <returns>Массив значений интенсивности в заданном диапазоне.</returns>
	private double[] GetSegmentData()
	{
		if (_darkSpectrum.Data != null)
		{
			for (var i = 0; i < _dataSize; i++)
			{
				_spectrum.Data[i] -= _darkSpectrum.Data[i];
			}
		}

		// Заполнение словаря с нормализованным спектром
		_normalizedSpectrum.Clear();

		var coeff = _connection.GetWavelengthCalibrationCoeff().GetAwaiter().GetResult();
		_calibrationCoeff.Coeff = coeff.Coeff;

		// Пересчет пикселей в длины волн и создание словаря (длина волны, интенсивность)
		_normalizedSpectrum = _calibrationCoeff.CalcWavelength(_dataSize)
			.Zip(_spectrum.Data, (wave, intensity) => new { wave, intensity })
			.ToDictionary(x => x.wave, x => x.intensity);

		// Поиск ближайшего ключа к заданной длине волны
		var centerWavelength = _normalizedSpectrum.Keys.OrderBy(w => Math.Abs(w - _waveLength)).First();

		//Определение границ спектра
		var minWaveLength = _normalizedSpectrum.Keys.FirstOrDefault();
		var maxWaveLength = _normalizedSpectrum.Keys.LastOrDefault();

		// Определение границ диапазона
		var halfRange = _width / 2.0;
		var leftWavelength = _normalizedSpectrum.Keys.Where(w => w <= centerWavelength - halfRange)
			.DefaultIfEmpty(minWaveLength).Max();
		var rightWavelength = _normalizedSpectrum.Keys.Where(w => w >= centerWavelength + halfRange)
			.DefaultIfEmpty(maxWaveLength).Min();

		// Извлечение сегмента данных
		var segmentData = _normalizedSpectrum
			.Where(kv => kv.Key >= leftWavelength && kv.Key <= rightWavelength)
			.Select(kv => kv.Value)
			.ToArray();

		return segmentData;
	}

	private void HandleException(Exception ex)
	{
		//Debug.Write(ex.ToString());
	}
}
