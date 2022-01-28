using System;

namespace MFDLabs.Diagnostics
{
	public static class PercentageInvoker
	{
		public static void InvokeAction(Action action, int invokePercentage)
		{
			if (invokePercentage < 0 || invokePercentage > 100)
				throw new ArgumentOutOfRangeException("invokePercentage must be between 0 and 100.");
			if (_rng.Next() % 100 < invokePercentage) action();
		}

		private static readonly IRandom _rng = RandomFactory.GetDefaultRandom();
	}
}
