using System;
using System.Threading;

namespace SafetySystem
{
	public class Worker
	{
		private bool _cancelled = false;

		public int refresh_time = 3000;

		public void Cancel()
		{
			_cancelled = true;
		}

		public void Work()
		{
			while (true)
			{
				if (_cancelled)
					break;
				Thread.Sleep(refresh_time);
				ProcessChanged();
			}
			WorkCompleted(_cancelled);
		}

		public event Action ProcessChanged;

		public event Action<bool> WorkCompleted;
	}
}