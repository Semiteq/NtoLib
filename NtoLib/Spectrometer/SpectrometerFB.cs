using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;

using FB;
using FB.VisualFB;
using InSAT.Library.Interop;
using InSAT.OPC;
using InSAT.Library.Types;
using InSAT.Library.Persistence;

using Device.ATR.Devices;
using Device.ATR.Common.Utils;
using Device.ATR.Model.Spectrometer;
using MasterSCADALib;
using NtoLib.Devices.Pumps;
using System.Linq;

namespace NtoLib.Spectrometer
{
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
        private const int SnID = 16;
        private const int PnID = 17;

        private const int IntensityID = 20;

        private int _integrationTime = 10;
        private int _interval = 0;
        private int _average = 1;

        private int _comNumber = 0;
        
        private int _waveLength = 670;
        private double _intensity = 1;
        private int _width = 100;
        
        private bool _connectionOK = false;

        private const int _minWaveLength = 186;
        private const int _maxWaveLength = 1025;

        [NonSerialized] private Spectrum _spectrum = new();
        [NonSerialized] private Spectrum _darkSpectrum = new();
        [NonSerialized] private DeviceService _connection = new();
        [NonSerialized] private AcquireParameter _spectrumParameter = new();

        protected override void UpdateData()
        {

            base.UpdateData();

            SetPinValue(ConnectionOKID, _connectionOK);

            UpdateSpectrumParameters();

            int newComNumber = GetPinValue<int>(ComNumberID);
            if (_comNumber != newComNumber)
            {
                _comNumber = newComNumber;
                TryCloseConnection();
            }

            TryOpenConnection();

            if (_connectionOK)
                TryUpdatePNSN();
            
            _waveLength = UpdatePinValue(WaveLengthID, _waveLength);
            _intensity = UpdatePinValue(IntensityID, _intensity);
            _width = UpdatePinValue(DeltaID, _width);

            TryAcquireSpectrum();
        }

        private void UpdateSpectrumParameters()
        {
            int newIntegrationTime = GetPinValue<int>(IntegrationTimeID);
            int newInterval = GetPinValue<int>(IntervalID);
            int newAverage = GetPinValue<int>(AverageID);

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

        private void TryCloseConnection()
        {
            try
            {
                _connection.Close();
            }
            catch
            {
                // Игнорируем ошибку при первом подключении
            }
        }

        private void TryOpenConnection()
        {
            try
            {
                _connectionOK = _connection.Open($"COM{_comNumber}").GetAwaiter().GetResult();
            }
            catch (Device.ATR.Model.AlertException ex)
            {
                _connectionOK = false;
                HandleException(ex);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void TryAcquireSpectrum()
        {
            try
            {
                _darkSpectrum = _connection.AcquireDark(_spectrumParameter).GetAwaiter().GetResult();
                _spectrum = _connection.Acquire(_spectrumParameter).GetAwaiter().GetResult();
                
                _spectrum = SubtractDarkSpectrum(_spectrum, _darkSpectrum);
                if (_spectrum != null)
                {
                    double[] segmentData = GetSegmentData();
                    _intensity = PeakQualityEstimator.EstimatePeakQuality(segmentData, segmentData.Length/2, PeakQualityEstimator.PeakQualityMethod.SignalToNoiseRatio, 10);
                    
                    //_intensity = segmentData.Average();
                    SetPinValue(IntensityID, _intensity);
                }
            }
            catch (Device.ATR.Model.AlertException ex)
            {
                _connectionOK = false;
                HandleException(ex);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void TryUpdatePNSN()
        {
            try
            {
                SetPinValue(SnID, _connection.GetSN().GetAwaiter().GetResult());
                SetPinValue(PnID, _connection.GetPN().GetAwaiter().GetResult());
            }
            catch (Device.ATR.Model.AlertException ex)
            {
                _connectionOK = false;
                HandleException(ex);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private double[] GetSegmentData()
        {
            if (_waveLength < _minWaveLength)
                _waveLength = _minWaveLength;

            if (_waveLength > _maxWaveLength)
                _waveLength = _maxWaveLength;

            int arrayLength = _spectrum.Data.Length;
            double range = _maxWaveLength - _minWaveLength;

            int centerIndex = (int)Math.Round(arrayLength * (_waveLength - _minWaveLength) / range);

            int leftIndex = centerIndex - (int)Math.Round((double)(range / arrayLength * _width / 2), 0);

            if (leftIndex < 0)
                leftIndex = 0;

            int rightIndex = centerIndex + (int)Math.Round((double)(range / arrayLength * _width / 2), 0);

            if (rightIndex > arrayLength)
                rightIndex = arrayLength;

            double[] segmentData = new double[rightIndex - leftIndex];
            Array.Copy(_spectrum.Data, leftIndex, segmentData, 0, rightIndex - leftIndex);

            segmentData = segmentData.Select(x => (x - segmentData.Min())).ToArray();

            return segmentData;
        }

        private T UpdatePinValue<T>(int pinID, T currentValue)
        {
            T newValue = GetPinValue<T>(pinID);
            if (!EqualityComparer<T>.Default.Equals(newValue, currentValue))
            {
                return newValue;
            }
            return currentValue;
        }

        private void HandleException(Exception ex)
        {
            Debug.Write(ex.ToString());
        }

        private Spectrum SubtractDarkSpectrum(Spectrum spectrum, Spectrum darkSpectrum)
        {
            double[] resultData = new double[spectrum.Data.Length];
            for (int i = 0; i < spectrum.Data.Length; i++)
            {
                resultData[i] = spectrum.Data[i] - darkSpectrum.Data[i];
            }

            return new Spectrum
            {
                Parameter = spectrum.Parameter,
                Data = resultData,
                Dark = darkSpectrum.Data
            };
        }
        
    }
}
