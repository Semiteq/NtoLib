using System;

namespace NtoLib.Utils
{
	public static class Int32Extension
	{
		public static void SetBit(this ref int word, int index, bool value)
		{
			if (index < 0 || index > 15)
				throw new IndexOutOfRangeException();

			int valueInt = value ? 1 : 0;
			word |= (valueInt << index);
		}

		public static bool GetBit(this int word, int index)
		{
			if (index < 0 || index > 15)
				throw new IndexOutOfRangeException();

			return (word & (1 << index)) != 0;
		}
	}
}
