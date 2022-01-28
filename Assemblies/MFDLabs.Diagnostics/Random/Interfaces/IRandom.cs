namespace MFDLabs.Diagnostics
{
	public interface IRandom
	{
		int Next();
		int Next(int maxValue);
		int Next(int minValue, int maxValue);
		double NextDouble();
		void NextBytes(byte[] buffer);
		long NextLong();
		long NextLong(long maxValue);
		long NextLong(long minValue, long maxValue);
	}
}
