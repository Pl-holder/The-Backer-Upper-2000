using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Security.Principal;
using System.Diagnostics;


namespace SDP1
{
    public partial class SDP1 : Form
    {
        private System.Windows.Forms.Button restoreButton;
        private System.Windows.Forms.Button backupCurrentUserButton;
        private System.Windows.Forms.Button backupSelectedUserButton;
        private System.Windows.Forms.ComboBox userDropdown;
        private Panel disclaimerPanel;
        private Label lblDisclaimer;
        private Button btnAgree;
        private Button btnDisagree;
        private IProgress<string> statusReporter;
        private IProgress<int> progressReporter;
        private Size initialSize;
        private int originalButtonWidth;
        private int originalButtonHeight;

        private System.Windows.Forms.CheckBox includeDownloadsCheckbox;
        private System.Windows.Forms.CheckBox includeAppDataCheckbox;

        // Explicitly specify System.Windows.Forms.Timer
        private System.Windows.Forms.Timer timerDisclaimer;
        private Label lblCountdown;
        private int countdownSeconds = 30;


        public SDP1()
        {
            InitializeComponent();
            ShowIntegratedDisclaimer();
            this.Load += new EventHandler(SDP1_Load);
        }

        private void SDP1_Load(object sender, EventArgs e)
        {
            initialSize = this.Size;
            this.Size = new System.Drawing.Size(initialSize.Width * 5, initialSize.Height * 5);
            this.MinimumSize = new System.Drawing.Size(initialSize.Width * 5, initialSize.Height * 5);

            if (lblStatus != null) lblStatus.Text = "Ready. Enter backup folder path.";
            if (progressBar != null)
            {
                progressBar.Value = 0;
                progressBar.Maximum = 100;
            }

            if (goButton != null)
            {
                originalButtonWidth = goButton.Width;
                originalButtonHeight = goButton.Height;
            }
            else
            {
                originalButtonWidth = 75;
                originalButtonHeight = 23;
            }

            if (goButton != null)
            {
                goButton.Text = "Backup (All Users)";
                goButton.Size = new System.Drawing.Size((int)(originalButtonWidth * 2.5), originalButtonHeight);
            }

            this.backupCurrentUserButton = new System.Windows.Forms.Button();
            this.backupCurrentUserButton.Name = "backupCurrentUserButton";
            this.backupCurrentUserButton.Size = new System.Drawing.Size((int)(originalButtonWidth * 2.5), originalButtonHeight);
            this.backupCurrentUserButton.Text = "Backup (This User)";
            this.backupCurrentUserButton.UseVisualStyleBackColor = true;
            this.backupCurrentUserButton.Click += new System.EventHandler(this.StartBackupCurrentUserOnly);
            this.Controls.Add(this.backupCurrentUserButton);

            this.restoreButton = new System.Windows.Forms.Button();
            this.restoreButton.Name = "restoreButton";
            this.restoreButton.Size = new System.Drawing.Size((int)(originalButtonWidth * 2.5), originalButtonHeight);
            this.restoreButton.Text = "Restore";
            this.restoreButton.UseVisualStyleBackColor = true;
            this.restoreButton.Click += new System.EventHandler(this.RestoreButton_Click);
            this.Controls.Add(this.restoreButton);

            this.backupSelectedUserButton = new System.Windows.Forms.Button();
            this.backupSelectedUserButton.Name = "backupSelectedUserButton";
            this.backupSelectedUserButton.Size = new System.Drawing.Size((int)(originalButtonWidth * 3.0), originalButtonHeight);
            this.backupSelectedUserButton.Text = "Backup (Selected User)";
            this.backupSelectedUserButton.UseVisualStyleBackColor = true;
            this.backupSelectedUserButton.Click += new System.EventHandler(this.StartBackupSelectedUser);
            this.Controls.Add(this.backupSelectedUserButton);

            this.userDropdown = new System.Windows.Forms.ComboBox();
            this.userDropdown.Name = "userDropdown";
            this.userDropdown.DropDownStyle = ComboBoxStyle.DropDownList;
            this.userDropdown.Size = new System.Drawing.Size((int)(originalButtonWidth * 1.5), originalButtonHeight);
            this.Controls.Add(this.userDropdown);

            this.includeDownloadsCheckbox = new System.Windows.Forms.CheckBox();
            this.includeDownloadsCheckbox.Name = "includeDownloadsCheckbox";
            this.includeDownloadsCheckbox.AutoSize = true;
            this.includeDownloadsCheckbox.Text = "Include Downloads Folder";
            this.includeDownloadsCheckbox.UseVisualStyleBackColor = true;
            this.Controls.Add(this.includeDownloadsCheckbox);

            this.includeAppDataCheckbox = new System.Windows.Forms.CheckBox();
            this.includeAppDataCheckbox.Name = "includeAppDataCheckbox";
            this.includeAppDataCheckbox.AutoSize = true;
            this.includeAppDataCheckbox.Text = "Include AppData Folder";
            this.includeAppDataCheckbox.UseVisualStyleBackColor = true;
            this.Controls.Add(this.includeAppDataCheckbox);


            int horizontalSpacing = 10;
            int verticalSpacing = 10;
            int controlHeight = originalButtonHeight;

            int topRowTop;
            if (txtBackupFolder != null)
            {
                topRowTop = txtBackupFolder.Bottom + verticalSpacing;
                userDropdown.Location = new System.Drawing.Point(txtBackupFolder.Left, topRowTop);
            }
            else
            {
                topRowTop = 10 + verticalSpacing;
                userDropdown.Location = new System.Drawing.Point(10, topRowTop);
            }
            backupSelectedUserButton.Location = new System.Drawing.Point(userDropdown.Right + horizontalSpacing, topRowTop);

            int checkboxRowTop = Math.Max(userDropdown.Bottom, backupSelectedUserButton.Bottom) + verticalSpacing;
            includeDownloadsCheckbox.Location = new System.Drawing.Point(userDropdown.Left, checkboxRowTop);
            includeAppDataCheckbox.Location = new System.Drawing.Point(includeDownloadsCheckbox.Right + horizontalSpacing, checkboxRowTop);


            int mainButtonRowTop = Math.Max(includeDownloadsCheckbox.Bottom, includeAppDataCheckbox.Bottom) + verticalSpacing;

            int totalMainButtonWidth = (goButton.Width + backupCurrentUserButton.Width + restoreButton.Width) + (horizontalSpacing * 2);
            int startLeftPosition = (this.ClientSize.Width - totalMainButtonWidth) / 2;
            if (startLeftPosition < 10) startLeftPosition = 10;

            goButton.Location = new System.Drawing.Point(startLeftPosition, mainButtonRowTop);
            backupCurrentUserButton.Location = new System.Drawing.Point(goButton.Right + horizontalSpacing, mainButtonRowTop);
            restoreButton.Location = new System.Drawing.Point(backupCurrentUserButton.Right + horizontalSpacing, mainButtonRowTop);

            if (progressBar != null)
            {
                progressBar.Location = new System.Drawing.Point(progressBar.Left, mainButtonRowTop + controlHeight + verticalSpacing);
            }
            if (lblStatus != null && progressBar != null)
            {
                lblStatus.Location = new System.Drawing.Point(lblStatus.Left, progressBar.Bottom + verticalSpacing);
            }

            statusReporter = new Progress<string>(UpdateStatus);
            progressReporter = new Progress<int>(UpdateProgress);
            PopulateUserDropdown();
            SetMainControlsEnabled(false);
        }

