using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DataTierGenerator
{
	/// <summary>
	/// Generates C# data access and data transfer classes.
	/// </summary>
	internal static class CsGenerator
	{
		/// <summary>
		/// Creates a project file that references each generated C# code file for data access.
		/// </summary>
		/// <param name="path">The path where the project file should be created.</param>
		/// <param name="projectName">The name of the project.</param>
		/// <param name="tableList">The list of tables code files were created for.</param>
		/// <param name="daoSuffix">The suffix to append to the name of each data access class.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		public static void CreateProjectFile(string path, string projectName, List<Table> tableList, string daoSuffix, string dtoSuffix)
		{
			string projectXml = Utility.GetResource("DataTierGenerator.Resources.Project.xml");
			XmlDocument document = new XmlDocument();
			document.LoadXml(projectXml);

			XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);
			namespaceManager.AddNamespace(String.Empty, "http://schemas.microsoft.com/developer/msbuild/2003");
			namespaceManager.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");

			document.SelectSingleNode("/msbuild:Project/msbuild:PropertyGroup/msbuild:ProjectGuid", namespaceManager).InnerText = "{" + Guid.NewGuid().ToString() + "}";
			document.SelectSingleNode("/msbuild:Project/msbuild:PropertyGroup/msbuild:RootNamespace", namespaceManager).InnerText = projectName;
			document.SelectSingleNode("/msbuild:Project/msbuild:PropertyGroup/msbuild:AssemblyName", namespaceManager).InnerText = projectName;

			XmlNode itemGroupNode = document.SelectSingleNode("/msbuild:Project/msbuild:ItemGroup[msbuild:Compile]", namespaceManager);
			foreach (Table table in tableList)
			{
				string className = Utility.FormatClassName(table.Name);
				
				XmlNode dtoCompileNode = document.CreateElement("Compile", "http://schemas.microsoft.com/developer/msbuild/2003");
				XmlAttribute dtoAttribute = document.CreateAttribute("Include");
				dtoAttribute.Value = className + dtoSuffix + ".cs";
				dtoCompileNode.Attributes.Append(dtoAttribute);
				itemGroupNode.AppendChild(dtoCompileNode);
				
				XmlNode dataCompileNode = document.CreateElement("Compile", "http://schemas.microsoft.com/developer/msbuild/2003");
				XmlAttribute dataAttribute = document.CreateAttribute("Include");
				dataAttribute.Value = Path.Combine("Repositories", Utility.FormatClassName(table.Name) + daoSuffix + ".cs");
				dataCompileNode.Attributes.Append(dataAttribute);
				itemGroupNode.AppendChild(dataCompileNode);
			}
			
			document.Save(Path.Combine(path, projectName + ".csproj"));
		}

		/// <summary>
		/// Creates the AssemblyInfo.cs file for the project.
		/// </summary>
		/// <param name="path">The root path of the project.</param>
		/// <param name="assemblyTitle">The title of the assembly.</param>
		/// <param name="databaseName">The name of the database the assembly provides access to.</param>
		public static void CreateAssemblyInfo(string path, string assemblyTitle, string databaseName)
		{
			string assemblyInfo = Utility.GetResource("DataTierGenerator.Resources.AssemblyInfo.txt");
			assemblyInfo.Replace("#AssemblyTitle", assemblyTitle);
			assemblyInfo.Replace("#DatabaseName", databaseName);

			string propertiesDirectory = Path.Combine(path, "Properties");
			if (Directory.Exists(propertiesDirectory) == false)
			{
				Directory.CreateDirectory(propertiesDirectory);
			}

			File.WriteAllText(Path.Combine(propertiesDirectory, "AssemblyInfo.cs"), assemblyInfo);
		}

		/// <summary>
		/// Creates the SharpCore DLLs required by the generated code.
		/// </summary>
		/// <param name="path">The root path of the project</param>
		public static void CreateSharpCore(string path)
		{
			string sharpCoreDirectory = Path.Combine(Path.Combine(path, "Lib"), "SharpCore");
			if (Directory.Exists(sharpCoreDirectory) == false)
			{
				Directory.CreateDirectory(sharpCoreDirectory);
			}

			Utility.WriteResourceToFile("DataTierGenerator.Resources.SharpCore.SharpCore.Data.dll", Path.Combine(sharpCoreDirectory, "SharpCore.Data.dll"));
			Utility.WriteResourceToFile("DataTierGenerator.Resources.SharpCore.SharpCore.Data.pdb", Path.Combine(sharpCoreDirectory, "SharpCore.Data.pdb"));
			Utility.WriteResourceToFile("DataTierGenerator.Resources.SharpCore.SharpCore.Extensions.dll", Path.Combine(sharpCoreDirectory, "SharpCore.Extensions.dll"));
			Utility.WriteResourceToFile("DataTierGenerator.Resources.SharpCore.SharpCore.Extensions.pdb", Path.Combine(sharpCoreDirectory, "SharpCore.Extensions.pdb"));
			Utility.WriteResourceToFile("DataTierGenerator.Resources.SharpCore.SharpCore.Utilities.dll", Path.Combine(sharpCoreDirectory, "SharpCore.Utilities.dll"));
			Utility.WriteResourceToFile("DataTierGenerator.Resources.SharpCore.SharpCore.Utilities.pdb", Path.Combine(sharpCoreDirectory, "SharpCore.Utilities.pdb"));
		}

		/// <summary>
		/// Creates a C# class for all of the table's stored procedures.
		/// </summary>
		/// <param name="table">Instance of the Table class that represents the table this class will be created for.</param>
		/// <param name="targetNamespace">The namespace that the generated C# classes should contained in.</param>
		/// <param name="daoSuffix">The suffix to be appended to the data access class.</param>
		/// <param name="path">Path where the class should be created.</param>
		public static void CreateDataTransferClass(Table table, string targetNamespace, string dtoSuffix, string path)
		{
			string className = Utility.FormatClassName(table.Name) + dtoSuffix;

			using (StreamWriter streamWriter = new StreamWriter(Path.Combine(path, className + ".cs")))
			{
				// Create the header for the class
				streamWriter.WriteLine("using System;");
				streamWriter.WriteLine();
				streamWriter.WriteLine("namespace " + targetNamespace);
				streamWriter.WriteLine("{");

				streamWriter.WriteLine("\tpublic class " + className);
				streamWriter.WriteLine("\t{");

				// Create an explicit public constructor
				streamWriter.WriteLine("\t\t#region Constructors");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Initializes a new instance of the " + className + " class.");
				streamWriter.WriteLine("\t\t/// </summary>");
				streamWriter.WriteLine("\t\tpublic " + className + "()");
				streamWriter.WriteLine("\t\t{");
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();

				// Create the "partial" constructor
				int parameterCount = 0;
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Initializes a new instance of the " + className + " class.");
				streamWriter.WriteLine("\t\t/// </summary>");
				streamWriter.Write("\t\tpublic " + className + "(");
				for (int i = 0; i < table.Columns.Count; i++)
				{
					Column column = table.Columns[i];
					if (column.IsIdentity == false && column.IsRowGuidCol == false)
					{
						streamWriter.Write(Utility.CreateMethodParameter(column));
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.Write(", ");
						}
						parameterCount++;
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");
				foreach (Column column in table.Columns)
				{
					if (column.IsIdentity == false && column.IsRowGuidCol == false)
					{
						streamWriter.WriteLine("\t\t\tthis." + Utility.FormatPascal(column.Name) + " = " + Utility.FormatCamel(column.Name) + ";");
					}
				}
				streamWriter.WriteLine("\t\t}");

				// Create the "full featured" constructor, if we haven't already
				if (parameterCount < table.Columns.Count)
				{
					streamWriter.WriteLine();
					streamWriter.WriteLine("\t\t/// <summary>");
					streamWriter.WriteLine("\t\t/// Initializes a new instance of the " + className + " class.");
					streamWriter.WriteLine("\t\t/// </summary>");
					streamWriter.Write("\t\tpublic " + className + "(");
					for (int i = 0; i < table.Columns.Count; i++)
					{
						Column column = table.Columns[i];
						streamWriter.Write(Utility.CreateMethodParameter(column));
						if (i < (table.Columns.Count - 1))
						{
							streamWriter.Write(", ");
						}
					}
					streamWriter.WriteLine(")");
					streamWriter.WriteLine("\t\t{");
					foreach (Column column in table.Columns)
					{
						streamWriter.WriteLine("\t\t\tthis." + Utility.FormatCamel(column.Name) + " = " + Utility.FormatCamel(column.Name) + ";");
					}
					streamWriter.WriteLine("\t\t}");
				}

				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t#endregion");
				streamWriter.WriteLine();

				// Append the public properties
				streamWriter.WriteLine("\t\t#region Properties");
				for (int i = 0; i < table.Columns.Count; i++)
				{
					Column column = table.Columns[i];
					string parameter = Utility.CreateMethodParameter(column);
					string type = parameter.Split(' ')[0];
					string name = parameter.Split(' ')[1];

					streamWriter.WriteLine("\t\t/// <summary>");
					streamWriter.WriteLine("\t\t/// Gets or sets the " + Utility.FormatPascal(name) + " value.");
					streamWriter.WriteLine("\t\t/// </summary>");
					streamWriter.WriteLine("\t\tpublic " + type + " " + Utility.FormatPascal(name) + " { get; set; }");

					if (i < (table.Columns.Count - 1))
					{
						streamWriter.WriteLine();
					}
				}
				
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t#endregion");

				// Close out the class and namespace
				streamWriter.WriteLine("\t}");
				streamWriter.WriteLine("}");
			}
		}
		
		/// <summary>
		/// Creates a C# data access class for all of the table's stored procedures.
		/// </summary>
		/// <param name="databaseName">The name of the database.</param>
		/// <param name="table">Instance of the Table class that represents the table this class will be created for.</param>
		/// <param name="targetNamespace">The namespace that the generated C# classes should contained in.</param>
		/// <param name="storedProcedurePrefix">Prefix to be appended to the name of the stored procedure.</param>
		/// <param name="daoSuffix">The suffix to be appended to the data access class.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="path">Path where the class should be created.</param>
		public static void CreateDataAccessClass(string databaseName, Table table, string targetNamespace, string storedProcedurePrefix, string daoSuffix, string dtoSuffix, string path)
		{
			string className = Utility.FormatClassName(table.Name) + daoSuffix;
			path = Path.Combine(path, "Repositories");
			
			using (StreamWriter streamWriter = new StreamWriter(Path.Combine(path, className + ".cs")))
			{
				// Create the header for the class
				streamWriter.WriteLine("using System;");
				streamWriter.WriteLine("using System.Collections.Generic;");
				streamWriter.WriteLine("using System.Data;");
				streamWriter.WriteLine("using System.Data.SqlClient;");
				streamWriter.WriteLine("using SharpCore.Data;");
				streamWriter.WriteLine("using SharpCore.Extensions;");
				streamWriter.WriteLine("using SharpCore.Utilities;");
				streamWriter.WriteLine();

				streamWriter.WriteLine("namespace " + targetNamespace + "." + daoSuffix);
				streamWriter.WriteLine("{");

				streamWriter.WriteLine("\tpublic class " + className);
				streamWriter.WriteLine("\t{");

				// Append the fields
				streamWriter.WriteLine("\t\t#region Fields");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\tprivate string connectionStringName;");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t#endregion");
				streamWriter.WriteLine();
				
				// Append the constructors
				streamWriter.WriteLine("\t\t#region Constructors");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\tpublic " + className + "(string connectionStringName)");
				streamWriter.WriteLine("\t\t{");
				streamWriter.WriteLine("\t\t\tValidationUtility.ValidateArgument(\"connectionStringName\", connectionStringName);");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t\tthis.connectionStringName = connectionStringName;");
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t#endregion");
				streamWriter.WriteLine();

				// Append the access methods
				streamWriter.WriteLine("\t\t#region Methods");
				streamWriter.WriteLine();
				
				CreateInsertMethod(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateUpdateMethod(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateDeleteMethod(table, storedProcedurePrefix, streamWriter);
				CreateDeleteAllByMethods(table, storedProcedurePrefix, streamWriter);
				CreateSelectMethod(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateSelectJsonMethod(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateSelectAllMethod(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateSelectAllJsonMethod(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateSelectAllByMethods(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateSelectAllByJsonMethods(table, storedProcedurePrefix, dtoSuffix, streamWriter);
				CreateMapMethod(table, dtoSuffix, streamWriter);

				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t#endregion");

				// Close out the class and namespace
				streamWriter.WriteLine("\t}");
				streamWriter.WriteLine("}");
			}
		}

		/// <summary>
		/// Creates a string that represents the insert functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateInsertMethod(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			string className = Utility.FormatClassName(table.Name) + dtoSuffix;
			string variableName = Utility.FormatVariableName(table.Name);

			// Append the method header
			streamWriter.WriteLine("\t\t/// <summary>");
			streamWriter.WriteLine("\t\t/// Saves a record to the " + table.Name + " table.");
			streamWriter.WriteLine("\t\t/// </summary>");
			streamWriter.WriteLine("\t\tpublic void Insert(" + className + " " + variableName + ")");
			streamWriter.WriteLine("\t\t{");
			
			// Append validation for the parameter
			streamWriter.WriteLine("\t\t\tValidationUtility.ValidateArgument(\"" + variableName + "\", " + variableName + ");");
			streamWriter.WriteLine();
			
			// Append the parameter declarations
			streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
			streamWriter.WriteLine("\t\t\t{");
			for (int i = 0; i < table.Columns.Count; i++)
			{
				Column column = table.Columns[i];
				if (column.IsIdentity == false && column.IsRowGuidCol == false)
				{
					streamWriter.Write("\t\t\t\t" + Utility.CreateSqlParameter(table, column));
					if (i < (table.Columns.Count - 1))
					{
						streamWriter.Write(",");
					}
					
					streamWriter.WriteLine();
				}
			}

			streamWriter.WriteLine("\t\t\t};");
			streamWriter.WriteLine();
			
			bool hasReturnValue = false;
			foreach (Column column in table.Columns)
			{
				if (column.IsIdentity || column.IsRowGuidCol)
				{
					if (column.IsIdentity && column.Length == "4")
					{
						streamWriter.WriteLine("\t\t\t" + variableName + "." + Utility.FormatPascal(column.Name) + " = (int) SqlClientUtility.ExecuteScalar(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Insert\", parameters);");
						hasReturnValue = true;
					}
					else if (column.IsIdentity && column.Length == "8")
					{
						streamWriter.WriteLine("\t\t\t" + variableName + "." + Utility.FormatPascal(column.Name) + " = (long) SqlClientUtility.ExecuteScalar(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Insert\", parameters);");
						hasReturnValue = true;
					}
					else if (column.IsRowGuidCol)
					{
						streamWriter.WriteLine("\t\t\t" + variableName + "." + Utility.FormatPascal(column.Name) + " = (Guid) SqlClientUtility.ExecuteScalar(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Insert\", parameters);");
						hasReturnValue = true;
					}
				}
			}

			if (hasReturnValue == false)
			{
				streamWriter.WriteLine("\t\t\tSqlClientUtility.ExecuteNonQuery(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Insert\", parameters);");
			}

			// Append the method footer
			streamWriter.WriteLine("\t\t}");
			streamWriter.WriteLine();
		}

		/// <summary>
		/// Creates a string that represents the update functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateUpdateMethod(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			if (table.PrimaryKeys.Count > 0 && table.Columns.Count != table.PrimaryKeys.Count && table.Columns.Count != table.ForeignKeys.Count)
			{
				string className = Utility.FormatClassName(table.Name) + dtoSuffix;
				string variableName = Utility.FormatVariableName(table.Name);

				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Updates a record in the " + table.Name + " table.");
				streamWriter.WriteLine("\t\t/// </summary>");
				streamWriter.WriteLine("\t\tpublic void Update(" + className + " " + variableName + ")");
				streamWriter.WriteLine("\t\t{");

				// Append validation for the parameter
				streamWriter.WriteLine("\t\t\tValidationUtility.ValidateArgument(\"" + variableName + "\", " + variableName + ");");
				streamWriter.WriteLine();

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < table.Columns.Count; i++)
				{
					Column column = table.Columns[i];
					streamWriter.Write("\t\t\t\t" + Utility.CreateSqlParameter(table, column));
					if (i < (table.Columns.Count - 1))
					{
						streamWriter.Write(",");
					}
					
					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();

				streamWriter.WriteLine("\t\t\tSqlClientUtility.ExecuteNonQuery(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Update\", parameters);");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the delete functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateDeleteMethod(Table table, string storedProcedurePrefix, StreamWriter streamWriter)
		{
			if (table.PrimaryKeys.Count > 0)
			{
				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Deletes a record from the " + table.Name + " table by its primary key.");
				streamWriter.WriteLine("\t\t/// </summary>");
				streamWriter.Write("\t\tpublic void Delete(");
				for (int i = 0; i < table.PrimaryKeys.Count; i++)
				{
					Column column = table.PrimaryKeys[i];
					streamWriter.Write(Utility.CreateMethodParameter(column));
					if (i < (table.PrimaryKeys.Count - 1))
					{
						streamWriter.Write(", ");
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < table.PrimaryKeys.Count; i++)
				{
					Column column = table.PrimaryKeys[i];
					streamWriter.Write("\t\t\t\tnew SqlParameter(\"@" + column.Name + "\", " + Utility.FormatCamel(column.Name) + ")");
					if (i < (table.PrimaryKeys.Count - 1))
					{
						streamWriter.Write(",");
					}

					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();
				
				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\tSqlClientUtility.ExecuteNonQuery(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Delete\", parameters);");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the "delete by" functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateDeleteAllByMethods(Table table, string storedProcedurePrefix, StreamWriter streamWriter)
		{
			// Create a stored procedure for each foreign key
			foreach (List<Column> compositeKeyList in table.ForeignKeys.Values)
			{
				// Create the stored procedure name
				StringBuilder stringBuilder = new StringBuilder(255);
				stringBuilder.Append("DeleteAllBy");
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
				string methodName = stringBuilder.ToString();
				string procedureName = storedProcedurePrefix + table.Name + methodName;

				// Create the delete function based on keys
				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Deletes a record from the " + table.Name + " table by a foreign key.");
				streamWriter.WriteLine("\t\t/// </summary>");

				streamWriter.Write("\t\tpublic void " + methodName + "(");
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					streamWriter.Write(Utility.CreateMethodParameter(column));
					if (i < (compositeKeyList.Count - 1))
					{
						streamWriter.Write(", ");
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					streamWriter.Write("\t\t\t\tnew SqlParameter(\"@" + column.Name + "\", " + Utility.FormatCamel(column.Name) + ")");
					if (i < (compositeKeyList.Count - 1))
					{
						streamWriter.Write(",");
					}

					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();
				
				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\tSqlClientUtility.ExecuteNonQuery(connectionStringName, CommandType.StoredProcedure, \"" + procedureName + "\", parameters);");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the select by primary key functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateSelectMethod(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			if (table.PrimaryKeys.Count > 0 && table.Columns.Count != table.ForeignKeys.Count)
			{
				string className = Utility.FormatClassName(table.Name) + dtoSuffix;

				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Selects a single record from the " + table.Name + " table.");
				streamWriter.WriteLine("\t\t/// </summary>");

				streamWriter.Write("\t\tpublic " + className + " Select(");
				for (int i = 0; i < table.PrimaryKeys.Count; i++)
				{
					Column column = table.PrimaryKeys[i];
					streamWriter.Write(Utility.CreateMethodParameter(column));
					if (i < (table.PrimaryKeys.Count - 1))
					{
						streamWriter.Write(", ");
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < table.PrimaryKeys.Count; i++)
				{
					Column column = table.PrimaryKeys[i];
					streamWriter.Write("\t\t\t\tnew SqlParameter(\"@" + column.Name + "\", " + Utility.FormatCamel(column.Name) + ")");
					if (i < (table.PrimaryKeys.Count - 1))
					{
						streamWriter.Write(",");
					}

					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();
				
				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\tusing (SqlDataReader dataReader = SqlClientUtility.ExecuteReader(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Select\", parameters))");
				streamWriter.WriteLine("\t\t\t{");
				streamWriter.WriteLine("\t\t\t\tif (dataReader.Read())");
				streamWriter.WriteLine("\t\t\t\t{");
				streamWriter.WriteLine("\t\t\t\t\treturn MapDataReader(dataReader);");
				streamWriter.WriteLine("\t\t\t\t}");
				streamWriter.WriteLine("\t\t\t\telse");
				streamWriter.WriteLine("\t\t\t\t{");
				streamWriter.WriteLine("\t\t\t\t\treturn null;");
				streamWriter.WriteLine("\t\t\t\t}");
				streamWriter.WriteLine("\t\t\t}");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the select JSON by primary key functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateSelectJsonMethod(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			if (table.PrimaryKeys.Count > 0 && table.Columns.Count != table.ForeignKeys.Count)
			{
				string className = Utility.FormatClassName(table.Name) + dtoSuffix;

				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Selects a single record from the " + table.Name + " table.");
				streamWriter.WriteLine("\t\t/// </summary>");

				streamWriter.Write("\t\tpublic string SelectJson(");
				for (int i = 0; i < table.PrimaryKeys.Count; i++)
				{
					Column column = table.PrimaryKeys[i];
					streamWriter.Write(Utility.CreateMethodParameter(column));
					if (i < (table.PrimaryKeys.Count - 1))
					{
						streamWriter.Write(", ");
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < table.PrimaryKeys.Count; i++)
				{
					Column column = table.PrimaryKeys[i];
					streamWriter.Write("\t\t\t\tnew SqlParameter(\"@" + column.Name + "\", " + Utility.FormatCamel(column.Name) + ")");
					if (i < (table.PrimaryKeys.Count - 1))
					{
						streamWriter.Write(",");
					}

					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();
				
				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\treturn SqlClientUtility.ExecuteJson(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "Select\", parameters);");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the select functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateSelectAllMethod(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			if (table.Columns.Count != table.PrimaryKeys.Count && table.Columns.Count != table.ForeignKeys.Count)
			{
				string className = Utility.FormatClassName(table.Name) + dtoSuffix;
				string dtoVariableName = Utility.FormatCamel(className);

				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Selects all records from the " + table.Name + " table.");
				streamWriter.WriteLine("\t\t/// </summary>");
				streamWriter.WriteLine("\t\tpublic List<" + className + "> SelectAll()");
				streamWriter.WriteLine("\t\t{");

				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\tusing (SqlDataReader dataReader = SqlClientUtility.ExecuteReader(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "SelectAll\"))");
				streamWriter.WriteLine("\t\t\t{");
				streamWriter.WriteLine("\t\t\t\tList<" + className + "> " + dtoVariableName + "List = new List<" + className + ">();");
				streamWriter.WriteLine("\t\t\t\twhile (dataReader.Read())");
				streamWriter.WriteLine("\t\t\t\t{");
				streamWriter.WriteLine("\t\t\t\t\t" + className + " " + dtoVariableName + " = MapDataReader(dataReader);");
				streamWriter.WriteLine("\t\t\t\t\t" + dtoVariableName + "List.Add(" + dtoVariableName + ");");
				streamWriter.WriteLine("\t\t\t\t}");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t\t\treturn " + dtoVariableName + "List;");
				streamWriter.WriteLine("\t\t\t}");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the select JSON functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateSelectAllJsonMethod(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			if (table.Columns.Count != table.PrimaryKeys.Count && table.Columns.Count != table.ForeignKeys.Count)
			{
				string className = Utility.FormatClassName(table.Name) + dtoSuffix;
				string dtoVariableName = Utility.FormatCamel(className);

				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Selects all records from the " + table.Name + " table.");
				streamWriter.WriteLine("\t\t/// </summary>");
				streamWriter.WriteLine("\t\tpublic string SelectAllJson()");
				streamWriter.WriteLine("\t\t{");

				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\treturn SqlClientUtility.ExecuteJson(connectionStringName, CommandType.StoredProcedure, \"" + table.Name + "SelectAll\");");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the "select by" functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateSelectAllByMethods(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			string className = Utility.FormatClassName(table.Name) + dtoSuffix;
			string dtoVariableName = Utility.FormatCamel(className);

			// Create a stored procedure for each foreign key
			foreach (List<Column> compositeKeyList in table.ForeignKeys.Values)
			{
				// Create the stored procedure name
				StringBuilder stringBuilder = new StringBuilder(255);
				stringBuilder.Append("SelectAllBy");
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
				string methodName = stringBuilder.ToString();
				string procedureName = storedProcedurePrefix + table.Name + methodName;

				// Create the select function based on keys
				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Selects all records from the " + table.Name + " table by a foreign key.");
				streamWriter.WriteLine("\t\t/// </summary>");

				streamWriter.Write("\t\tpublic List<" + className + "> " + methodName + "(");
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					streamWriter.Write(Utility.CreateMethodParameter(column));
					if (i < (compositeKeyList.Count - 1))
					{
						streamWriter.Write(", ");
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					streamWriter.Write("\t\t\t\tnew SqlParameter(\"@" + column.Name + "\", " + Utility.FormatCamel(column.Name) + ")");
					if (i < (compositeKeyList.Count - 1))
					{
						streamWriter.Write(",");
					}

					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();
				
				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\tusing (SqlDataReader dataReader = SqlClientUtility.ExecuteReader(connectionStringName, CommandType.StoredProcedure, \"" + procedureName + "\", parameters))");
				streamWriter.WriteLine("\t\t\t{");
				streamWriter.WriteLine("\t\t\t\tList<" + className + "> " + dtoVariableName + "List = new List<" + className + ">();");
				streamWriter.WriteLine("\t\t\t\twhile (dataReader.Read())");
				streamWriter.WriteLine("\t\t\t\t{");
				streamWriter.WriteLine("\t\t\t\t\t" + className + " " + dtoVariableName + " = MapDataReader(dataReader);");
				streamWriter.WriteLine("\t\t\t\t\t" + dtoVariableName + "List.Add(" + dtoVariableName + ");");
				streamWriter.WriteLine("\t\t\t\t}");
				streamWriter.WriteLine();
				streamWriter.WriteLine("\t\t\t\treturn " + dtoVariableName + "List;");
				streamWriter.WriteLine("\t\t\t}");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}

		/// <summary>
		/// Creates a string that represents the "select by" JSON functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
		/// <param name="storedProcedurePrefix">The prefix that is used on the stored procedure that this method will call.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of each data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateSelectAllByJsonMethods(Table table, string storedProcedurePrefix, string dtoSuffix, StreamWriter streamWriter)
		{
			string className = Utility.FormatClassName(table.Name) + dtoSuffix;
			string dtoVariableName = Utility.FormatCamel(className);

			// Create a stored procedure for each foreign key
			foreach (List<Column> compositeKeyList in table.ForeignKeys.Values)
			{
				// Create the stored procedure name
				StringBuilder stringBuilder = new StringBuilder(255);
				stringBuilder.Append("SelectAllBy");
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

				string methodName = stringBuilder.ToString();
				string procedureName = storedProcedurePrefix + table.Name + methodName;

				// Create the select function based on keys
				// Append the method header
				streamWriter.WriteLine("\t\t/// <summary>");
				streamWriter.WriteLine("\t\t/// Selects all records from the " + table.Name + " table by a foreign key.");
				streamWriter.WriteLine("\t\t/// </summary>");

				streamWriter.Write("\t\tpublic string " + methodName + "Json(");
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					streamWriter.Write(Utility.CreateMethodParameter(column));
					if (i < (compositeKeyList.Count - 1))
					{
						streamWriter.Write(", ");
					}
				}
				streamWriter.WriteLine(")");
				streamWriter.WriteLine("\t\t{");

				// Append the parameter declarations
				streamWriter.WriteLine("\t\t\tSqlParameter[] parameters = new SqlParameter[]");
				streamWriter.WriteLine("\t\t\t{");
				for (int i = 0; i < compositeKeyList.Count; i++)
				{
					Column column = compositeKeyList[i];
					streamWriter.Write("\t\t\t\tnew SqlParameter(\"@" + column.Name + "\", " + Utility.FormatCamel(column.Name) + ")");
					if (i < (compositeKeyList.Count - 1))
					{
						streamWriter.Write(",");
					}

					streamWriter.WriteLine();
				}

				streamWriter.WriteLine("\t\t\t};");
				streamWriter.WriteLine();
				
				// Append the stored procedure execution
				streamWriter.WriteLine("\t\t\treturn SqlClientUtility.ExecuteJson(connectionStringName, CommandType.StoredProcedure, \"" + procedureName + "\", parameters);");

				// Append the method footer
				streamWriter.WriteLine("\t\t}");
				streamWriter.WriteLine();
			}
		}
		
		/// <summary>
		/// Creates a string that represents the "map" functionality of the data access class.
		/// </summary>
		/// <param name="table">The Table instance that this method will be created for.</param>
        /// <param name="dtoSuffix">The suffix to append to the name of the data transfer class.</param>
		/// <param name="streamWriter">The StreamWriter instance that will be used to create the method.</param>
		private static void CreateMapMethod(Table table, string dtoSuffix, StreamWriter streamWriter)
		{
			string className = Utility.FormatClassName(table.Name) + dtoSuffix;
			string variableName = Utility.FormatVariableName(className);

			streamWriter.WriteLine("\t\t/// <summary>");
			streamWriter.WriteLine("\t\t/// Creates a new instance of the " + className + " class and populates it with data from the specified SqlDataReader.");
			streamWriter.WriteLine("\t\t/// </summary>");
			streamWriter.WriteLine("\t\tprivate " + className + " MapDataReader(SqlDataReader dataReader)");
			streamWriter.WriteLine("\t\t{");
			streamWriter.WriteLine("\t\t\t" + className + " " + variableName + " = new " + className + "();");
			
			foreach (Column column in table.Columns)
			{
				string columnNamePascal = Utility.FormatPascal(column.Name);
				streamWriter.WriteLine("\t\t\t" + variableName + "." + columnNamePascal + " = dataReader." + Utility.GetGetMethod(column) + "(\"" + column.Name + "\", " + Utility.GetDefaultValue(column) + ");");
			}
			
			streamWriter.WriteLine();
			streamWriter.WriteLine("\t\t\treturn " + variableName + ";");
			streamWriter.WriteLine("\t\t}");
		}
	}
}
