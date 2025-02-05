using System;
using System.Linq;

public static class PeakQualityEstimator
{
    /// <summary>
    /// Enum representing different methods for estimating peak quality.
    /// </summary>
    public enum PeakQualityMethod
    {
        SignalToNoiseRatio,
        FullWidthAtHalfMaximum,
        Prominence
    }

    /// <summary>
    /// Estimates the quality of a peak in a spectrum.
    /// </summary>
    /// <param name="spectrum">The input spectrum data (array of doubles).</param>
    /// <param name="peakIndex">The index of the peak in the spectrum array.</param>
    /// <param name="method">The method to use for quality estimation (default: SignalToNoiseRatio).</param>
    /// <param name="windowSize">The window size for SNR calculation (number of data points on either side of the peak to consider as noise).</param>
    /// <returns>A double value representing the peak quality. Higher values indicate better quality.</returns>
    /// <exception cref="ArgumentException">Thrown if the spectrum is null or empty, or if an invalid method is specified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the peak index is out of range.</exception>
    public static double EstimatePeakQuality(double[] spectrum, int peakIndex, PeakQualityMethod method = PeakQualityMethod.SignalToNoiseRatio, int windowSize = 10)
    {
        // Input validation
        if (spectrum == null || spectrum.Length == 0)
        {
            throw new ArgumentException("Spectrum cannot be null or empty.");
        }

        if (peakIndex < 0 || peakIndex >= spectrum.Length)
        {
            throw new ArgumentOutOfRangeException("peakIndex", "Peak index is out of range.");
        }

        // Select and execute the chosen method
        switch (method)
        {
            case PeakQualityMethod.SignalToNoiseRatio:
                return CalculateSNR(spectrum, peakIndex, windowSize);
            case PeakQualityMethod.FullWidthAtHalfMaximum:
                return CalculateFWHM(spectrum, peakIndex);
            case PeakQualityMethod.Prominence:
                return CalculateProminence(spectrum, peakIndex);
            default:
                throw new ArgumentException("Invalid peak quality method specified.");
        }
    }

    /// <summary>
    /// Calculates the Signal-to-Noise Ratio (SNR) of a peak.
    /// SNR = Peak Value / RMS Noise
    /// </summary>
    private static double CalculateSNR(double[] spectrum, int peakIndex, int windowSize)
    {
        double peakValue = spectrum[peakIndex];

        // Define the noise window around the peak (excluding the peak itself)
        int startIndex = Math.Max(0, peakIndex - windowSize);
        int endIndex = Math.Min(spectrum.Length - 1, peakIndex + windowSize);


        double[] noiseData = spectrum
            .Skip(startIndex) // Skip elements before the start index
            .Take(endIndex - startIndex + 1) // Take elements within the window
            .Where((val, idx) => startIndex + idx != peakIndex) // Exclude the peak itself
            .ToArray();


        // Handle cases with zero noise to avoid division by zero.  Return a very large SNR in these cases, indicating a "perfect" peak.
        if (noiseData.Length == 0 || noiseData.All(x => x == 0))
        {
            return double.PositiveInfinity;
        }

        // Calculate Root Mean Square (RMS) of the noise
        double noiseRMS = CalculateRMS(noiseData);

        return peakValue / noiseRMS;
    }

    /// <summary>
    /// Calculates the Full Width at Half Maximum (FWHM) of a peak.
    /// FWHM is the width of the peak at half of its maximum value.  A smaller FWHM indicates a sharper peak.
    /// </summary>
    private static double CalculateFWHM(double[] spectrum, int peakIndex)
    {
        double peakValue = spectrum[peakIndex];
        double halfMax = peakValue / 2.0;

        // Find the left and right boundaries of the peak at half maximum
        int leftIndex = peakIndex;
        while (leftIndex > 0 && spectrum[leftIndex - 1] > halfMax)
        {
            leftIndex--;
        }

        int rightIndex = peakIndex;
        while (rightIndex < spectrum.Length - 1 && spectrum[rightIndex + 1] > halfMax)
        {
            rightIndex++;
        }

        // Return the FWHM. 
        return rightIndex - leftIndex + 1;
    }



    /// <summary>
    /// Calculates the prominence of a peak.
    /// Prominence is the vertical distance between the peak and the lowest contour line encircling it but containing no higher peak.  
    /// It's a measure of how much the peak stands out from the surrounding baseline.
    /// </summary>
    private static double CalculateProminence(double[] spectrum, int peakIndex)
    {
        double peakValue = spectrum[peakIndex];

        // Find the minimum values to the left and right of the peak
        double leftMin = spectrum.Take(peakIndex).Min(); // Min value to the left
        double rightMin = spectrum.Skip(peakIndex + 1).Min(); // Min value to the right

        // Calculate prominence as the difference between the peak value and the higher of the two minimums
        double prominence = peakValue - Math.Max(leftMin, rightMin);
        return prominence;
    }


    /// <summary>
    /// Calculates the Root Mean Square (RMS) of an array of data.
    /// </summary>
    private static double CalculateRMS(double[] data)
    {
        double sumOfSquares = data.Sum(x => x * x);
        return Math.Sqrt(sumOfSquares / data.Length);
    }
}

/// For bell-shaped data (presumably a Gaussian or similar distribution) of approximately 50 points, Full Width at Half Maximum (FWHM) 
/// or Prominence are generally good choices, with FWHM often being slightly preferred due to its simplicity and direct interpretability 
/// in this context. Here's a breakdown: FWHM: FWHM directly measures the width of the bell curve at half its maximum height. This is a 
/// very intuitive and meaningful metric for bell-shaped data. It's easy to calculate and understand. Since you have 50 points, you have 
/// enough data for a reasonable FWHM calculation, even if the peak isn't perfectly centered. Prominence: Prominence measures how much 
/// the peak stands out from the surrounding baseline. For well-defined bell curves, where the baseline is low and flat, prominence will 
/// work well. However, if there's any significant noise or other peaks nearby, prominence might be less reliable than FWHM. 
/// SNR (Signal-to-Noise Ratio): SNR is less suitable for this specific scenario.SNR works best when you have a relatively constant noise 
/// floor.With a bell curve, the "noise"(the tails of the distribution) isn't constant; it changes depending on how far you are from the peak. 
/// The windowSize parameter becomes crucial for SNR, and choosing an appropriate value can be tricky. You'd need to fine - tune it based 
/// on the shape and width of your bell curve.
/// In summary: For a clean, well - defined bell curve with approximately 50 points, FWHM is probably the best choice due to its simplicity 
/// and direct relation to the shape of the curve.If there's a chance of noise or other peaks near your bell curve, prominence would be a more 
/// robust alternative. Avoid SNR unless you have a good understanding of the noise characteristics and how to set the windowSize appropriately.