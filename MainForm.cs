using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DataTierGenerator
{
	/// <summary>
	/// Form used to collect the connection information for the code we're going to generate.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label serverLabel;
		private System.Windows.Forms.Label databaseLabel;
		private System.Windows.Forms.GroupBox authenticationGroupBox;
		private System.Windows.Forms.TextBox passwordTextBox;
		private System.Windows.Forms.TextBox loginNameTextBox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.Label loginNameLabel;
		private System.Windows.Forms.RadioButton sqlServerAuthenticationRadioButton;
		private System.Windows.Forms.RadioButton windowsAuthenticationRadioButton;
		private System.Windows.Forms.TextBox serverTextBox;
		private System.Windows.Forms.TextBox databaseTextBox;
		private System.Windows.Forms.Button generateButton;
		private System.Windows.Forms.GroupBox sqlGroupBox;
		private System.Windows.Forms.TextBox grantUserTextBox;
		private System.Windows.Forms.Label grantUserLabel;
		private System.Windows.Forms.CheckBox multipleFilesCheckBox;
		private System.Windows.Forms.GroupBox csGroupBox;
		private System.Windows.Forms.TextBox namespaceTextBox;
		private System.Windows.Forms.Label namespaceLabel;
		private System.Windows.Forms.TextBox storedProcedurePrefixTextBox;
		private System.Windows.Forms.Label storedProcedurePrefixLabel;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.TextBox daoSuffixTextBox;
		private System.Windows.Forms.Label daoSuffixLabel;
        private TextBox projectTextBox;
        private Label projectLabel;
        private TextBox dtoSuffixTextBox;
        private Label dtoSuffixLabel;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		
		public MainForm()
	    {
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			
			Generator.DatabaseCounted += new CountUpdate(Generator_DatabaseCounted);
			Generator.TableCounted += new CountUpdate(Generator_TableCounted);
			
			// Load settings from the Registry
			using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"Software\Adrian Anttila\CSharpDataTier"))
	        {
				serverTextBox.Text = (string) registryKey.GetValue("Server", String.Empty);
				databaseTextBox.Text = (string) registryKey.GetValue("Database", String.Empty);
				if (((int) registryKey.GetValue("AuthenticationType", 1)) == 1)
	            {
					windowsAuthenticationRadioButton.Checked = true;
				}
                else
	            {
					sqlServerAuthenticationRadioButton.Checked = true;
				}
				
                loginNameTextBox.Text = (string) registryKey.GetValue("Login", String.Empty);
				grantUserTextBox.Text = (string) registryKey.GetValue("GrantUser", String.Empty);
				storedProcedurePrefixTextBox.Text = (string) registryKey.GetValue("StoredProcedurePrefix", String.Empty);
				if (((int) registryKey.GetValue("CreateMultipleFiles", 0)) == 1)
	            {
					multipleFilesCheckBox.Checked = true;
				}

				projectTextBox.Text = (string) registryKey.GetValue("Project", String.Empty);
				namespaceTextBox.Text = (string) registryKey.GetValue("Namespace", String.Empty);
				daoSuffixTextBox.Text = (string) registryKey.GetValue("DaoSuffix", String.Empty);
				dtoSuffixTextBox.Text = (string) registryKey.GetValue("DtoSuffix", String.Empty);
			}
		}
		
		private void DatabaseTextBox_TextChanged(object sender, System.EventArgs e)
	    {
			EnableGenerateButton();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
	    {
			if (disposing)
	        {
				if (components != null)
	            {
					components.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		private void EnableGenerateButton()
	    {
			if (serverTextBox.Text.Length == 0)
	        {
				generateButton.Enabled = false;
				return;
			}

			if (databaseTextBox.Text.Length == 0)
	        {
				generateButton.Enabled = false;
				return;
			}
		
			if (sqlServerAuthenticationRadioButton.Checked)
	        {
				if (loginNameTextBox.Text.Length == 0)
	            {
					generateButton.Enabled = false;
					return;
				}
			}

			if (namespaceTextBox.Text.Length == 0)
	        {
				generateButton.Enabled = false;
				return;
			}

			generateButton.Enabled = true;
		}

		private void GenerateButton_Click(object sender, System.EventArgs e)
    	{
			string connectionString;
			
			try
	        {
				generateButton.Enabled = false;
				
				// Build the connection string
				if (windowsAuthenticationRadioButton.Checked)
	            {
					connectionString = "Server=" + serverTextBox.Text + "; Database=" + databaseTextBox.Text + "; Integrated Security=SSPI;";
				}
                else
	            {
					connectionString = "Server=" + serverTextBox.Text + "; Database=" + databaseTextBox.Text + "; User ID=" + loginNameTextBox.Text + "; Password=" + passwordTextBox.Text + ";";
				}
				
				// Let the user select where to create the C# and SQL code
                string outputDirectory;
			    using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"Software\Adrian Anttila\CSharpDataTier"))
	            {
				    outputDirectory = (string) registryKey.GetValue("OutputDirectory", String.Empty);
                }

				using (FolderBrowserDialog dialog = new FolderBrowserDialog())
	            {
					dialog.Description = "Select an output directory";
                    dialog.SelectedPath = outputDirectory;
					dialog.ShowNewFolderButton = true;
					if (dialog.ShowDialog(this) == DialogResult.OK)
	                {
						outputDirectory = dialog.SelectedPath;
					}
                    else
	                {
						return;
					}
				}				
				
				// Generate the SQL and C# code
				Generator.Generate(outputDirectory, connectionString, grantUserTextBox.Text, storedProcedurePrefixTextBox.Text, multipleFilesCheckBox.Checked, projectTextBox.Text, namespaceTextBox.Text, daoSuffixTextBox.Text, dtoSuffixTextBox.Text);
				
				// Save the current state to the Registry
				using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(@"Software\Adrian Anttila\CSharpDataTier"))
	            {
					registryKey.SetValue("Server", serverTextBox.Text, RegistryValueKind.String);
					registryKey.SetValue("Database", databaseTextBox.Text, RegistryValueKind.String);
					if (windowsAuthenticationRadioButton.Checked)
	                {
						registryKey.SetValue("AuthenticationType", 1, RegistryValueKind.DWord);
					}
                    else
	                {
						registryKey.SetValue("AuthenticationType", 2, RegistryValueKind.DWord);
					}

					registryKey.SetValue("Login", loginNameTextBox.Text, RegistryValueKind.String);
					registryKey.SetValue("GrantUser", grantUserTextBox.Text, RegistryValueKind.String);
					registryKey.SetValue("StoredProcedurePrefix", storedProcedurePrefixTextBox.Text, RegistryValueKind.String);
					if (multipleFilesCheckBox.Checked)
                	{
						registryKey.SetValue("CreateMultipleFiles", 1, RegistryValueKind.DWord);
					}
                    else
	                {
						registryKey.SetValue("CreateMultipleFiles", 0, RegistryValueKind.DWord);
					}

					registryKey.SetValue("Project", projectTextBox.Text, RegistryValueKind.String);
					registryKey.SetValue("Namespace", namespaceTextBox.Text, RegistryValueKind.String);
					registryKey.SetValue("DaoSuffix", daoSuffixTextBox.Text, RegistryValueKind.String);
					registryKey.SetValue("DtoSuffix", dtoSuffixTextBox.Text, RegistryValueKind.String);
                    registryKey.SetValue("OutputDirectory", outputDirectory, RegistryValueKind.String);
				}
				
				// Inform the user we're done
				MessageBox.Show("C# classes and stored procedures generated successfully.");
			}
            catch (Exception ex)
	        {
				MessageBox.Show(ex.Message);
			}
            finally
	        {
				progressBar.Value = 0;
				generateButton.Enabled = true;
			}
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
	    {
            this.serverLabel = new System.Windows.Forms.Label();
            this.databaseLabel = new System.Windows.Forms.Label();
            this.authenticationGroupBox = new System.Windows.Forms.GroupBox();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.loginNameTextBox = new System.Windows.Forms.TextBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.loginNameLabel = new System.Windows.Forms.Label();
            this.sqlServerAuthenticationRadioButton = new System.Windows.Forms.RadioButton();
            this.windowsAuthenticationRadioButton = new System.Windows.Forms.RadioButton();
            this.serverTextBox = new System.Windows.Forms.TextBox();
            this.databaseTextBox = new System.Windows.Forms.TextBox();
            this.generateButton = new System.Windows.Forms.Button();
            this.sqlGroupBox = new System.Windows.Forms.GroupBox();
            this.storedProcedurePrefixTextBox = new System.Windows.Forms.TextBox();
            this.storedProcedurePrefixLabel = new System.Windows.Forms.Label();
            this.grantUserTextBox = new System.Windows.Forms.TextBox();
            this.grantUserLabel = new System.Windows.Forms.Label();
            this.multipleFilesCheckBox = new System.Windows.Forms.CheckBox();
            this.csGroupBox = new System.Windows.Forms.GroupBox();
            this.projectTextBox = new System.Windows.Forms.TextBox();
            this.projectLabel = new System.Windows.Forms.Label();
            this.dtoSuffixTextBox = new System.Windows.Forms.TextBox();
            this.dtoSuffixLabel = new System.Windows.Forms.Label();
            this.daoSuffixTextBox = new System.Windows.Forms.TextBox();
            this.daoSuffixLabel = new System.Windows.Forms.Label();
            this.namespaceTextBox = new System.Windows.Forms.TextBox();
            this.namespaceLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.authenticationGroupBox.SuspendLayout();
            this.sqlGroupBox.SuspendLayout();
            this.csGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // serverLabel
            // 
            this.serverLabel.Location = new System.Drawing.Point(12, 12);
            this.serverLabel.Name = "serverLabel";
            this.serverLabel.Size = new System.Drawing.Size(60, 23);
            this.serverLabel.TabIndex = 0;
            this.serverLabel.Text = "Server:";
            // 
            // databaseLabel
            // 
            this.databaseLabel.Location = new System.Drawing.Point(12, 42);
            this.databaseLabel.Name = "databaseLabel";
            this.databaseLabel.Size = new System.Drawing.Size(60, 23);
            this.databaseLabel.TabIndex = 1;
            this.databaseLabel.Text = "Database:";
            // 
            // authenticationGroupBox
            // 
            this.authenticationGroupBox.Controls.Add(this.passwordTextBox);
            this.authenticationGroupBox.Controls.Add(this.loginNameTextBox);
            this.authenticationGroupBox.Controls.Add(this.passwordLabel);
            this.authenticationGroupBox.Controls.Add(this.loginNameLabel);
            this.authenticationGroupBox.Controls.Add(this.sqlServerAuthenticationRadioButton);
            this.authenticationGroupBox.Controls.Add(this.windowsAuthenticationRadioButton);
            this.authenticationGroupBox.Location = new System.Drawing.Point(12, 72);
            this.authenticationGroupBox.Name = "authenticationGroupBox";
            this.authenticationGroupBox.Size = new System.Drawing.Size(240, 138);
            this.authenticationGroupBox.TabIndex = 2;
            this.authenticationGroupBox.TabStop = false;
            this.authenticationGroupBox.Text = "Authentication";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.passwordTextBox.Enabled = false;
            this.passwordTextBox.Location = new System.Drawing.Point(102, 108);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.PasswordChar = '*';
            this.passwordTextBox.Size = new System.Drawing.Size(132, 20);
            this.passwordTextBox.TabIndex = 5;
            // 
            // loginNameTextBox
            // 
            this.loginNameTextBox.BackColor = System.Drawing.SystemColors.InactiveBorder;
            this.loginNameTextBox.Enabled = false;
            this.loginNameTextBox.Location = new System.Drawing.Point(102, 78);
            this.loginNameTextBox.Name = "loginNameTextBox";
            this.loginNameTextBox.Size = new System.Drawing.Size(132, 20);
            this.loginNameTextBox.TabIndex = 4;
            this.loginNameTextBox.TextChanged += new System.EventHandler(this.LoginNameTextBox_TextChanged);
            // 
            // passwordLabel
            // 
            this.passwordLabel.Enabled = false;
            this.passwordLabel.Location = new System.Drawing.Point(30, 108);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(72, 23);
            this.passwordLabel.TabIndex = 3;
            this.passwordLabel.Text = "Password:";
            // 
            // loginNameLabel
            // 
            this.loginNameLabel.Enabled = false;
            this.loginNameLabel.Location = new System.Drawing.Point(30, 78);
            this.loginNameLabel.Name = "loginNameLabel";
            this.loginNameLabel.Size = new System.Drawing.Size(72, 23);
            this.loginNameLabel.TabIndex = 2;
            this.loginNameLabel.Text = "Login Name:";
            // 
            // sqlServerAuthenticationRadioButton
            // 
            this.sqlServerAuthenticationRadioButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.sqlServerAuthenticationRadioButton.Location = new System.Drawing.Point(12, 48);
            this.sqlServerAuthenticationRadioButton.Name = "sqlServerAuthenticationRadioButton";
            this.sqlServerAuthenticationRadioButton.Size = new System.Drawing.Size(156, 24);
            this.sqlServerAuthenticationRadioButton.TabIndex = 3;
            this.sqlServerAuthenticationRadioButton.Text = "SQL Server Authentication";
            this.sqlServerAuthenticationRadioButton.CheckedChanged += new System.EventHandler(this.SqlServerAuthenticationRadioButton_CheckedChanged);
            // 
            // windowsAuthenticationRadioButton
            // 
            this.windowsAuthenticationRadioButton.Checked = true;
            this.windowsAuthenticationRadioButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.windowsAuthenticationRadioButton.Location = new System.Drawing.Point(12, 24);
            this.windowsAuthenticationRadioButton.Name = "windowsAuthenticationRadioButton";
            this.windowsAuthenticationRadioButton.Size = new System.Drawing.Size(156, 24);
            this.windowsAuthenticationRadioButton.TabIndex = 2;
            this.windowsAuthenticationRadioButton.TabStop = true;
            this.windowsAuthenticationRadioButton.Text = "Windows Authentication";
            this.windowsAuthenticationRadioButton.CheckedChanged += new System.EventHandler(this.WindowsAuthenticationRadioButton_CheckedChanged);
            // 
            // serverTextBox
            // 
            this.serverTextBox.Location = new System.Drawing.Point(72, 12);
            this.serverTextBox.Name = "serverTextBox";
            this.serverTextBox.Size = new System.Drawing.Size(180, 20);
            this.serverTextBox.TabIndex = 0;
            this.serverTextBox.TextChanged += new System.EventHandler(this.ServerTextBox_TextChanged);
            // 
            // databaseTextBox
            // 
            this.databaseTextBox.Location = new System.Drawing.Point(72, 42);
            this.databaseTextBox.Name = "databaseTextBox";
            this.databaseTextBox.Size = new System.Drawing.Size(180, 20);
            this.databaseTextBox.TabIndex = 1;
            this.databaseTextBox.TextChanged += new System.EventHandler(this.DatabaseTextBox_TextChanged);
            // 
            // generateButton
            // 
            this.generateButton.Enabled = false;
            this.generateButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.generateButton.Location = new System.Drawing.Point(480, 268);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(72, 24);
            this.generateButton.TabIndex = 10;
            this.generateButton.Text = "Generate";
            this.generateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            // 
            // sqlGroupBox
            // 
            this.sqlGroupBox.Controls.Add(this.storedProcedurePrefixTextBox);
            this.sqlGroupBox.Controls.Add(this.storedProcedurePrefixLabel);
            this.sqlGroupBox.Controls.Add(this.grantUserTextBox);
            this.sqlGroupBox.Controls.Add(this.grantUserLabel);
            this.sqlGroupBox.Controls.Add(this.multipleFilesCheckBox);
            this.sqlGroupBox.Location = new System.Drawing.Point(264, 6);
            this.sqlGroupBox.Name = "sqlGroupBox";
            this.sqlGroupBox.Size = new System.Drawing.Size(288, 108);
            this.sqlGroupBox.TabIndex = 9;
            this.sqlGroupBox.TabStop = false;
            this.sqlGroupBox.Text = "SQL";
            // 
            // storedProcedurePrefixTextBox
            // 
            this.storedProcedurePrefixTextBox.Location = new System.Drawing.Point(78, 54);
            this.storedProcedurePrefixTextBox.Name = "storedProcedurePrefixTextBox";
            this.storedProcedurePrefixTextBox.Size = new System.Drawing.Size(204, 20);
            this.storedProcedurePrefixTextBox.TabIndex = 7;
            // 
            // storedProcedurePrefixLabel
            // 
            this.storedProcedurePrefixLabel.Location = new System.Drawing.Point(12, 54);
            this.storedProcedurePrefixLabel.Name = "storedProcedurePrefixLabel";
            this.storedProcedurePrefixLabel.Size = new System.Drawing.Size(66, 23);
            this.storedProcedurePrefixLabel.TabIndex = 10;
            this.storedProcedurePrefixLabel.Text = "Prefix:";
            // 
            // grantUserTextBox
            // 
            this.grantUserTextBox.Location = new System.Drawing.Point(78, 24);
            this.grantUserTextBox.Name = "grantUserTextBox";
            this.grantUserTextBox.Size = new System.Drawing.Size(204, 20);
            this.grantUserTextBox.TabIndex = 6;
            // 
            // grantUserLabel
            // 
            this.grantUserLabel.Location = new System.Drawing.Point(12, 24);
            this.grantUserLabel.Name = "grantUserLabel";
            this.grantUserLabel.Size = new System.Drawing.Size(66, 23);
            this.grantUserLabel.TabIndex = 8;
            this.grantUserLabel.Text = "Grant User:";
            // 
            // multipleFilesCheckBox
            // 
            this.multipleFilesCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.multipleFilesCheckBox.Location = new System.Drawing.Point(12, 78);
            this.multipleFilesCheckBox.Name = "multipleFilesCheckBox";
            this.multipleFilesCheckBox.Size = new System.Drawing.Size(216, 24);
            this.multipleFilesCheckBox.TabIndex = 8;
            this.multipleFilesCheckBox.Text = "Create multiple files for stored procedures";
            // 
            // csGroupBox
            // 
            this.csGroupBox.Controls.Add(this.projectTextBox);
            this.csGroupBox.Controls.Add(this.projectLabel);
            this.csGroupBox.Controls.Add(this.dtoSuffixTextBox);
            this.csGroupBox.Controls.Add(this.dtoSuffixLabel);
            this.csGroupBox.Controls.Add(this.daoSuffixTextBox);
            this.csGroupBox.Controls.Add(this.daoSuffixLabel);
            this.csGroupBox.Controls.Add(this.namespaceTextBox);
            this.csGroupBox.Controls.Add(this.namespaceLabel);
            this.csGroupBox.Location = new System.Drawing.Point(264, 126);
            this.csGroupBox.Name = "csGroupBox";
            this.csGroupBox.Size = new System.Drawing.Size(288, 136);
            this.csGroupBox.TabIndex = 10;
            this.csGroupBox.TabStop = false;
            this.csGroupBox.Text = "C#";
            // 
            // projectTextBox
            // 
            this.projectTextBox.Location = new System.Drawing.Point(78, 20);
            this.projectTextBox.Name = "projectTextBox";
            this.projectTextBox.Size = new System.Drawing.Size(204, 20);
            this.projectTextBox.TabIndex = 10;
            // 
            // projectLabel
            // 
            this.projectLabel.Location = new System.Drawing.Point(12, 20);
            this.projectLabel.Name = "projectLabel";
            this.projectLabel.Size = new System.Drawing.Size(72, 23);
            this.projectLabel.TabIndex = 9;
            this.projectLabel.Text = "Project:";
            // 
            // dtoSuffixTextBox
            // 
            this.dtoSuffixTextBox.Location = new System.Drawing.Point(77, 106);
            this.dtoSuffixTextBox.Name = "dtoSuffixTextBox";
            this.dtoSuffixTextBox.Size = new System.Drawing.Size(204, 20);
            this.dtoSuffixTextBox.TabIndex = 16;
            // 
            // dtoSuffixLabel
            // 
            this.dtoSuffixLabel.Location = new System.Drawing.Point(11, 106);
            this.dtoSuffixLabel.Name = "dtoSuffixLabel";
            this.dtoSuffixLabel.Size = new System.Drawing.Size(72, 23);
            this.dtoSuffixLabel.TabIndex = 15;
            this.dtoSuffixLabel.Text = "DTO Suffix:";
            // 
            // daoSuffixTextBox
            // 
            this.daoSuffixTextBox.Location = new System.Drawing.Point(78, 77);
            this.daoSuffixTextBox.Name = "daoSuffixTextBox";
            this.daoSuffixTextBox.Size = new System.Drawing.Size(204, 20);
            this.daoSuffixTextBox.TabIndex = 14;
            // 
            // daoSuffixLabel
            // 
            this.daoSuffixLabel.Location = new System.Drawing.Point(12, 77);
            this.daoSuffixLabel.Name = "daoSuffixLabel";
            this.daoSuffixLabel.Size = new System.Drawing.Size(72, 23);
            this.daoSuffixLabel.TabIndex = 13;
            this.daoSuffixLabel.Text = "DAO Suffix:";
            // 
            // namespaceTextBox
            // 
            this.namespaceTextBox.Location = new System.Drawing.Point(78, 47);
            this.namespaceTextBox.Name = "namespaceTextBox";
            this.namespaceTextBox.Size = new System.Drawing.Size(204, 20);
            this.namespaceTextBox.TabIndex = 12;
            this.namespaceTextBox.TextChanged += new System.EventHandler(this.namespaceTextBox_TextChanged);
            // 
            // namespaceLabel
            // 
            this.namespaceLabel.Location = new System.Drawing.Point(12, 47);
            this.namespaceLabel.Name = "namespaceLabel";
            this.namespaceLabel.Size = new System.Drawing.Size(72, 23);
            this.namespaceLabel.TabIndex = 11;
            this.namespaceLabel.Text = "Namespace:";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 268);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(462, 23);
            this.progressBar.TabIndex = 11;
            // 
            // MainForm
            // 
            this.AcceptButton = this.generateButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(564, 307);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.csGroupBox);
            this.Controls.Add(this.sqlGroupBox);
            this.Controls.Add(this.generateButton);
            this.Controls.Add(this.databaseTextBox);
            this.Controls.Add(this.serverTextBox);
            this.Controls.Add(this.authenticationGroupBox);
            this.Controls.Add(this.databaseLabel);
            this.Controls.Add(this.serverLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "Data Tier Generator";
            this.authenticationGroupBox.ResumeLayout(false);
            this.authenticationGroupBox.PerformLayout();
            this.sqlGroupBox.ResumeLayout(false);
            this.sqlGroupBox.PerformLayout();
            this.csGroupBox.ResumeLayout(false);
            this.csGroupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private void LoginNameTextBox_TextChanged(object sender, System.EventArgs e)
	    {
			EnableGenerateButton();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static void Main()
	    {
			Application.EnableVisualStyles();
			Application.DoEvents();
			Application.Run(new MainForm());
		}

		private void ServerTextBox_TextChanged(object sender, System.EventArgs e)
	    {
			EnableGenerateButton();
		}

		private void SqlServerAuthenticationRadioButton_CheckedChanged(object sender, System.EventArgs e)
	    {
			loginNameLabel.Enabled = true;
			loginNameTextBox.Enabled  = true;
			loginNameTextBox.BackColor = SystemColors.Window;

			passwordLabel.Enabled = true;
			passwordTextBox.Enabled  = true;
			passwordTextBox.BackColor = SystemColors.Window;

			EnableGenerateButton();
		}

		private void WindowsAuthenticationRadioButton_CheckedChanged(object sender, System.EventArgs e)
	    {
			loginNameLabel.Enabled = false;
			loginNameTextBox.Enabled  = false;
			loginNameTextBox.BackColor = SystemColors.InactiveBorder;
			loginNameTextBox.Text = "";

			passwordLabel.Enabled = false;
			passwordTextBox.Enabled  = false;
			passwordTextBox.BackColor = SystemColors.InactiveBorder;
			passwordTextBox.Text = "";

			EnableGenerateButton();
		}

		private void namespaceTextBox_TextChanged(object sender, System.EventArgs e)
	    {
			EnableGenerateButton();
		}

		private void Generator_DatabaseCounted(object sender, CountEventArgs e)
	    {
			progressBar.Maximum = e.Count;
		}

		private void Generator_TableCounted(object sender, CountEventArgs e)
	    {
			progressBar.Value = e.Count;
		}
	}
}