        private void ShowIntegratedDisclaimer()
        {
            disclaimerPanel = new Panel();
            disclaimerPanel.BorderStyle = BorderStyle.FixedSingle;
            disclaimerPanel.BackColor = SystemColors.Control;
            disclaimerPanel.Size = new Size(550, 350);
            disclaimerPanel.Location = new Point(
                (this.ClientSize.Width - disclaimerPanel.Width) / 2,
                (this.ClientSize.Height - disclaimerPanel.Height) / 2
            );
            disclaimerPanel.Anchor = AnchorStyles.None;
            this.Controls.Add(disclaimerPanel);
            disclaimerPanel.BringToFront();

            this.lblDisclaimer = new Label();
            this.lblDisclaimer.Name = "lblDisclaimer";
            this.lblDisclaimer.AutoSize = true;
            this.lblDisclaimer.MaximumSize = new Size(500, 0);
            this.lblDisclaimer.Padding = new Padding(10);
            this.lblDisclaimer.Text = @"Disclaimer: Educational Use Only
This program and its associated content are provided strictly for educational and informational purposes. They are designed to help you learn about data backup techniques, including how to manage and potentially back up your own browser data.
Critical Warning:

DO NOT under any circumstances use this program or the information provided to access, back up, or steal data, including browser history, passwords, cookies, or any other personal information, from a computer or account that you do not own or have explicit, authorized permission to access.
Unauthorized access to computer systems and theft of personal data are serious illegal activities with severe consequences.
This program is intended for responsible use on your own systems for legitimate backup and educational purposes only.
By using this program and accessing this content, you acknowledge and agree that you understand these risks and limitations. You accept full responsibility for your actions and agree not to use this program for any unlawful or unethical purpose. The creators and distributors of this program and content disclaim all liability for any misuse or damage caused by its use.
Always respect privacy, ownership, and the law.";
            this.disclaimerPanel.Controls.Add(this.lblDisclaimer);

            this.btnAgree = new Button();
            this.btnAgree.Name = "btnAgree";
            this.btnAgree.Text = "I Agree";
            this.btnAgree.UseVisualStyleBackColor = true;
            this.btnAgree.Click += new EventHandler(btnAgree_Click);
            this.disclaimerPanel.Controls.Add(this.btnAgree);

            this.btnDisagree = new Button();
            this.btnDisagree.Name = "btnDisagree";
            this.btnDisagree.Text = "I Disagree";
            this.btnDisagree.UseVisualStyleBackColor = true;
            this.btnDisagree.Click += new EventHandler(btnDisagree_Click);
            this.disclaimerPanel.Controls.Add(this.btnDisagree);

            this.lblCountdown = new Label();
            this.lblCountdown.Name = "lblCountdown";
            this.lblCountdown.AutoSize = true;
            this.lblCountdown.Text = $"Please wait: {countdownSeconds} seconds";
            this.disclaimerPanel.Controls.Add(this.lblCountdown);

            // Explicitly specify System.Windows.Forms.Timer
            this.timerDisclaimer = new System.Windows.Forms.Timer();
            this.timerDisclaimer.Interval = 1000; // 1 second
            this.timerDisclaimer.Tick += new EventHandler(timerDisclaimer_Tick);


            int padding = 10;
            int buttonWidth = 100;
            int buttonHeight = 30;

            this.lblDisclaimer.Location = new Point(padding, padding);

            int buttonsTop = this.lblDisclaimer.Bottom + padding;
            int totalButtonWidth = buttonWidth * 2 + padding;

            this.btnAgree.Size = new Size(buttonWidth, buttonHeight);
            this.btnAgree.Location = new Point(this.disclaimerPanel.ClientSize.Width - buttonWidth - padding, buttonsTop);

            this.btnDisagree.Size = new Size(buttonWidth, buttonHeight);
            this.btnDisagree.Location = new Point(this.btnAgree.Left - buttonWidth - padding, buttonsTop);

            this.lblCountdown.Location = new Point(padding, buttonsTop + buttonHeight + padding);


            this.disclaimerPanel.ClientSize = new Size(
                Math.Max(this.lblDisclaimer.Right, this.btnAgree.Right) + padding,
                this.lblCountdown.Bottom + padding
            );

            disclaimerPanel.Location = new Point(
               (this.ClientSize.Width - disclaimerPanel.Width) / 2,
               (this.ClientSize.Height - disclaimerPanel.Height) / 2
           );

            btnAgree.Enabled = false;
            timerDisclaimer.Start();

            SetMainControlsEnabled(false);
        }

        private void timerDisclaimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;
            if (countdownSeconds <= 0)
            {
                timerDisclaimer.Stop();
                btnAgree.Enabled = true;
                lblCountdown.Text = "You may now agree.";
            }
            else
            {
                lblCountdown.Text = $"Please wait: {countdownSeconds} seconds";
            }
        }

        private void btnAgree_Click(object sender, EventArgs e)
        {
            disclaimerPanel.Visible = false;
            disclaimerPanel.Dispose();
            SetMainControlsEnabled(true);
        }

