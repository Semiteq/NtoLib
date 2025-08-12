using System.Linq;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Properties.Definitions
{
    public class TimeDefinition : FloatDefinitionBase
    {
        public override string Units => "с";
        public override float MinValue => 0;
        public override float MaxValue => 86400;
        public override string MinMaxErrorMessage => $"Время должно быть в пределах от {MinValue} до {MaxValue}";

        public override string FormatValue(object value)
        {
            var totalSeconds = (float)value;
            var hours = (int)(totalSeconds / 3600);
            var minutes = (int)((totalSeconds % 3600) / 60);
            var seconds = (int)(totalSeconds % 60);

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        public override (bool Success, object Value ) TryParse(string input)
        {
            var sanitizedInput = new string(input.Trim().Where(c => char.IsDigit(c) || c == ',' || c == ':').ToArray()).Replace(',', '.');

            // Parse time format hh:mm:ss[.ms]
            if (sanitizedInput.Contains(':'))
            {
                var timeResult = TryParseTimeFormat(sanitizedInput);
                if (timeResult.Success)
                    return (true, timeResult.TotalSeconds);
            }
            
            // Parse numeric value (seconds)
            return base.TryParse(input);
        }

        private (bool Success, float TotalSeconds) TryParseTimeFormat(string input)
        {
            var totalSeconds = 0f;

            // Regular expression to match time format hh:mm:ss[.ms]
            var regex = new System.Text.RegularExpressions.Regex(@"^(?<hours>\d{1,2}):(?<minutes>\d{1,2})(:(?<seconds>\d{1,2})(\.(?<milliseconds>\d+))?)?$");
            var match = regex.Match(input);

            if (!match.Success) 
                return (false, 0f);

            // Parse hours and minutes
            if (!int.TryParse(match.Groups["hours"].Value, out var hours)) 
                return (false, 0f);
            
            if (!int.TryParse(match.Groups["minutes"].Value, out var minutes)) 
                return (false, 0f);

            // Parse optional seconds and milliseconds
            var seconds = 0;
            var milliseconds = 0f;

            if (match.Groups["seconds"].Success && !int.TryParse(match.Groups["seconds"].Value, out seconds)) 
                return (false, 0f);
            
            if (match.Groups["milliseconds"].Success && !float.TryParse($"0.{match.Groups["milliseconds"].Value}", out milliseconds)) 
                return (false, 0f);

            totalSeconds = hours * 3600 + minutes * 60 + seconds + milliseconds;
            return (true, totalSeconds);
        }
    }
}