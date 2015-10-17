using System;

namespace DataTierGenerator
{
	/// <summary>
	/// Provides data for the DatabaseProcessed and TableProcessed events.
	/// </summary>
	public sealed class CountEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the CountEventArgs class.
		/// </summary>
		/// <param name="count">The count to be reported.</param>
		public CountEventArgs(int count)
		{
			this.Count = count;
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count { get; private set; }
	}
}
