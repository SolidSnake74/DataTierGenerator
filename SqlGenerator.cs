using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataTierGenerator
{
	/// <summary>
	/// Generates SQL Server stored procedures for a database.
	/// </summary>
	internal static class SqlGenerator
	{
        /// <summary>
        /// Creates the "use [database]" statement in a specified file.
        /// </summary>
		/// <param name="databaseName">The name of the database that the login will be created for.</param>
		/// <param name="path">Path where the "use [database]" statement should be created.</param>
		/// <param name="createMultipleFiles">Indicates if the script should be created in its own file.</param>
        public static void CreateUseDatabaseStatement(string databaseName, string path, bool createMultipleFiles)
        {
            if (!createMultipleFiles)
            {
				string fileName = Path.Combine(path, "StoredProcedures.sql");
		        using (StreamWriter streamWriter = new StreamWriter(fileName, true))
		        {
                    streamWriter.WriteLine("use [{0}]", databaseName);
                    streamWriter.WriteLine("go");
                }
            }
        }
        /// <summary>
        /// Writes the "use [database]" statement to the specified stream.
        /// </summary>
		/// <param name="databaseName">The name of the database that the login will be created for.</param>
		/// <param name="streamWriter">StreamWriter to write the script to.</param>
        public static void CreateUseDatabaseStatement(string databaseName, StreamWriter streamWriter)
        {
            streamWriter.WriteLine("use [{0}]", databaseName);
            streamWriter.WriteLine("go");
        }

		/// <summary>
		/// Creates the SQL script that is responsible for granting the specified login access to the specified database.
		/// </summary>
		/// <param name="databaseName">The name of the database that the login will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="path">Path where the script should be created.</param>
		/// <param name="createMultipleFiles">Indicates if the script should be created in its own file.</param>
		public static void CreateUserQueries(string databaseName, string grantLoginName, string path, bool createMultipleFiles)
		{
			if (grantLoginName.Length > 0)
			{
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, "GrantUserPermissions.sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
					streamWriter.Write(Utility.GetUserQueries(databaseName, grantLoginName));
				}
			}
		}

		/// <summary>
		/// Creates an insert stored procedure SQL script for the specified table
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateInsertStoredProcedure(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			// Create the stored procedure name
			string procedureName = storedProcedurePrefix + table.Name + "Insert";
			string fileName;

			// Determine the file name to be used
			if (createMultipleFiles)
			{
				fileName = Path.Combine(path, procedureName + ".sql");
			}
			else
			{
				fileName = Path.Combine(path, "StoredProcedures.sql");
			}

			using (StreamWriter streamWriter = new StreamWriter(fileName, true))
			{
				// Create the "use" statement or the seperator
				if (createMultipleFiles)
				{
                    CreateUseDatabaseStatement(databaseName, streamWriter);
                }
                else
                {
					streamWriter.WriteLine();
					streamWriter.WriteLine("/******************************************************************************");
					streamWriter.WriteLine("******************************************************************************/");
				}

				// Create the drop statment
				streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
				streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
				streamWriter.WriteLine("go");
				streamWriter.WriteLine();

				// Create the SQL for the stored procedure
				streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
				streamWriter.WriteLine("(");

				// Create the parameter list
				for (int i = 0; i < table.Columns.Count; i++)
				{
					Column column = table.Columns[i];
					if (column.IsIdentity == false && column.IsRowGuidCol == false)
					{
						streamWriter.Write("\t" + Utility.CreateParameterString(column, true));
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.Write(",");
						}
						streamWriter.WriteLine();
					}
				}
				streamWriter.WriteLine(")");

				streamWriter.WriteLine();
				streamWriter.WriteLine("as");
				streamWriter.WriteLine();
				streamWriter.WriteLine("set nocount on");
				streamWriter.WriteLine();

				// Initialize all RowGuidCol columns
				foreach (Column column in table.Columns)
				{
					if (column.IsRowGuidCol)
					{
						streamWriter.WriteLine("set @" + column.Name + " = NewID()");
						streamWriter.WriteLine();
						break;
					}
				}

				streamWriter.WriteLine("insert into [" + table.Name + "]");
				streamWriter.WriteLine("(");

				// Create the parameter list
				for (int i = 0; i < table.Columns.Count; i++)
				{
					Column column = table.Columns[i];

					// Ignore any identity columns
					if (column.IsIdentity == false)
					{
						// Append the column name as a parameter of the insert statement
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.WriteLine("\t[" + column.Name + "],");
						}
						else
						{
							streamWriter.WriteLine("\t[" + column.Name + "]");
						}
					}
				}

				streamWriter.WriteLine(")");
				streamWriter.WriteLine("values");
				streamWriter.WriteLine("(");

				// Create the values list
				for (int i = 0; i < table.Columns.Count; i++)
				{
					Column column = table.Columns[i];

					// Is the current column an identity column?
					if (column.IsIdentity == false)
					{
						// Append the necessary line breaks and commas
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.WriteLine("\t@" + column.Name + ",");
						}
						else
						{
							streamWriter.WriteLine("\t@" + column.Name);
						}
					}
				}

				streamWriter.WriteLine(")");

				// Should we include a line for returning the identity?
				foreach (Column column in table.Columns)
				{
					// Is the current column an identity column?
					if (column.IsIdentity)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("select scope_identity()");
						break;
					}
					else if (column.IsRowGuidCol)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("Select @" + column.Name);
						break;
					}
				}

				streamWriter.WriteLine("go");

				// Create the grant statement, if a user was specified
				if (grantLoginName.Length > 0)
				{
					streamWriter.WriteLine();
					streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
					streamWriter.WriteLine("go");
				}
			}
		}

		/// <summary>
		/// Creates an update stored procedure SQL script for the specified table
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateUpdateStoredProcedure(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			if (table.PrimaryKeys.Count > 0 && table.Columns.Count != table.PrimaryKeys.Count && table.Columns.Count != table.ForeignKeys.Count)
			{
				// Create the stored procedure name
				string procedureName = storedProcedurePrefix + table.Name + "Update";
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, procedureName + ".sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
				    // Create the "use" statement or the seperator
				    if (createMultipleFiles)
				    {
                        CreateUseDatabaseStatement(databaseName, streamWriter);
                    }
                    else
                    {
						streamWriter.WriteLine();
						streamWriter.WriteLine("/******************************************************************************");
						streamWriter.WriteLine("******************************************************************************/");
					}

					// Create the drop statment
					streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
					streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("go");
					streamWriter.WriteLine();

					// Create the SQL for the stored procedure
					streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("(");

					// Create the parameter list
					for (int i = 0; i < table.Columns.Count; i++)
					{
						Column column = table.Columns[i];
						
						if (i == 0)
						{
						
						}
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false) + ",");
						}
						else
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false));
						}
					}
					streamWriter.WriteLine(")");

					streamWriter.WriteLine();
					streamWriter.WriteLine("as");
					streamWriter.WriteLine();
					streamWriter.WriteLine("set nocount on");
					streamWriter.WriteLine();
					streamWriter.WriteLine("update [" + table.Name + "]");
					streamWriter.Write("set");

					// Create the set statement
					bool firstLine = true;
					for (int i = 0; i < table.Columns.Count; i++)
					{
						Column column = (Column) table.Columns[i];

						// Ignore Identity and RowGuidCol columns
						if (table.PrimaryKeys.Contains(column) == false)
						{
							if (firstLine)
							{
								streamWriter.Write(" ");
								firstLine = false;
							}
							else
							{
								streamWriter.Write("\t");
							}

							streamWriter.Write("[" + column.Name + "] = @" + column.Name);

							if (i < (table.Columns.Count - 1))
							{
								streamWriter.Write(",");
							}
							
							streamWriter.WriteLine();
						}
					}

					streamWriter.Write("where");

					// Create the where clause
					for (int i = 0; i < table.PrimaryKeys.Count; i++)
					{
						Column column = table.PrimaryKeys[i];

						if (i == 0)
						{
							streamWriter.Write(" [" + column.Name + "] = @" + column.Name);
						}
						else
						{
							streamWriter.Write("\tand [" + column.Name + "] = @" + column.Name);
						}
					}
					streamWriter.WriteLine();

					streamWriter.WriteLine("go");

					// Create the grant statement, if a user was specified
					if (grantLoginName.Length > 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
						streamWriter.WriteLine("go");
					}
				}
			}
		}

		/// <summary>
		/// Creates an delete stored procedure SQL script for the specified table
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateDeleteStoredProcedure(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			if (table.PrimaryKeys.Count > 0)
			{
				// Create the stored procedure name
				string procedureName = storedProcedurePrefix + table.Name + "Delete";
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, procedureName + ".sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
				    // Create the "use" statement or the seperator
				    if (createMultipleFiles)
				    {
                        CreateUseDatabaseStatement(databaseName, streamWriter);
                    }
                    else
                    {
						streamWriter.WriteLine();
						streamWriter.WriteLine("/******************************************************************************");
						streamWriter.WriteLine("******************************************************************************/");
					}

					// Create the drop statment
					streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
					streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("go");
					streamWriter.WriteLine();

					// Create the SQL for the stored procedure
					streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("(");

					// Create the parameter list
					for (int i = 0; i < table.PrimaryKeys.Count; i++)
					{
						Column column = table.PrimaryKeys[i];

						if (i < (table.PrimaryKeys.Count - 1))
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false) + ",");
						}
						else
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false));
						}
					}
					streamWriter.WriteLine(")");

					streamWriter.WriteLine();
					streamWriter.WriteLine("as");
					streamWriter.WriteLine();
					streamWriter.WriteLine("set nocount on");
					streamWriter.WriteLine();
					streamWriter.WriteLine("delete from [" + table.Name + "]");
					streamWriter.Write("where");

					// Create the where clause
					for (int i = 0; i < table.PrimaryKeys.Count; i++)
					{
						Column column = table.PrimaryKeys[i];

						if (i == 0)
						{
							streamWriter.WriteLine(" [" + column.Name + "] = @" + column.Name);
						}
						else
						{
							streamWriter.WriteLine("\tand [" + column.Name + "] = @" + column.Name);
						}
					}

					streamWriter.WriteLine("go");

					// Create the grant statement, if a user was specified
					if (grantLoginName.Length > 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
						streamWriter.WriteLine("go");
					}
				}
			}
		}

		/// <summary>
		/// Creates one or more delete stored procedures SQL script for the specified table and its foreign keys
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateDeleteAllByStoredProcedures(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			// Create a stored procedure for each foreign key
			foreach (List<Column> compositeKeyList in table.ForeignKeys.Values)
			{
				// Create the stored procedure name
				StringBuilder stringBuilder = new StringBuilder(255);
				stringBuilder.Append(storedProcedurePrefix + table.Name + "DeleteAllBy");

				// Create the parameter list
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					if (i > 0)
					{
						stringBuilder.Append("_" + Utility.FormatPascal(column.Name));
					}
					else
					{
						stringBuilder.Append(Utility.FormatPascal(column.Name));
					}
				}

				string procedureName = stringBuilder.ToString();
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, procedureName + ".sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
				    // Create the "use" statement or the seperator
				    if (createMultipleFiles)
				    {
                        CreateUseDatabaseStatement(databaseName, streamWriter);
                    }
                    else
                    {
						streamWriter.WriteLine();
						streamWriter.WriteLine("/******************************************************************************");
						streamWriter.WriteLine("******************************************************************************/");
					}

					// Create the drop statment
					streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
					streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("go");
					streamWriter.WriteLine();

					// Create the SQL for the stored procedure
					streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("(");

					// Create the parameter list
					for (int i = 0; i < compositeKeyList.Count; i++)
					{
						Column column = compositeKeyList[i];

						if (i < (compositeKeyList.Count - 1))
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false) + ",");
						}
						else
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false));
						}
					}
					streamWriter.WriteLine(")");

					streamWriter.WriteLine();
					streamWriter.WriteLine("as");
					streamWriter.WriteLine();
					streamWriter.WriteLine("set nocount on");
					streamWriter.WriteLine();
					streamWriter.WriteLine("delete from [" + table.Name + "]");
					streamWriter.Write("where");

					// Create the where clause
					for (int i = 0; i < compositeKeyList.Count; i++)
					{
						Column column = compositeKeyList[i];

						if (i == 0)
						{
							streamWriter.WriteLine(" [" + column.Name + "] = @" + column.Name);
						}
						else
						{
							streamWriter.WriteLine("\tand [" + column.Name + "] = @" + column.Name);
						}
					}

					streamWriter.WriteLine("go");

					// Create the grant statement, if a user was specified
					if (grantLoginName.Length > 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
						streamWriter.WriteLine("go");
					}
				}
			}
		}

		/// <summary>
		/// Creates an select stored procedure SQL script for the specified table
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateSelectStoredProcedure(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			if (table.PrimaryKeys.Count > 0 && table.ForeignKeys.Count != table.Columns.Count)
			{
				// Create the stored procedure name
				string procedureName = storedProcedurePrefix + table.Name + "Select";
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, procedureName + ".sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
				    // Create the "use" statement or the seperator
				    if (createMultipleFiles)
				    {
                        CreateUseDatabaseStatement(databaseName, streamWriter);
                    }
                    else
                    {
						streamWriter.WriteLine();
						streamWriter.WriteLine("/******************************************************************************");
						streamWriter.WriteLine("******************************************************************************/");
					}

					// Create the drop statment
					streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
					streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("go");
					streamWriter.WriteLine();

					// Create the SQL for the stored procedure
					streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("(");

					// Create the parameter list
					for (int i = 0; i < table.PrimaryKeys.Count; i++)
					{
						Column column = table.PrimaryKeys[i];

						if (i == (table.PrimaryKeys.Count - 1))
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false));
						}
						else
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false) + ",");
						}
					}

					streamWriter.WriteLine(")");

					streamWriter.WriteLine();
					streamWriter.WriteLine("as");
					streamWriter.WriteLine();
					streamWriter.WriteLine("set nocount on");
					streamWriter.WriteLine();
					streamWriter.Write("select");

					// Create the list of columns
					for (int i = 0; i < table.Columns.Count; i++)
					{
						Column column = table.Columns[i];

						if (i == 0)
						{
							streamWriter.Write(" ");
						}
						else
						{
							streamWriter.Write("\t");
						}

						streamWriter.Write("[" + column.Name + "]");

						if (i < (table.Columns.Count - 1))
						{
							streamWriter.Write(",");
						}

						streamWriter.WriteLine();
					}

					streamWriter.WriteLine("from [" + table.Name + "]");
					streamWriter.Write("where");

					// Create the where clause
					for (int i = 0; i < table.PrimaryKeys.Count; i++)
					{
						Column column = table.PrimaryKeys[i];

						if (i == 0)
						{
							streamWriter.WriteLine(" [" + column.Name + "] = @" + column.Name);
						}
						else
						{
							streamWriter.WriteLine("\tand [" + column.Name + "] = @" + column.Name);
						}
					}

					streamWriter.WriteLine("go");

					// Create the grant statement, if a user was specified
					if (grantLoginName.Length > 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
						streamWriter.WriteLine("go");
					}
				}
			}
		}

		/// <summary>
		/// Creates an select all stored procedure SQL script for the specified table
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateSelectAllStoredProcedure(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			if (table.PrimaryKeys.Count > 0 && table.ForeignKeys.Count != table.Columns.Count)
			{
				// Create the stored procedure name
				string procedureName = storedProcedurePrefix + table.Name + "SelectAll";
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, procedureName + ".sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
				    // Create the "use" statement or the seperator
				    if (createMultipleFiles)
				    {
                        CreateUseDatabaseStatement(databaseName, streamWriter);
                    }
                    else
                    {
						streamWriter.WriteLine();
						streamWriter.WriteLine("/******************************************************************************");
						streamWriter.WriteLine("******************************************************************************/");
					}

					// Create the drop statment
					streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
					streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("go");
					streamWriter.WriteLine();

					// Create the SQL for the stored procedure
					streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine();
					streamWriter.WriteLine("as");
					streamWriter.WriteLine();
					streamWriter.WriteLine("set nocount on");
					streamWriter.WriteLine();
					streamWriter.Write("select");

					// Create the list of columns
					for (int i = 0; i < table.Columns.Count; i++)
					{
						Column column = table.Columns[i];

						if (i == 0)
						{
							streamWriter.Write(" ");
						}
						else
						{
							streamWriter.Write("\t");
						}
						
						streamWriter.Write("[" + column.Name + "]");
						
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.Write(",");
						}
						
						streamWriter.WriteLine();
					}

					streamWriter.WriteLine("from [" + table.Name + "]");

					streamWriter.WriteLine("go");

					// Create the grant statement, if a user was specified
					if (grantLoginName.Length > 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
						streamWriter.WriteLine("go");
					}
				}
			}
		}

		/// <summary>
		/// Creates one or more select stored procedures SQL script for the specified table and its foreign keys
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this stored procedure will be created for.</param>
		/// <param name="grantLoginName">Name of the SQL Server user that should have execute rights on the stored procedure.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="path">Path where the stored procedure script should be created.</param>
		/// <param name="createMultipleFiles">Indicates the procedure(s) generated should be created in its own file.</param>
		public static void CreateSelectAllByStoredProcedures(string databaseName, Table table, string grantLoginName, string storedProcedurePrefix, string path, bool createMultipleFiles)
		{
			// Create a stored procedure for each foreign key
			foreach (List<Column> compositeKeyList in table.ForeignKeys.Values)
			{
				// Create the stored procedure name
				StringBuilder stringBuilder = new StringBuilder(255);
				stringBuilder.Append(storedProcedurePrefix + table.Name + "SelectAllBy");

				// Create the parameter list
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					if (i > 0)
					{
						stringBuilder.Append("_" + Utility.FormatPascal(column.Name));
					}
					else
					{
						stringBuilder.Append(Utility.FormatPascal(column.Name));
					}
				}

				string procedureName = stringBuilder.ToString();
				string fileName;

				// Determine the file name to be used
				if (createMultipleFiles)
				{
					fileName = Path.Combine(path, procedureName + ".sql");
				}
				else
				{
					fileName = Path.Combine(path, "StoredProcedures.sql");
				}

				using (StreamWriter streamWriter = new StreamWriter(fileName, true))
				{
				    // Create the "use" statement or the seperator
				    if (createMultipleFiles)
				    {
                        CreateUseDatabaseStatement(databaseName, streamWriter);
                    }
                    else
                    {
						streamWriter.WriteLine();
						streamWriter.WriteLine("/******************************************************************************");
						streamWriter.WriteLine("******************************************************************************/");
					}

					// Create the drop statment
					streamWriter.WriteLine("if exists (select * from dbo.sysobjects where id = object_id(N'[dbo].[" + procedureName + "]') and ObjectProperty(id, N'IsProcedure') = 1)");
					streamWriter.WriteLine("\tdrop procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("go");
					streamWriter.WriteLine();

					// Create the SQL for the stored procedure
					streamWriter.WriteLine("create procedure [dbo].[" + procedureName + "]");
					streamWriter.WriteLine("(");

					// Create the parameter list
					for (int i = 0; i < compositeKeyList.Count; i++)
					{
						Column column = compositeKeyList[i];

						if (i < (compositeKeyList.Count - 1))
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false) + ",");
						}
						else
						{
							streamWriter.WriteLine("\t" + Utility.CreateParameterString(column, false));
						}
					}
					streamWriter.WriteLine(")");

					streamWriter.WriteLine();
					streamWriter.WriteLine("as");
					streamWriter.WriteLine();
					streamWriter.WriteLine("set nocount on");
					streamWriter.WriteLine();
					streamWriter.Write("select");

					// Create the list of columns
					for (int i = 0; i < table.Columns.Count; i++)
					{
						Column column = table.Columns[i];

						if (i == 0)
						{
							streamWriter.Write(" ");
						}
						else
						{
							streamWriter.Write("\t");
						}

						streamWriter.Write("[" + column.Name + "]");

						if (i < (table.Columns.Count - 1))
						{
							streamWriter.Write(",");
						}

						streamWriter.WriteLine();
					}

					streamWriter.WriteLine("from [" + table.Name + "]");
					streamWriter.Write("where");

					// Create the where clause
					for (int i = 0; i < compositeKeyList.Count; i++)
					{
						Column column = compositeKeyList[i];

						if (i == 0)
						{
							streamWriter.WriteLine(" [" + column.Name + "] = @" + column.Name);
						}
						else
						{
							streamWriter.WriteLine("\tand [" + column.Name + "] = @" + column.Name);
						}
					}

					streamWriter.WriteLine("go");

					// Create the grant statement, if a user was specified
					if (grantLoginName.Length > 0)
					{
						streamWriter.WriteLine();
						streamWriter.WriteLine("grant execute on [dbo].[" + procedureName + "] to [" + grantLoginName + "]");
						streamWriter.WriteLine("go");
					}
				}
			}
		}
	}
}
