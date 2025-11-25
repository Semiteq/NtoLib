using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using FB;
using FB.VisualFB;

using InSAT.Library.Interop;
using InSAT.OPC;

using NtoLib.Utils;

namespace NtoLib.Devices.Valves
{
	[Serializable]
	[ComVisible(true)]
	[Guid("0B747EAD-4E9B-47CE-99AA-12BF8F5192A4")]
	[CatID(CatIDs.CATID_OTHER)]
	[DisplayName("Клапан")]
	[VisualControls(typeof(ValveControl))]
	public class ValveFB : VisualFBBaseExtended
	{
		public const int StatusWordId = 1;
		public const int CommandWordId = 5;


		public const int ConnectionOkId = 0;
		public const int NotOpenedId = 1;
		public const int NotClosedId = 2;
		public const int CollisionId = 3;
		public const int UsedByAutoModeId = 4;
		public const int OpenedId = 5;
		public const int SmoothlyOpenedId = 6;
		public const int ClosedId = 7;
		public const int OpeningClosingId = 8;

		public const int ForceCloseId = 12;
		public const int BlockClosingId = 13;
		public const int BlockOpeningId = 14;
		public const int IsSmoothValveId = 15;


		public const int OpenCmdId = 100;
		public const int CloseCmdId = 101;
		public const int OpenSmoothlyCmdId = 102;


		public const int CollistionEventId = 5000;
		private EventTrigger _collisionEvent;

		public const int NotOpenedEventId = 5001;
		private EventTrigger _notOpenedEvent;

		public const int NotClosedEventId = 5002;
		private EventTrigger _notClosedEvent;


		public const int OpenedEventId = 5010;
		private EventTrigger _openedEvent;

		public const int OpenedSmoothlyEventId = 5011;
		private EventTrigger _openedSmoothlyEvent;

		public const int ClosedEventId = 5012;
		private EventTrigger _closedEvent;



		protected override void ToRuntime()
		{
			base.ToRuntime();

			string[] splittedString = FullName.Split('.');
			string name = splittedString[splittedString.Length - 1];

			TimeSpan initialInactivity = TimeSpan.FromSeconds(10);

			string message = $"Коллизия концевиков у {name}";
			_collisionEvent = new EventTrigger(this, CollistionEventId, message, initialInactivity);

			message = $"{name} не открылся";
			_notOpenedEvent = new EventTrigger(this, NotOpenedEventId, message, initialInactivity);

			message = $"{name} не закрылся";
			_notClosedEvent = new EventTrigger(this, NotClosedEventId, message, initialInactivity);

			message = $"{name} открылся";
			_openedEvent = new EventTrigger(this, OpenedEventId, message, initialInactivity, true);

			message = $"{name} плавно открылся";
			_openedSmoothlyEvent = new EventTrigger(this, OpenedSmoothlyEventId, message, initialInactivity, true);

			message = $"{name} закрылся";
			_closedEvent = new EventTrigger(this, ClosedEventId, message, initialInactivity, true);
		}

		protected override void UpdateData()
		{
			base.UpdateData();

			int statusWord = 0;
			if (GetPinQuality(StatusWordId) == OpcQuality.Ok)
				statusWord = GetPinValue<int>(StatusWordId);

			bool connectionOk = statusWord.GetBit(ConnectionOkId);
			SetVisualAndUiPin(ConnectionOkId, connectionOk);
			bool notOpened = statusWord.GetBit(NotOpenedId);
			SetVisualAndUiPin(NotOpenedId, notOpened);
			bool notClosed = statusWord.GetBit(NotClosedId);
			SetVisualAndUiPin(NotClosedId, notClosed);
			bool collision = statusWord.GetBit(CollisionId);
			SetVisualAndUiPin(CollisionId, collision);
			SetVisualAndUiPin(UsedByAutoModeId, statusWord.GetBit(UsedByAutoModeId));
			bool opened = statusWord.GetBit(OpenedId);
			SetVisualAndUiPin(OpenedId, opened);
			bool openedSmoothly = statusWord.GetBit(SmoothlyOpenedId);
			SetVisualAndUiPin(SmoothlyOpenedId, openedSmoothly);
			bool closed = statusWord.GetBit(ClosedId);
			SetVisualAndUiPin(ClosedId, closed);
			SetVisualAndUiPin(OpeningClosingId, statusWord.GetBit(OpeningClosingId));

			SetVisualAndUiPin(ForceCloseId, statusWord.GetBit(ForceCloseId));
			SetVisualAndUiPin(BlockClosingId, statusWord.GetBit(BlockClosingId));
			SetVisualAndUiPin(BlockOpeningId, statusWord.GetBit(BlockOpeningId));
			SetVisualAndUiPin(IsSmoothValveId, statusWord.GetBit(IsSmoothValveId));


			int commandWord = 0;
			commandWord.SetBit(0, GetVisualPin<bool>(OpenCmdId));
			commandWord.SetBit(1, GetVisualPin<bool>(OpenSmoothlyCmdId));
			commandWord.SetBit(2, GetVisualPin<bool>(CloseCmdId));
			SetPinValue(CommandWordId, commandWord);



			_collisionEvent.Update(collision);
			_notOpenedEvent.Update(notOpened);
			_notClosedEvent.Update(notClosed);

			_openedEvent.Update(opened);
			_openedSmoothlyEvent.Update(openedSmoothly);
			_closedEvent.Update(closed);
		}



		protected override OpcQuality GetConnectionQuality()
		{
			return GetPinQuality(StatusWordId);
		}
	}
}
