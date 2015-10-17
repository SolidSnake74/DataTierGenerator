using System;
using System.Collections.Generic;

namespace DataTierGenerator
{
	/// <summary>
	/// Class that stores information for tables in a database.
	/// </summary>
	public class Table
    {
		/// <summary>
		/// Default constructor; initializes all collections.
		/// </summary>
		public Table()
        {
			this.Columns = new List<Column>();
			this.PrimaryKeys = new List<Column>();
			this.ForeignKeys = new Dictionary<string, List<Column>>();
		}
		
		/// <summary>
		/// Contains the list of Column instances that define the table.
		/// </summary>
		public List<Column> Columns { get; private set; }

		/// <summary>
		/// Contains the list of Column instances that define the table.  The Dictionary returned 
		/// is keyed on the foreign key name, and the value associated with the key is an 
		/// List of Column instances that compose the foreign key.
		/// </summary>
		public Dictionary<string, List<Column>> ForeignKeys { get; private set; }

		/// <summary>
		/// Name of the table.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Contains the list of primary key Column instances that define the table.
		/// </summary>
		public List<Column> PrimaryKeys { get; private set; }
	}
}
