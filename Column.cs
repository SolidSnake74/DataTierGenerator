using System;

namespace DataTierGenerator
{
	/// <summary>
	/// Class that stores information for columns in a database table.
	/// </summary>
	public class Column
	{
		/// <summary>
		/// Name of the column.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Data type of the column.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Length in bytes of the column.
		/// </summary>
		public string Length { get; set; }

		/// <summary>
		/// Precision of the column.  Applicable to decimal, float, and numeric data types only.
		/// </summary>
		public string Precision { get; set; }

		/// <summary>
		/// Scale of the column.  Applicable to decimal, and numeric data types only.
		/// </summary>
		public string Scale { get; set; }

		/// <summary>
		/// Flags the column as a uniqueidentifier column.
		/// </summary>
		public bool IsRowGuidCol { get; set; }

		/// <summary>
		/// Flags the column as an identity column.
		/// </summary>
		public bool IsIdentity { get; set; }

		/// <summary>
		/// Flags the column as being computed.
		/// </summary>
		public bool IsComputed { get; set; }
	}
}
