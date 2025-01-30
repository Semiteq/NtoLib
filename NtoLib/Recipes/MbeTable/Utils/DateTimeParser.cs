using System.Globalization;

namespace NtoLib.Recipes.MbeTable
{
    public static class DateTimeParser
    {
        public static bool TryParse(string text, out float resultSeconds)
        {
            text = text.TrimEnd(' ', 'c');

            CultureInfo culture = CultureInfo.InvariantCulture;
            if(float.TryParse(text, NumberStyles.Any, culture, out resultSeconds))
                return true;

            resultSeconds = 0;

            var splittedText = text.Split(':');
            int length = splittedText.Length;
            if(length < 1)
                return false;
            if(!float.TryParse(splittedText[length - 1], NumberStyles.Any, culture, out var seconds))
                return false;

            if(length < 2)
            {
                resultSeconds = seconds;
                return true;
            }
            if(!int.TryParse(splittedText[length - 2], out var minutes) || minutes > 60)
                return false;
            resultSeconds += minutes * 60f;

            if(length < 3)
            {
                resultSeconds = seconds + minutes * 60f;
                return true;
            }
            if(!int.TryParse(splittedText[length - 3], out var hours))
                return false;

            resultSeconds = seconds + minutes * 60 + hours * 3600;
            return true;
        }
    }
}
