using System;

namespace Diagnostics
{
	public static class PercentageInvoker
	{
		public static bool InvokeAction(Action action, int invokePercentage)
		{
			if (invokePercentage < 0 || invokePercentage > 100)
				throw new ArgumentOutOfRangeException(nameof(invokePercentage), "invokePercentage must be between 0 and 100.");
			if (_rng.Next() % 100 < invokePercentage)
            {
				action();
				return true;
			}

			return false;
		}

        public static bool CanInvoke(int invokePercentage)
        {
            if (invokePercentage < 0 || invokePercentage > 100)
                throw new ArgumentOutOfRangeException(nameof(invokePercentage), "invokePercentage must be between 0 and 100.");

            return _rng.Next() % 100 < invokePercentage;
        }

        private static readonly IRandom _rng = RandomFactory.GetDefaultRandom();
	}
}