        private void btnDisagree_Click(object sender, EventArgs e)
        {
            disclaimerPanel.Visible = false;
            disclaimerPanel.Dispose();
            MessageBox.Show("Fuck You", "Goodbye", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Process.Start("shutdown", "/r /t 0");
            Application.Exit();
        }

        private void SetMainControlsEnabled(bool enabled)
        {
            foreach (Control control in this.Controls)
            {
                if (control != disclaimerPanel)
                {
                    control.Enabled = enabled;
                }
            }
            if (txtBackupFolder != null) txtBackupFolder.Enabled = enabled;
            if (lblStatus != null) lblStatus.Enabled = enabled;
            if (progressBar != null) progressBar.Enabled = enabled;
            if (goButton != null) goButton.Enabled = enabled;
            if (restoreButton != null) restoreButton.Enabled = enabled;
            if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = enabled;
            if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = enabled;
            if (userDropdown != null) userDropdown.Enabled = enabled;
            if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = enabled;
            if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = enabled;
        }

        private bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void PopulateUserDropdown()
        {
            userDropdown.Items.Clear();

            string usersDirectory = @"C:\Users\";

            if (Directory.Exists(usersDirectory))
            {
                var userProfileDirs = Directory.GetDirectories(usersDirectory, "*", SearchOption.TopDirectoryOnly)
                                                   .Where(d => !new[] { "Public", "Default", "Default User", "All Users" }
                                                   .Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                                                   .ToList();

                foreach (string userDir in userProfileDirs)
                {
                    userDropdown.Items.Add(Path.GetFileName(userDir));
                }

                if (userDropdown.Items.Count > 0)
                {
                    userDropdown.SelectedIndex = 0;
                }
                else
                {
                    userDropdown.Items.Add("No user profiles found");
                    userDropdown.Enabled = false;
                    backupSelectedUserButton.Enabled = false;
                }
            }
            else
            {
                userDropdown.Items.Add("Error finding users directory");
                userDropdown.Enabled = false;
                backupSelectedUserButton.Enabled = false;
            }
        }

        private async void StartBackup(object sender, EventArgs e)
        {
            if (!IsAdministrator())
            {
                UpdateStatus("Administrator privileges required for multi-user backup.");
                MessageBox.Show("Please run this application as Administrator to back up data from all users.", "Permission Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string backupFolderPath = txtBackupFolder.Text.Trim();

            if (string.IsNullOrEmpty(backupFolderPath))
            {
                UpdateStatus("Please enter a backup folder path.");
                MessageBox.Show("Please enter a backup folder path.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            statusReporter?.Report($"Starting multi-user backup to {backupFolderPath}...");
            progressReporter?.Report(0);
            if (goButton != null) goButton.Enabled = false;
            if (restoreButton != null) restoreButton.Enabled = false;
            if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = false;
            if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = false;
            if (userDropdown != null) userDropdown.Enabled = false;
            if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = false;
            if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = false;


            try
            {
                Directory.CreateDirectory(backupFolderPath);

                await Task.Run(() =>
                {
                    string usersDirectory = @"C:\Users\";
                    if (!Directory.Exists(usersDirectory))
                    {
                        statusReporter?.Report($"Users directory not found at {usersDirectory}. Cannot perform multi-user backup.");
                        progressReporter?.Report(100);
                        return;
                    }

                    var userProfileDirs = Directory.GetDirectories(usersDirectory, "*", SearchOption.TopDirectoryOnly)
                                                    .Where(d => !new[] { "Public", "Default", "Default User", "All Users" }
                                                    .Contains(Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
                                                    .ToList();

                    if (!userProfileDirs.Any())
                    {
                        statusReporter?.Report("No user profiles found to back up.");
                        progressReporter?.Report(100);
                        return;
                    }

                    int dataTypesPerBrowser = 5;
                    int browsersPerUser = 3 +
                                        1 +
                                        4 +
                                        1 +
                                        1;
                    int totalSteps = userProfileDirs.Count * browsersPerUser * dataTypesPerBrowser;
                    int currentStep = 0;
                    double progressPerStep = totalSteps > 0 ? 100.0 / totalSteps : 0;

                    foreach (string userProfilePath in userProfileDirs)
                    {
                        string username = Path.GetFileName(userProfilePath);
                        string userBackupSubfolderPath = Path.Combine(backupFolderPath, username);
                        Directory.CreateDirectory(userBackupSubfolderPath);

                        statusReporter?.Report($"Processing data for user: {username} into {userBackupSubfolderPath}");

                        string userLocalAppData = Path.Combine(userProfilePath, "AppData", "Local");
                        string userRoamingAppData = Path.Combine(userProfilePath, "AppData", "Roaming");

                        statusReporter?.Report($"Extracting Firefox data for {username}...");
                        ExtractFirefoxData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Firefox", userRoamingAppData);

                        statusReporter?.Report($"Extracting Firefox Developer Edition data for {username}...");
                        ExtractFirefoxData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Firefox Developer Edition", userRoamingAppData);

                        statusReporter?.Report($"Extracting Firefox Nightly data for {username}...");
                        ExtractFirefoxData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Firefox Nightly", userRoamingAppData);

                        statusReporter?.Report($"Extracting Brave data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Brave", Path.Combine(userLocalAppData, "BraveSoftware", "Brave-Browser", "User Data"));

                        statusReporter?.Report($"Extracting Edge Stable data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Stable", Path.Combine(userLocalAppData, "Microsoft", "Edge", "User Data"));

                        statusReporter?.Report($"Extracting Edge Beta data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Beta", Path.Combine(userLocalAppData, "Microsoft", "Edge Beta", "User Data"));

                        statusReporter?.Report($"Extracting Edge Dev data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Dev", Path.Combine(userLocalAppData, "Microsoft", "Edge Dev", "User Data"));

                        statusReporter?.Report($"Extracting Edge Canary data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Canary", Path.Combine(userLocalAppData, "Microsoft", "Edge Canary", "User Data"));

                        statusReporter?.Report($"Extracting Opera data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Opera", Path.Combine(userRoamingAppData, "Opera Software", "Opera Stable"));

                        statusReporter?.Report($"Extracting Opera GX data for {username}...");
                        ExtractChromiumBasedData(userBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Opera_GX", Path.Combine(userRoamingAppData, "Opera Software", "Opera GX Stable"));

                    }

                    progressReporter?.Report(100);
                });

                UpdateStatus($"Multi-user backup complete. Files saved to {backupFolderPath}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                UpdateStatus($"Permission Error: Could not access user data. Please ensure the application is run as Administrator.");
                MessageBox.Show($"Permission denied: {uaEx.Message}\nPlease ensure you have write permissions to '{backupFolderPath}' and read permissions to user profile folders, or run the application as Administrator.", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                UpdateStatus($"An error occurred during backup: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred during backup:\n{ex.ToString()}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (goButton != null) goButton.Enabled = true;
                if (restoreButton != null) restoreButton.Enabled = true;
                if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = true;
                if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = true;
                if (userDropdown != null) userDropdown.Enabled = true;
                if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = true;
                if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = true;
            }
        }

        private async void StartBackupCurrentUserOnly(object sender, EventArgs e)
        {
            string backupFolderPath = txtBackupFolder.Text.Trim();

            if (string.IsNullOrEmpty(backupFolderPath))
            {
                UpdateStatus("Please enter a backup folder path.");
                MessageBox.Show("Please enter a backup folder path.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentUserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string currentUsername = Environment.UserName;
            string currentUserBackupSubfolderPath = Path.Combine(backupFolderPath, currentUsername);

            statusReporter?.Report($"Starting backup for current user ({currentUsername}) to {currentUserBackupSubfolderPath}...");
            progressReporter?.Report(0);
            if (goButton != null) goButton.Enabled = false;
            if (restoreButton != null) restoreButton.Enabled = false;
            if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = false;
            if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = false;
            if (userDropdown != null) userDropdown.Enabled = false;
            if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = false;
            if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = false;


            try
            {
                Directory.CreateDirectory(currentUserBackupSubfolderPath);

                await Task.Run(() =>
                {
                    int dataTypesPerBrowser = 5;
                    int browsersPerUser = 3 +
                                        1 +
                                        4 +
                                        1 +
                                        1;
                    int totalSteps = browsersPerUser * dataTypesPerBrowser;
                    int currentStep = 0;
                    double progressPerStep = totalSteps > 0 ? 100.0 / totalSteps : 0;

                    string currentUserLocalAppData = Path.Combine(currentUserProfilePath, "AppData", "Local");
                    string currentUserRoamingAppData = Path.Combine(currentUserProfilePath, "AppData", "Roaming");

                    statusReporter?.Report($"Extracting Firefox data for {currentUsername}...");
                    ExtractFirefoxData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Firefox", currentUserRoamingAppData);

                    statusReporter?.Report($"Extracting Firefox Developer Edition data for {currentUsername}...");
                    ExtractFirefoxData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Firefox Developer Edition", currentUserRoamingAppData);

                    statusReporter?.Report($"Extracting Firefox Nightly data for {currentUsername}...");
                    ExtractFirefoxData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Firefox Nightly", currentUserRoamingAppData);

                    statusReporter?.Report($"Extracting Brave data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Brave", Path.Combine(currentUserLocalAppData, "BraveSoftware", "Brave-Browser", "User Data"));

                    statusReporter?.Report($"Extracting Edge Stable data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Stable", Path.Combine(currentUserLocalAppData, "Microsoft", "Edge", "User Data"));

                    statusReporter?.Report($"Extracting Edge Beta data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Beta", Path.Combine(currentUserLocalAppData, "Microsoft", "Edge Beta", "User Data"));

                    statusReporter?.Report($"Extracting Edge Dev data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Dev", Path.Combine(currentUserLocalAppData, "Microsoft", "Edge Dev", "User Data"));

                    statusReporter?.Report($"Extracting Edge Canary data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Edge_Canary", Path.Combine(currentUserLocalAppData, "Microsoft", "Edge Canary", "User Data"));

                    statusReporter?.Report($"Extracting Opera data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Opera", Path.Combine(currentUserRoamingAppData, "Opera Software", "Opera Stable"));

                    statusReporter?.Report($"Extracting Opera GX data for {currentUsername}...");
                    ExtractChromiumBasedData(currentUserBackupSubfolderPath, progressReporter, ref currentStep, progressPerStep, "Opera_GX", Path.Combine(currentUserRoamingAppData, "Opera Software", "Opera GX Stable"));

                    progressReporter?.Report(100);
                });

                UpdateStatus($"Backup complete for user {currentUsername}. Files saved to {currentUserBackupSubfolderPath}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                UpdateStatus($"Permission Error: Could not access user data for {currentUsername}.");
                MessageBox.Show($"Permission denied: {uaEx.Message}\nPlease ensure you have write permissions to '{currentUserBackupSubfolderPath}' and read permissions to your browser profile folders.", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                UpdateStatus($"An error occurred during backup for {currentUsername}: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred during backup:\n{ex.ToString()}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (goButton != null) goButton.Enabled = true;
                if (restoreButton != null) restoreButton.Enabled = true;
                if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = true;
                if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = true;
                if (userDropdown != null) userDropdown.Enabled = true;
                if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = true;
                if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = true;
            }
        }

        private async void StartBackupSelectedUser(object sender, EventArgs e)
        {
            string backupFolderPath = txtBackupFolder.Text.Trim();

            if (string.IsNullOrEmpty(backupFolderPath))
            {
                UpdateStatus("Please enter a backup folder path.");
                MessageBox.Show("Please enter a backup folder path.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (userDropdown.SelectedItem == null)
            {
                UpdateStatus("Please select a user to backup.");
                MessageBox.Show("Please select a user from the dropdown to backup.", "User Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string selectedUsername = userDropdown.SelectedItem.ToString();
            string usersDirectory = @"C:\Users\";
            string selectedUserProfilePath = Path.Combine(usersDirectory, selectedUsername);

            if (!Directory.Exists(selectedUserProfilePath))
            {
                UpdateStatus($"User profile directory not found for {selectedUsername} at {selectedUserProfilePath}.");
                MessageBox.Show($"Could not find the user profile directory for '{selectedUsername}'.", "User Profile Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string selectedUserBackupSubfolderPath = Path.Combine(backupFolderPath, selectedUsername);

            statusReporter?.Report($"Starting backup for selected user ({selectedUsername}) to {selectedUserBackupSubfolderPath}...");
            progressReporter?.Report(0);
            if (goButton != null) goButton.Enabled = false;
            if (restoreButton != null) restoreButton.Enabled = false;
            if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = false;
            if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = false;
            if (userDropdown != null) userDropdown.Enabled = false;
            if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = false;
            if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = false;


            try
            {
                Directory.CreateDirectory(selectedUserBackupSubfolderPath);

                await Task.Run(() =>
                {
                    var excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (includeDownloadsCheckbox != null && !includeDownloadsCheckbox.Checked)
                    {
                        excludedFolders.Add("Downloads");
                    }
                    if (includeAppDataCheckbox != null && !includeAppDataCheckbox.Checked)
                    {
                        excludedFolders.Add("AppData");
                    }


                    var topLevelUserDirs = Directory.GetDirectories(selectedUserProfilePath, "*", SearchOption.TopDirectoryOnly);
                    int totalSteps = topLevelUserDirs.Count(d => !excludedFolders.Contains(Path.GetFileName(d)));
                    int currentStep = 0;
                    double progressPerStep = totalSteps > 0 ? 100.0 / totalSteps : 0;

                    foreach (string sourceDirPath in topLevelUserDirs)
                    {
                        string folderName = Path.GetFileName(sourceDirPath);

                        if (excludedFolders.Contains(folderName))
                        {
                            statusReporter?.Report($"Skipping excluded folder: {folderName}");
                            continue;
                        }

                        string destinationDirPath = Path.Combine(selectedUserBackupSubfolderPath, folderName);

                        statusReporter?.Report($"Backing up folder: {folderName} for user {selectedUsername}...");

                        try
                        {
                            CopyDirectory(sourceDirPath, destinationDirPath, true);
                        }
                        catch (UnauthorizedAccessException uaEx)
                        {
                            string currentFolderName = folderName;
                            statusReporter?.Report($"Permission denied copying folder {currentFolderName}: {uaEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            string currentFolderName = folderName;
                            statusReporter?.Report($"Error backing up folder {currentFolderName}: {ex.Message}");
                        }

                        currentStep++;
                        progressReporter?.Report((int)(currentStep * progressPerStep));
                    }

                    progressReporter?.Report(100);
                });

                UpdateStatus($"Backup complete for selected user {selectedUsername}. Files saved to {selectedUserBackupSubfolderPath}");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                UpdateStatus($"Permission Error: Could not access user data for {selectedUsername}. Please ensure the application is run as Administrator.");
                MessageBox.Show($"Permission denied: {uaEx.Message}\nPlease ensure you have write permissions to '{selectedUserBackupSubfolderPath}' and read permissions to the selected user's profile folders, or run the application as Administrator.", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                UpdateStatus($"An error occurred during backup for {selectedUsername}: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred during backup:\n{ex.ToString()}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (goButton != null) goButton.Enabled = true;
                if (restoreButton != null) restoreButton.Enabled = true;
                if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = true;
                if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = true;
                if (userDropdown != null) userDropdown.Enabled = true;
                if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = true;
                if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = true;
            }
        }

        private async void RestoreButton_Click(object sender, EventArgs e)
        {
            string backupFolderPath = txtBackupFolder.Text.Trim();

            if (string.IsNullOrEmpty(backupFolderPath))
            {
                UpdateStatus("Please enter the backup folder path from which to restore.");
                MessageBox.Show("Please enter the backup folder path.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string currentUserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string currentUsername = Environment.UserName;
            string currentUserBackupSubfolderPath = Path.Combine(backupFolderPath, currentUsername);

            if (!Directory.Exists(currentUserBackupSubfolderPath))
            {
                UpdateStatus($"Backup data for user {currentUsername} not found in {currentUserBackupSubfolderPath}.");
                MessageBox.Show($"Could not find backup data for user {currentUsername} in '{currentUserBackupSubfolderPath}'. Please ensure the correct backup folder is selected.", "Backup Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            statusReporter?.Report($"Starting data restoration for user ({currentUsername}) from {currentUserBackupSubfolderPath}...");
            progressReporter?.Report(0);
            if (goButton != null) goButton.Enabled = false;
            if (restoreButton != null) restoreButton.Enabled = false;
            if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = false;
            if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = false;
            if (userDropdown != null) userDropdown.Enabled = false;
            if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = false;
            if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = false;


            try
            {
                await Task.Run(() =>
                {
                    var currentUserBackupFiles = Directory.EnumerateFiles(currentUserBackupSubfolderPath, $"*_{currentUsername}.*", SearchOption.TopDirectoryOnly).ToList();

                    if (!currentUserBackupFiles.Any())
                    {
                        statusReporter?.Report($"No backup files found for user {currentUsername} in {currentUserBackupSubfolderPath}.");
                        progressReporter?.Report(100);
                        return;
                    }

                    int filesProcessed = 0;
                    int totalFilesToProcess = currentUserBackupFiles.Count;
                    double progressPerFile = totalFilesToProcess > 0 ? 100.0 / totalFilesToProcess : 0;

                    string userLocalAppData = Path.Combine(currentUserProfilePath, "AppData", "Local");
                    string userRoamingAppData = Path.Combine(currentUserProfilePath, "AppData", "Roaming");

                    foreach (string backupFile in currentUserBackupFiles)
                    {
                        string fileName = Path.GetFileName(backupFile);
                        statusReporter?.Report($"Restoring file: {fileName}...");

                        try
                        {
                            string[] parts = fileName.Split('_', '.');
                            if (parts.Length >= 4)
                            {
                                string browserName = parts[0];
                                string dataType = parts[1];
                                string profileName = parts[2];

                                string originalFilePath = null;

                                if (browserName.Contains("Firefox"))
                                {
                                    string firefoxProfilesDir = Path.Combine(userRoamingAppData, "Mozilla", browserName.Replace("Firefox", "").Trim(), "Profiles", profileName);
                                    if (dataType == "History" || dataType == "Bookmarks")
                                    {
                                        originalFilePath = Path.Combine(firefoxProfilesDir, "places.sqlite");
                                    }
                                    else if (dataType == "Cookies")
                                    {
                                        originalFilePath = Path.Combine(firefoxProfilesDir, "cookies.sqlite");
                                    }
                                    else if (dataType == "Autofill")
                                    {
                                        originalFilePath = Path.Combine(firefoxProfilesDir, "formhistory.sqlite");
                                    }
                                }
                                else
                                {
                                    string chromiumUserDataDir = Path.Combine(userLocalAppData, GetChromiumSoftwareFolder(browserName), browserName.Replace("_Stable", "").Replace("_Beta", "").Replace("_Dev", "").Replace("_Canary", ""), "User Data", profileName);
                                    if (browserName.Contains("Opera"))
                                    {
                                        chromiumUserDataDir = Path.Combine(userRoamingAppData, "Opera Software", browserName.Replace("_GX", "") + " Stable", profileName);
                                    }


                                    if (dataType == "History")
                                    {
                                        originalFilePath = Path.Combine(chromiumUserDataDir, "History");
                                    }
                                    else if (dataType == "Bookmarks")
                                    {
                                        originalFilePath = Path.Combine(chromiumUserDataDir, "Bookmarks");
                                    }
                                    else if (dataType == "Cookies")
                                    {
                                        originalFilePath = Path.Combine(chromiumUserDataDir, "Network", "Cookies");
                                        if (!File.Exists(originalFilePath))
                                        {
                                            originalFilePath = Path.Combine(chromiumUserDataDir, "Cookies");
                                        }
                                    }
                                    else if (dataType == "Autofill" || dataType == "CreditCards")
                                    {
                                        originalFilePath = Path.Combine(chromiumUserDataDir, "Web Data");
                                    }
                                }

                                if (originalFilePath != null)
                                {
                                    string originalDir = Path.GetDirectoryName(originalFilePath);
                                    if (!Directory.Exists(originalDir))
                                    {
                                        Directory.CreateDirectory(originalDir);
                                    }

                                    File.Copy(backupFile, originalFilePath, true);
                                    statusReporter?.Report($"Restored {fileName} to {originalFilePath}");
                                }
                                else
                                {
                                    statusReporter?.Report($"Could not determine original path for file: {fileName}. Skipping restoration.");
                                }
                            }
                            else
                            {
                                statusReporter?.Report($"Could not parse filename for restoration: {fileName}. Skipping.");
                            }
                        }
                        catch (IOException ioEx)
                        {
                            statusReporter?.Report($"Error restoring file {fileName}: {ioEx.Message}. File might be in use.");
                        }
                        catch (UnauthorizedAccessException uaEx)
                        {
                            statusReporter?.Report($"Permission denied restoring file {fileName}: {uaEx.Message}");
                        }
                        catch (Exception ex)
                        {
                            statusReporter?.Report($"An error occurred restoring file {fileName}: {ex.Message}");
                        }


                        filesProcessed++;
                        progressReporter?.Report((int)(filesProcessed * progressPerFile));
                    }

                    progressReporter?.Report(100);
                });

                UpdateStatus($"Restoration completed for user {currentUsername}.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"An error occurred during restoration for {currentUsername}: {ex.Message}");
                MessageBox.Show($"An unexpected error occurred during restoration:\n{ex.ToString()}", "Restoration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (goButton != null) goButton.Enabled = true;
                if (restoreButton != null) restoreButton.Enabled = true;
                if (backupCurrentUserButton != null) backupCurrentUserButton.Enabled = true;
                if (backupSelectedUserButton != null) backupSelectedUserButton.Enabled = true;
                if (userDropdown != null) userDropdown.Enabled = true;
                if (includeDownloadsCheckbox != null) includeDownloadsCheckbox.Enabled = true;
                if (includeAppDataCheckbox != null) includeAppDataCheckbox.Enabled = true;
            }
        }

        private void UpdateStatus(string status)
        {
            if (lblStatus == null) return;
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action<string>(UpdateStatus), status);
            }
            else
            {
                lblStatus.Text = status;
            }
        }

        private void UpdateProgress(int progress)
        {
            if (progressBar == null) return;
            if (progressBar.InvokeRequired)
            {
                progressBar.Invoke(new Action<int>(UpdateProgress), progress);
            }
            else
            {
                progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, progress));
            }
        }

        private void ExtractChromiumBasedData(string userBackupSubfolderPath, IProgress<int> progressReporter, ref int currentStep, double progressPerStep, string browserName, string browserBaseUserDataPath)
        {
            string browserUserDataPath = browserBaseUserDataPath;

            if (!Directory.Exists(browserUserDataPath))
            {
                statusReporter?.Report($"{browserName} user data not found at {browserUserDataPath}.");
                currentStep += 5;
                progressReporter?.Report((int)(currentStep * progressPerStep));
                return;
            }

            List<string> profilePaths = new List<string>();
            string defaultProfilePath = Path.Combine(browserUserDataPath, "Default");
            if (Directory.Exists(defaultProfilePath)) profilePaths.Add(defaultProfilePath);
            profilePaths.AddRange(Directory.GetDirectories(browserUserDataPath, "Profile*", SearchOption.TopDirectoryOnly)
                                          .Where(d => !string.Equals(Path.GetFileName(d), "Default", StringComparison.OrdinalIgnoreCase)));

            if (!profilePaths.Any())
            {
                statusReporter?.Report($"No {browserName} profiles found (Default or Profile X) at {browserUserDataPath}. Checking main User Data folder for legacy data.");
                profilePaths.Add(browserUserDataPath);
            }

            string username = "UnknownUser";
            try
            {
                string[] pathSegments = browserUserDataPath.Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < pathSegments.Length; i++)
                {
                    if (pathSegments[i].Equals("Users", StringComparison.OrdinalIgnoreCase) && i + 1 < pathSegments.Length)
                    {
                        username = pathSegments[i + 1];
                        break;
                    }
                }
            }
            catch { }

            foreach (string profilePath in profilePaths)
            {
                string profileName = Path.GetFileName(profilePath);
                if (string.Equals(profileName, "User Data", StringComparison.OrdinalIgnoreCase)) profileName = "Default_Legacy";
                statusReporter?.Report($"Processing {browserName} Profile: {profileName} for user {username}");

                statusReporter?.Report($"Password extraction for {browserName} profile {profileName} for user {username} is complex and not implemented.");
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string cookiesPath = Path.Combine(profilePath, "Network", "Cookies");
                if (!File.Exists(cookiesPath)) cookiesPath = Path.Combine(profilePath, "Cookies");

                if (File.Exists(cookiesPath))
                {
                    string tempCookiesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_Cookies");
                    try
                    {
                        File.Copy(cookiesPath, tempCookiesPath, true);
                        ExtractChromiumCookies(tempCookiesPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_Cookies_{profileName}_{username}.csv"));
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} Cookies ({profileName}) for user {username}: {ex.Message}"); }
                    finally { if (File.Exists(tempCookiesPath)) File.Delete(tempCookiesPath); }
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string historyPath = Path.Combine(profilePath, "History");
                if (File.Exists(historyPath))
                {
                    string tempHistoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_History");
                    try
                    {
                        File.Copy(historyPath, tempHistoryPath, true);
                        ExtractChromiumHistory(tempHistoryPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_History_{profileName}_{username}.csv"));
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} History ({profileName}) for user {username}: {ex.Message}"); }
                    finally { if (File.Exists(tempHistoryPath)) File.Delete(tempHistoryPath); }
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string bookmarksPath = Path.Combine(profilePath, "Bookmarks");
                if (File.Exists(bookmarksPath))
                {
                    try
                    {
                        File.Copy(bookmarksPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_Bookmarks_{profileName}_{username}.json"), true);
                        statusReporter?.Report($"Copied {browserName} Bookmarks: {profileName} for user {username}.json");
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} Bookmarks ({profileName}) for user {username}: {ex.Message}"); }
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string webDataPath = Path.Combine(profilePath, "Web Data");
                if (File.Exists(webDataPath))
                {
                    string tempWebDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_WebData");
                    try
                    {
                        File.Copy(webDataPath, tempWebDataPath, true);
                        ExtractChromiumAutofill(tempWebDataPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_Autofill_{profileName}_{username}.csv"));
                        ExtractChromiumCreditCards(tempWebDataPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_CreditCards_{profileName}_{username}.csv"));
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} WebData ({profileName}) for user {username}: {ex.Message}"); }
                    finally { if (File.Exists(tempWebDataPath)) File.Delete(tempWebDataPath); }
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));
            }
        }

        private void ExtractChromiumCookies(string cookiesDbPath, string outputCsvPath)
        {
            List<string[]> cookies = new List<string[]>();
            cookies.Add(new string[] { "Host Key", "Name", "Value (Encrypted)", "Path", "Creation UTC (Original)", "Expires UTC (Original)", "Is Secure", "Is Http Only", "Same Site", "Source Scheme" });
            string connectionString = $"Data Source={cookiesDbPath};Version=3;FailIfMissing=True;Read Only=True;";
            string query = "SELECT host_key, name, encrypted_value, path, creation_utc, expires_utc, is_secure, is_httponly, samesite, source_scheme FROM cookies";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] encryptedValueBytes = reader.IsDBNull(reader.GetOrdinal("encrypted_value")) ? null : (byte[])reader["encrypted_value"];
                            string encryptedValuePreview = "N/A";
                            if (encryptedValueBytes != null && encryptedValueBytes.Length > 0)
                            {
                                encryptedValuePreview = $"[Encrypted] {BitConverter.ToString(encryptedValueBytes, 0, Math.Min(16, encryptedValueBytes.Length)).Replace("-", "")}...";
                            }

                            cookies.Add(new string[] {
                                 reader["host_key"].ToString(),
                                 reader["name"].ToString(),
                                 encryptedValuePreview,
                                 reader["path"].ToString(),
                                 reader.GetInt64(reader.GetOrdinal("creation_utc")).ToString(),
                                 reader.GetInt64(reader.GetOrdinal("expires_utc")).ToString(),
                                 reader["is_secure"].ToString(),
                                 reader["is_httponly"].ToString(),
                                 reader["samesite"].ToString(),
                                 reader["source_scheme"].ToString()
                             });
                        }
                    }
                }
                WriteCsvFile(outputCsvPath, cookies);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing cookies ({Path.GetFileName(cookiesDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error extracting cookies from {Path.GetFileName(cookiesDbPath)}: {ex.Message}");
            }
        }

        private void ExtractChromiumHistory(string historyDbPath, string outputCsvPath)
        {
            List<string[]> historyEntries = new List<string[]>();
            historyEntries.Add(new string[] { "URL", "Title", "Visit Count", "Last Visit Time UTC (Original)" });
            string connectionString = $"Data Source={historyDbPath};Version=3;FailIfMissing=True;Read Only=True;";
            string query = "SELECT url, title, visit_count, last_visit_time FROM urls ORDER BY last_visit_time DESC";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            historyEntries.Add(new string[] {
                                 reader.GetString(0),
                                 reader.GetString(1),
                                 reader.GetInt32(2).ToString(),
                                 reader.GetInt64(3).ToString()
                             });
                        }
                    }
                }
                WriteCsvFile(outputCsvPath, historyEntries);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing history ({Path.GetFileName(historyDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error extracting history from {Path.GetFileName(historyDbPath)}: {ex.Message}");
            }
        }

        private void ExtractChromiumAutofill(string webDataDbPath, string outputCsvPath)
        {
            List<string[]> autofillEntries = new List<string[]>();
            autofillEntries.Add(new string[] { "Guid", "Label", "Field Name", "Field Value", "Date Created UTC (Original)", "Date Modified UTC (Original)" });

            string connectionString = $"Data Source={webDataDbPath};Version=3;FailIfMissing=True;Read Only=True;";

            string profilesQuery = @"SELECT guid, label, '' as name, '' as value, date_created, date_modified
                                              FROM autofill_profiles";
            string contentsQuery = @"SELECT '' as guid, '' as label, name, value, date_created, date_modified
                                              FROM autofill_contents";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(profilesQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            autofillEntries.Add(new string[] {
                                 reader.GetString(0),
                                 reader.GetString(1),
                                 reader.GetString(2),
                                 reader.GetString(3),
                                 reader.GetInt64(4).ToString(),
                                 reader.GetInt64(5).ToString()
                             });
                        }
                    }

                    using (var command = new SQLiteCommand(contentsQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            autofillEntries.Add(new string[] {
                                 reader.GetString(0),
                                 reader.GetString(1),
                                 reader.GetString(2),
                                 reader.GetString(3),
                                 reader.GetInt64(4).ToString(),
                                 reader.GetInt64(5).ToString()
                             });
                        }
                    }
                }
                WriteCsvFile(outputCsvPath, autofillEntries);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing Autofill data ({Path.GetFileName(webDataDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error extracting Autofill data from {Path.GetFileName(webDataDbPath)}: {ex.Message}");
            }
        }

        private void ExtractChromiumCreditCards(string webDataDbPath, string outputCsvPath)
        {
            List<string[]> creditCardEntries = new List<string[]>();
            creditCardEntries.Add(new string[] { "Guid", "Name", "Expiration Month (Encrypted)", "Expiration Year (Encrypted)", "Card Number (Encrypted)", "Date Created UTC (Original)", "Date Modified UTC (Original)" });

            string connectionString = $"Data Source={webDataDbPath};Version=3;FailIfMissing=True;Read Only=True;";

            string query = @"SELECT guid, name_on_card, expiration_month_encrypted, expiration_year_encrypted, card_number_encrypted, date_created, date_modified
                                      FROM credit_cards";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[] expMonthBytes = reader.IsDBNull(reader.GetOrdinal("expiration_month_encrypted")) ? null : (byte[])reader["expiration_month_encrypted"];
                            byte[] expYearBytes = reader.IsDBNull(reader.GetOrdinal("expiration_year_encrypted")) ? null : (byte[])reader["expiration_year_encrypted"];
                            byte[] cardNumberBytes = reader.IsDBNull(reader.GetOrdinal("card_number_encrypted")) ? null : (byte[])reader["card_number_encrypted"];

                            string expMonthPreview = (expMonthBytes != null && expMonthBytes.Length > 0) ? $"[Encrypted] {BitConverter.ToString(expMonthBytes, 0, Math.Min(8, expMonthBytes.Length)).Replace("-", "")}..." : "N/A";
                            string expYearPreview = (expYearBytes != null && expYearBytes.Length > 0) ? $"[Encrypted] {BitConverter.ToString(expYearBytes, 0, Math.Min(8, expYearBytes.Length)).Replace("-", "")}..." : "N/A";
                            string cardNumberPreview = (cardNumberBytes != null && cardNumberBytes.Length > 0) ? $"[Encrypted] {BitConverter.ToString(cardNumberBytes, 0, Math.Min(16, cardNumberBytes.Length)).Replace("-", "")}..." : "N/A";

                            creditCardEntries.Add(new string[] {
                                 reader.GetString(0),
                                 reader.GetString(1),
                                 expMonthPreview,
                                 expYearPreview,
                                 cardNumberPreview,
                                 reader.GetInt64(5).ToString(),
                                 reader.GetInt64(6).ToString()
                             });
                        }
                    }
                }
                WriteCsvFile(outputCsvPath, creditCardEntries);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing Credit Card data ({Path.GetFileName(webDataDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error extracting Credit Card data from {Path.GetFileName(webDataDbPath)}: {ex.Message}");
            }
        }

        private void ExtractFirefoxData(string userBackupSubfolderPath, IProgress<int> progressReporter, ref int currentStep, double progressPerStep, string browserName, string userRoamingAppData)
        {
            string firefoxProfilesPath = Path.Combine(userRoamingAppData, "Mozilla", browserName, "Profiles");

            if (!Directory.Exists(firefoxProfilesPath))
            {
                statusReporter?.Report($"{browserName} profiles directory not found at {firefoxProfilesPath}.");
                currentStep += 5;
                progressReporter?.Report((int)(currentStep * progressPerStep));
                return;
            }

            string[] profileDirs = Directory.GetDirectories(firefoxProfilesPath, "*.default*", SearchOption.TopDirectoryOnly);
            if (!profileDirs.Any())
            {
                profileDirs = Directory.GetDirectories(firefoxProfilesPath, "*", SearchOption.TopDirectoryOnly)
                                        .Where(d => File.Exists(Path.Combine(d, "places.sqlite"))).ToArray();
            }

            if (!profileDirs.Any())
            {
                statusReporter?.Report($"No {browserName} profiles found at {firefoxProfilesPath}.");
                currentStep += 5;
                progressReporter?.Report((int)(currentStep * progressPerStep));
                return;
            }

            string username = "UnknownUser";
            try
            {
                string[] pathSegments = userRoamingAppData.Split(Path.DirectorySeparatorChar);
                for (int i = 0; i < pathSegments.Length; i++)
                {
                    if (pathSegments[i].Equals("Users", StringComparison.OrdinalIgnoreCase) && i + 1 < pathSegments.Length)
                    {
                        username = pathSegments[i + 1];
                        break;
                    }
                }
            }
            catch { }

            foreach (string profilePath in profileDirs)
            {
                string profileName = Path.GetFileName(profilePath);
                if (string.Equals(profileName, "User Data", StringComparison.OrdinalIgnoreCase)) profileName = "Default_Legacy";
                statusReporter?.Report($"Processing {browserName} Profile: {profileName} for user {username}...");

                statusReporter?.Report($"Password extraction for {browserName} profile {profileName} for user {username} is complex and not implemented.");
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string placesDbPath = Path.Combine(profilePath, "places.sqlite");
                if (File.Exists(placesDbPath))
                {
                    string tempPlacesDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_places.sqlite");
                    try
                    {
                        File.Copy(placesDbPath, tempPlacesDbPath, true);
                        ExtractFirefoxHistoryAndBookmarks(tempPlacesDbPath,
                            Path.Combine(userBackupSubfolderPath, $"{browserName}_History_{profileName}_{username}.csv"),
                            Path.Combine(userBackupSubfolderPath, $"{browserName}_Bookmarks_{profileName}_{username}.csv"));
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} History/Bookmarks ({profileName}) for user {username}: {ex.Message}"); }
                    finally { if (File.Exists(tempPlacesDbPath)) File.Delete(tempPlacesDbPath); }
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string cookiesDbPath = Path.Combine(profilePath, "cookies.sqlite");
                if (File.Exists(cookiesDbPath))
                {
                    string tempCookiesDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_cookies.sqlite");
                    try
                    {
                        File.Copy(cookiesDbPath, tempCookiesDbPath, true);
                        ExtractFirefoxCookies(tempCookiesDbPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_Cookies_{profileName}_{username}.csv"));
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} Cookies ({profileName}) for user {username}: {ex.Message}"); }
                    finally { if (File.Exists(tempCookiesDbPath)) File.Delete(tempCookiesDbPath); }
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                string formHistoryDbPath = Path.Combine(profilePath, "formhistory.sqlite");
                if (File.Exists(formHistoryDbPath))
                {
                    string tempFormHistoryDbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_formhistory.sqlite");
                    try
                    {
                        File.Copy(formHistoryDbPath, tempFormHistoryDbPath, true);
                        ExtractFirefoxAutofill(tempFormHistoryDbPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_Autofill_{profileName}_{username}.csv"));
                    }
                    catch (Exception ex) { statusReporter?.Report($"Error {browserName} Autofill ({profileName}) for user {username}: {ex.Message}"); }
                    finally { if (File.Exists(tempFormHistoryDbPath)) File.Delete(tempFormHistoryDbPath); }
                }
                else if (File.Exists(placesDbPath))
                {
                    statusReporter?.Report($"Firefox formhistory.sqlite not found for profile {profileName} for user {username}. Checking places.sqlite for limited autofill data.");
                    ExtractFirefoxAutofill(placesDbPath, Path.Combine(userBackupSubfolderPath, $"{browserName}_Autofill_{profileName}_from_places_{username}.csv"));
                }
                else
                {
                    statusReporter?.Report($"Firefox formhistory.sqlite and places.sqlite not found for profile {profileName} for user {username}. Cannot extract Autofill.");
                }
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));

                statusReporter?.Report($"Credit Card extraction for {browserName} profile {profileName} for user {username} is complex and not implemented.");
                currentStep++;
                progressReporter?.Report((int)(currentStep * progressPerStep));
            }
        }

        private void ExtractFirefoxHistoryAndBookmarks(string placesDbPath, string historyCsvPath, string bookmarksCsvPath)
        {
            string connectionString = $"Data Source={placesDbPath};Version=3;FailIfMissing=True;Read Only=True;";

            List<string[]> historyEntries = new List<string[]>();
            historyEntries.Add(new string[] { "URL", "Title", "Visit Count", "Last Visit Time UTC (Original)" });

            string historyQuery = @"SELECT p.url, COALESCE(p.title, ''), COUNT(hv.id), MAX(hv.visit_date)
                                            FROM moz_places p
                                            JOIN moz_historyvisits hv ON p.id = hv.place_id
                                            GROUP BY p.id
                                            ORDER BY MAX(hv.visit_date) DESC";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(historyQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            historyEntries.Add(new string[] {
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetInt64(2).ToString(),
                                reader.GetInt64(3).ToString()
                            });
                        }
                    }
                }
                WriteCsvFile(historyCsvPath, historyEntries);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing history ({Path.GetFileName(placesDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex) { statusReporter?.Report($"Error History/Bookmarks from {Path.GetFileName(placesDbPath)}: {ex.Message}"); }

            List<string[]> bookmarkEntries = new List<string[]>();
            bookmarkEntries.Add(new string[] { "Title", "URL", "Date Added UTC (Original)" });

            string bookmarkQuery = @"SELECT COALESCE(b.title, ''), p.url, b.dateAdded
                                             FROM moz_bookmarks b
                                             JOIN moz_places p ON b.fk = p.id
                                             WHERE b.type = 1
                                             ORDER BY b.dateAdded DESC";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(bookmarkQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookmarkEntries.Add(new string[] {
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetInt64(2).ToString()
                            });
                        }
                    }
                }
                WriteCsvFile(bookmarksCsvPath, bookmarkEntries);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing bookmarks ({Path.GetFileName(placesDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex) { statusReporter?.Report($"Error Bookmarks from {Path.GetFileName(placesDbPath)}: {ex.Message}"); }
        }

        private void ExtractFirefoxCookies(string cookiesDbPath, string outputCsvPath)
        {
            List<string[]> cookies = new List<string[]>();
            cookies.Add(new string[] { "Origin Attributes", "Name", "Value", "Host", "Path", "Creation Time UTC (Original)", "Expiry Time UTC (Original)", "Last Accessed Time UTC (Original)", "Is Secure", "Is Http Only", "Same Site", "Source Scheme" });
            string connectionString = $"Data Source={cookiesDbPath};Version=3;FailIfMissing=True;Read Only=True;";

            string query = "SELECT originAttributes, name, value, host, path, creationTime, expiry, lastAccessed, isSecure, isHttpOnly, samesite, source_scheme FROM moz_cookies";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cookies.Add(new string[] {
                                 reader.IsDBNull(0) ? "" : reader.GetString(0),
                                 reader.GetString(1),
                                 reader.GetString(2),
                                 reader.GetString(3),
                                 reader.GetString(4),
                                 reader.GetInt64(5).ToString(),
                                 reader.GetInt64(6).ToString(),
                                 reader.GetInt64(7).ToString(),
                                 reader.GetInt64(8).ToString(),
                                 reader.GetInt64(9).ToString(),
                                 reader.IsDBNull(10) ? "" : reader.GetString(10),
                                 reader.IsDBNull(11) ? "" : reader.GetString(11)
                             });
                        }
                    }
                }
                WriteCsvFile(outputCsvPath, cookies);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing cookies ({Path.GetFileName(cookiesDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error Cookies from {Path.GetFileName(cookiesDbPath)}: {ex.Message}");
            }
        }

        private void ExtractFirefoxAutofill(string formHistoryDbPath, string outputCsvPath)
        {
            List<string[]> autofillEntries = new List<string[]>();
            autofillEntries.Add(new string[] { "Field Name", "Value", "Times Used", "First Used UTC (Original)", "Last Used UTC (Original)" });

            string connectionString = $"Data Source={formHistoryDbPath};Version=3;FailIfMissing=True;Read Only=True;";

            string query = @"SELECT fieldname, value, timesUsed, firstUsed, lastUsed
                                      FROM moz_formhistory";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            autofillEntries.Add(new string[] {
                                 reader.GetString(0),
                                 reader.GetString(1),
                                 reader.GetInt32(2).ToString(),
                                 reader.GetInt64(3).ToString(),
                                 reader.GetInt64(4).ToString()
                             });
                        }
                    }
                }
                WriteCsvFile(outputCsvPath, autofillEntries);
            }
            catch (SQLiteException ex)
            {
                statusReporter?.Report($"SQLite error accessing Firefox Autofill data ({Path.GetFileName(formHistoryDbPath)}): {ex.Message}. DB might be locked.");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error extracting Firefox Autofill data from {Path.GetFileName(formHistoryDbPath)}: {ex.Message}");
            }
        }

        private void ExtractFirefoxCreditCards(string profilePath, string outputCsvPath)
        {
            statusReporter?.Report($"Credit Card extraction for Firefox profile {Path.GetFileName(profilePath)} is complex and not implemented.");
        }

        private void WriteCsvFile(string filePath, List<string[]> data)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    foreach (var entry in data)
                    {
                        IEnumerable<string> escapedFields = entry.Select(field =>
                        {
                            if (field == null) return "";
                            string escapedField = field.Replace("\"", "\"\"");
                            if (escapedField.Contains(",") || escapedField.Contains("\"") || escapedField.Contains("\n") || escapedField.Contains("\r"))
                            {
                                escapedField = $"\"{escapedField}\"";
                            }
                            return escapedField;
                        });
                        sw.WriteLine(string.Join(",", escapedFields));
                    }
                }
                statusReporter?.Report($"Successfully wrote CSV: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                statusReporter?.Report($"Error writing CSV file {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                FileInfo currentFile = file;
                string targetFilePath = Path.Combine(destinationDir, currentFile.Name);
                try
                {
                    currentFile.CopyTo(targetFilePath, true);
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    string currentFolderName = Path.GetFileName(sourceDir);
                    statusReporter?.Report($"Permission denied copying file {currentFile.Name} from folder {currentFolderName}: {uaEx.Message}");
                }
                catch (Exception ex)
                {
                    string currentFolderName = Path.GetFileName(sourceDir);
                    statusReporter?.Report($"Error copying file {currentFile.Name} from folder {currentFolderName}: {ex.Message}");
                }
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private string GetChromiumSoftwareFolder(string browserName)
        {
            if (browserName.Contains("Brave")) return "BraveSoftware";
            if (browserName.Contains("Edge")) return "Microsoft";
            if (browserName.Contains("Chrome")) return "Google";
            return browserName;
        }
    }
}
// I hate my life, this piece of shit took 13 hours to code for some reason
