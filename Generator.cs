using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace DataTierGenerator
{
	internal delegate void CountUpdate(object sender, CountEventArgs e);

	/// <summary>
	/// Generates C# and SQL code for accessing a database.
	/// </summary>
	internal static class Generator
	{
		public static event CountUpdate DatabaseCounted;
		public static event CountUpdate TableCounted;

		/// <summary>
		/// Generates the SQL and C# code for the specified database.
		/// </summary>
		/// <param name="outputDirectory">The directory where the C# and SQL code should be created.</param>
		/// <param name="connectionString">The connection string to be used to connect the to the database.</param>
		/// <param name="grantLoginName">The SQL Server login name that should be granted execute rights on the generated stored procedures.</param>
		/// <param name="storedProcedurePrefix">The prefix that should be used when creating stored procedures.</param>
		/// <param name="createMultipleFiles">A flag indicating if the generated stored procedures should be created in one file or separate files.</param>
        /// <param name="projectName">The name of the project file to be generated.</param>
		/// <param name="targetNamespace">The namespace that the generated C# classes should contained in.</param>
		/// <param name="daoSuffix">The suffix to be applied to all generated DAO classes.</param>
        /// <param name="dtoSuffix">The suffix to be applied to all generated DTO classes.</param>
		public static void Generate(string outputDirectory, string connectionString, string grantLoginName, string storedProcedurePrefix, bool createMultipleFiles, string projectName, string targetNamespace, string daoSuffix, string dtoSuffix)
		{
			List<Table> tableList = new List<Table>();
			string databaseName;
			string sqlPath;
			string csPath;

			using (SqlConnection connection = new SqlConnection(connectionString))
			{
				databaseName = Utility.FormatPascal(connection.Database);
				sqlPath = Path.Combine(outputDirectory, "SQL");
				csPath = Path.Combine(outputDirectory, "CS");

				connection.Open();

				// Get a list of the entities in the database
				DataTable dataTable = new DataTable();
				SqlDataAdapter dataAdapter = new SqlDataAdapter(Utility.GetTableQuery(connection.Database), connection);
				dataAdapter.Fill(dataTable);

				// Process each table
				foreach (DataRow dataRow in dataTable.Rows)
				{
					Table table = new Table();
					table.Name = (string) dataRow["TABLE_NAME"];
					QueryTable(connection, table);
					tableList.Add(table);
				}
			}

			DatabaseCounted(null, new CountEventArgs(tableList.Count));

			// Generate the necessary SQL and C# code for each table
			int count = 0;
			if (tableList.Count > 0)
			{
				// Create the necessary directories
				Utility.CreateSubDirectory(sqlPath, true);
				Utility.CreateSubDirectory(csPath, true);
				Utility.CreateSubDirectory(Path.Combine(csPath, "Repositories"), true);

                // Create the necessary "use [database]" statement
                SqlGenerator.CreateUseDatabaseStatement(databaseName, sqlPath, createMultipleFiles);

				// Create the necessary database logins
				SqlGenerator.CreateUserQueries(databaseName, grantLoginName, sqlPath, createMultipleFiles);

				// Create the CRUD stored procedures and data access code for each table
				foreach (Table table in tableList)
				{
					SqlGenerator.CreateInsertStoredProcedure(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);
					SqlGenerator.CreateUpdateStoredProcedure(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);
					SqlGenerator.CreateDeleteStoredProcedure(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);
					SqlGenerator.CreateDeleteAllByStoredProcedures(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);
					SqlGenerator.CreateSelectStoredProcedure(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);
					SqlGenerator.CreateSelectAllStoredProcedure(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);
					SqlGenerator.CreateSelectAllByStoredProcedures(databaseName, table, grantLoginName, storedProcedurePrefix, sqlPath, createMultipleFiles);

					CsGenerator.CreateDataTransferClass(table, targetNamespace, dtoSuffix, csPath);
					CsGenerator.CreateDataAccessClass(databaseName, table, targetNamespace, storedProcedurePrefix, daoSuffix, dtoSuffix, csPath);

					count++;
					TableCounted(null, new CountEventArgs(count));
				}

				CsGenerator.CreateSharpCore(csPath);
				CsGenerator.CreateAssemblyInfo(csPath, databaseName, databaseName);
				CsGenerator.CreateProjectFile(csPath, projectName, tableList, daoSuffix, dtoSuffix);
			}
		}

		/// <summary>
		/// Retrieves the column, primary key, and foreign key information for the specified table.
		/// </summary>
		/// <param name="connection">The SqlConnection to be used when querying for the table information.</param>
		/// <param name="table">The table instance that information should be retrieved for.</param>
		private static void QueryTable(SqlConnection connection, Table table)
		{
			// Get a list of the entities in the database
			DataTable dataTable = new DataTable();
			SqlDataAdapter dataAdapter = new SqlDataAdapter(Utility.GetColumnQuery(table.Name), connection);
			dataAdapter.Fill(dataTable);

			foreach (DataRow columnRow in dataTable.Rows)
			{
				Column column = new Column();
				column.Name = columnRow["COLUMN_NAME"].ToString();
				column.Type = columnRow["DATA_TYPE"].ToString();
				column.Precision = columnRow["NUMERIC_PRECISION"].ToString();
				column.Scale = columnRow["NUMERIC_SCALE"].ToString();

				// Determine the column's length
				if (columnRow["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value)
				{
					column.Length = columnRow["CHARACTER_MAXIMUM_LENGTH"].ToString();
				}
				else
				{
					column.Length = columnRow["COLUMN_LENGTH"].ToString();
				}

				// Is the column a RowGuidCol column?
				if (columnRow["IS_ROWGUIDCOL"].ToString() == "1")
				{
					column.IsRowGuidCol = true;
				}

				// Is the column an Identity column?
				if (columnRow["IS_IDENTITY"].ToString() == "1")
				{
					column.IsIdentity = true;
				}

				// Is columnRow column a computed column?
				if (columnRow["IS_COMPUTED"].ToString() == "1")
				{
					column.IsComputed = true;
				}

				table.Columns.Add(column);
			}

			// Get the list of primary keys
			DataTable primaryKeyTable = Utility.GetPrimaryKeyList(connection, table.Name);
			foreach (DataRow primaryKeyRow in primaryKeyTable.Rows)
			{
				string primaryKeyName = primaryKeyRow["COLUMN_NAME"].ToString();

				foreach (Column column in table.Columns)
				{
					if (column.Name == primaryKeyName)
					{
						table.PrimaryKeys.Add(column);
						break;
					}
				}
			}

			// Get the list of foreign keys
			DataTable foreignKeyTable = Utility.GetForeignKeyList(connection, table.Name);
			foreach (DataRow foreignKeyRow in foreignKeyTable.Rows)
			{
				string name = foreignKeyRow["FK_NAME"].ToString();
				string columnName = foreignKeyRow["FKCOLUMN_NAME"].ToString();

				if (table.ForeignKeys.ContainsKey(name) == false)
				{
					table.ForeignKeys.Add(name, new List<Column>());
				}

				List<Column> foreignKeys = table.ForeignKeys[name];

				foreach (Column column in table.Columns)
				{
					if (column.Name == columnName)
					{
						foreignKeys.Add(column);
						break;
					}
				}
			}
		}
	}
}
