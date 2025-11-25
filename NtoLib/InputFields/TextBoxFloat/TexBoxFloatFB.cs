using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;

namespace NtoLib.InputFields.TextBoxFloat
{
	[Serializable]
	[ComVisible(true)]
	[Guid("47B598E0-BE96-4E09-BEE6-CFD835BE4B9A")]
	[CatID(CatIDs.CATID_OTHER)]
	[DisplayName("Дробное поле")]
	[VisualControls(typeof(TextBoxFloatControl))]
	public class TextBoxFloatFB : VisualFBBase
	{
		private const int InputFromScadaId = 10;
		private const int OutputToScadaId = 50;

		private const int LockFromScadaId = 15;

		private const int MaxValueId = 20;
		private const int MinValueId = 25;

		public const int OutputToControlId = 110;
		public const int InputFromControlId = 150;

		public const int LockToControl = 115;

		public const int MaxValueToControlId = 120;
		public const int MinValueToControlId = 125;

		private float _lastInput;
		private float _lastOutput;



		protected override void ToRuntime()
		{
			base.ToRuntime();

			int input = GetPinValue<int>(InputFromScadaId);
			VisualPins.SetPinValue(OutputToControlId, input);

			SetPinValue(OutputToScadaId, input);
		}

		protected override void UpdateData()
		{
			base.UpdateData();

			float input = GetPinValue<float>(InputFromScadaId);
			bool inputChanged = false;
			if (input != _lastInput)
			{
				_lastInput = input;
				inputChanged = true;

				VisualPins.SetPinValue(OutputToControlId, input);
			}

			float output = VisualPins.GetPinValue<float>(InputFromControlId);
			bool outputChanged = false;
			if (output != _lastOutput)
			{
				_lastOutput = output;
				outputChanged = true;
			}

			if (inputChanged)
				SetPinValue(OutputToScadaId, input);
			else if (outputChanged)
				SetPinValue(OutputToScadaId, output);

			bool locked = GetPinValue<bool>(LockFromScadaId);
			VisualPins.SetPinValue(LockToControl, locked);

			float max = GetPinValue<float>(MaxValueId);
			VisualPins.SetPinValue(MaxValueToControlId, max);

			float min = GetPinValue<float>(MinValueId);
			VisualPins.SetPinValue(MinValueToControlId, min);
		}
	}
}
