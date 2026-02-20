using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Installer;

public sealed class MainForm : Form
{
	private const int FieldWidth = 470;
	private const int BrowseButtonWidth = 80;

	private const string ReleasesUrl = "https://github.com/SteTeam/NtoLib/releases/latest";
	private const string DocsUrl = "https://github.com/SteTeam/NtoLib/blob/master/Docs/readme.md";
	private readonly CheckBox _backupCheckBox;
	private readonly Button _closeButton;
	private readonly Button _configDirBrowseButton;
	private readonly TextBox _configDirTextBox;
	private readonly Button _dllDirBrowseButton;
	private readonly TextBox _dllDirTextBox;
	private readonly Button _installButton;
	private readonly RichTextBox _logTextBox;
	private readonly ProgressBar _progressBar;
	private readonly Button _zipBrowseButton;
	private readonly TextBox _zipPathTextBox;

	private CancellationTokenSource? _cancellationTokenSource;

	public MainForm()
	{
		BuildLayout(
			out _zipPathTextBox,
			out _zipBrowseButton,
			out _dllDirTextBox,
			out _dllDirBrowseButton,
			out _configDirTextBox,
			out _configDirBrowseButton,
			out _backupCheckBox,
			out _installButton,
			out _progressBar,
			out _logTextBox,
			out _closeButton);

		DetectZipArchive();
	}

	private void BuildLayout(
		out TextBox zipPathTextBox,
		out Button zipBrowseButton,
		out TextBox dllDirTextBox,
		out Button dllDirBrowseButton,
		out TextBox configDirTextBox,
		out Button configDirBrowseButton,
		out CheckBox backupCheckBox,
		out Button installButton,
		out ProgressBar progressBar,
		out RichTextBox logTextBox,
		out Button closeButton)
	{
		SuspendLayout();

		Text = "NtoLib Installer";
		Size = new Size(600, 560);
		MinimumSize = new Size(500, 500);
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);

		TryLoadIcon();

		var currentY = 12;
		var browseX = 12 + FieldWidth + 8;

		// Title
		var titleLabel = new Label
		{
			Text = "NtoLib Installer",
			Font = new Font("Segoe UI", 14F, FontStyle.Bold),
			Location = new Point(12, currentY),
			AutoSize = true
		};
		currentY += 36;

		// GitHub links
		var releasesLink = new LinkLabel
		{
			Text = "Latest release",
			Location = new Point(12, currentY),
			AutoSize = true
		};
		releasesLink.LinkClicked += (_, _) => OpenUrl(ReleasesUrl);

		var docsLink = new LinkLabel
		{
			Text = "Documentation",
			Location = new Point(releasesLink.PreferredWidth + 24, currentY),
			AutoSize = true
		};
		docsLink.LinkClicked += (_, _) => OpenUrl(DocsUrl);
		currentY += 24;

		// Zip path
		var zipLabel = new Label { Text = "Archive:", Location = new Point(12, currentY), AutoSize = true };
		currentY += 22;

		zipPathTextBox = new TextBox { Location = new Point(12, currentY), Size = new Size(FieldWidth, 23) };

		zipBrowseButton = new Button
		{
			Text = "Browse...",
			Location = new Point(browseX, currentY - 1),
			Size = new Size(BrowseButtonWidth, 25)
		};
		zipBrowseButton.Click += OnZipBrowseClicked;
		currentY += 32;

		// DLL directory
		var dllDirLabel = new Label { Text = "DLL destination:", Location = new Point(12, currentY), AutoSize = true };
		currentY += 22;

		dllDirTextBox = new TextBox
		{
			Text = InstallationPaths.DefaultDllDirectory,
			Location = new Point(12, currentY),
			Size = new Size(FieldWidth, 23)
		};

		dllDirBrowseButton = new Button
		{
			Text = "Browse...",
			Location = new Point(browseX, currentY - 1),
			Size = new Size(BrowseButtonWidth, 25)
		};
		dllDirBrowseButton.Click += (_, _) => BrowseForDirectory(_dllDirTextBox);
		currentY += 32;

		// Config directory
		var configDirLabel = new Label
		{
			Text = "Config destination:",
			Location = new Point(12, currentY),
			AutoSize = true
		};
		currentY += 22;

		configDirTextBox = new TextBox
		{
			Text = InstallationPaths.DefaultConfigDirectory,
			Location = new Point(12, currentY),
			Size = new Size(FieldWidth, 23)
		};

		configDirBrowseButton = new Button
		{
			Text = "Browse...",
			Location = new Point(browseX, currentY - 1),
			Size = new Size(BrowseButtonWidth, 25)
		};
		configDirBrowseButton.Click += (_, _) => BrowseForDirectory(_configDirTextBox);
		currentY += 36;

		// Backup checkbox
		backupCheckBox = new CheckBox
		{
			Text = "Back up existing files before overwriting",
			Checked = true,
			Location = new Point(12, currentY),
			AutoSize = true
		};
		currentY += 30;

		// Install button
		installButton = new Button { Text = "Install", Size = new Size(100, 32), Location = new Point(12, currentY) };
		installButton.Click += OnInstallClicked;

		closeButton = new Button { Text = "Close", Size = new Size(100, 32), Location = new Point(470, currentY) };
		closeButton.Click += (_, _) => Close();
		currentY += 42;

		// Progress bar
		progressBar = new ProgressBar
		{
			Location = new Point(12, currentY),
			Size = new Size(558, 22),
			Minimum = 0,
			Maximum = 100,
			Style = ProgressBarStyle.Continuous
		};
		currentY += 30;

		// Log text box
		logTextBox = new RichTextBox
		{
			Location = new Point(12, currentY),
			Size = new Size(558, 560 - currentY - 50),
			ReadOnly = true,
			BackColor = Color.FromArgb(30, 30, 30),
			ForeColor = Color.FromArgb(220, 220, 220),
			Font = new Font("Consolas", 9F),
			WordWrap = true,
			ScrollBars = RichTextBoxScrollBars.Vertical
		};

		Controls.AddRange(new Control[]
		{
			titleLabel, releasesLink, docsLink, zipLabel, zipPathTextBox, zipBrowseButton, dllDirLabel,
			dllDirTextBox, dllDirBrowseButton, configDirLabel, configDirTextBox, configDirBrowseButton,
			backupCheckBox, installButton, closeButton, progressBar, logTextBox
		});

		ResumeLayout(false);
		PerformLayout();
	}

	private void TryLoadIcon()
	{
		try
		{
			var exePath = Assembly.GetExecutingAssembly().Location;
			if (File.Exists(exePath))
			{
				Icon = Icon.ExtractAssociatedIcon(exePath);
			}
		}
		catch
		{
			// Not critical -- run without a custom icon
		}
	}

	private static void OpenUrl(string url)
	{
		Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
	}

	private void DetectZipArchive()
	{
		var zipPath = InstallerService.FindZipArchive();

		if (zipPath != null)
		{
			_zipPathTextBox.Text = zipPath;
			AppendLog($"Found archive: {Path.GetFileName(zipPath)}", Color.LightGreen);
		}
		else
		{
			AppendLog("No NtoLib_v*.zip archive found in the application directory.", Color.OrangeRed);
			AppendLog($"Expected location: {AppDomain.CurrentDomain.BaseDirectory}", Color.Gray);
		}
	}

	private void OnZipBrowseClicked(object? sender, EventArgs e)
	{
		using var dialog = new OpenFileDialog
		{
			Title = "Select NtoLib archive",
			Filter = "ZIP archives (*.zip)|*.zip",
			InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
		};

		if (dialog.ShowDialog() == DialogResult.OK)
		{
			_zipPathTextBox.Text = dialog.FileName;
		}
	}

	private void BrowseForDirectory(TextBox targetTextBox)
	{
		using var dialog = new FolderBrowserDialog
		{
			Description = "Select directory",
			SelectedPath = targetTextBox.Text
		};

		if (dialog.ShowDialog() == DialogResult.OK)
		{
			targetTextBox.Text = dialog.SelectedPath;
		}
	}

	private InstallationPaths BuildPathsFromForm()
	{
		return new InstallationPaths
		{
			DllDirectory = _dllDirTextBox.Text.Trim(),
			ConfigDirectory = _configDirTextBox.Text.Trim()
		};
	}

	private async void OnInstallClicked(object? sender, EventArgs e)
	{
		var zipPath = _zipPathTextBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
		{
			AppendLog("No valid archive selected.", Color.OrangeRed);

			return;
		}

		SetInstallationRunning(true);
		_cancellationTokenSource = new CancellationTokenSource();

		var progress = new Progress<InstallationProgress>(p =>
		{
			_progressBar.Value = Math.Min(p.Percentage, 100);
			AppendLog(p.Message, Color.FromArgb(220, 220, 220));
		});

		var paths = BuildPathsFromForm();
		var service = new InstallerService(paths, progress);

		try
		{
			InstallerService.ValidateZipArchive(zipPath);
			await service.InstallAsync(zipPath, _backupCheckBox.Checked, _cancellationTokenSource.Token);
			AppendLog("Installation completed successfully.", Color.LightGreen);
			MessageBox.Show(
				"NtoLib has been installed and registered successfully.",
				"Success",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
		catch (OperationCanceledException)
		{
			AppendLog("Installation was cancelled.", Color.Yellow);
		}
		catch (Exception ex)
		{
			AppendLog($"ERROR: {ex.Message}", Color.OrangeRed);
			MessageBox.Show(
				$"Installation failed:\n\n{ex.Message}",
				"Error",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
		finally
		{
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;
			SetInstallationRunning(false);
		}
	}

	private void SetInstallationRunning(bool running)
	{
		_installButton.Enabled = !running;
		_backupCheckBox.Enabled = !running;
		_closeButton.Enabled = !running;
		_zipPathTextBox.ReadOnly = running;
		_zipBrowseButton.Enabled = !running;
		_dllDirTextBox.ReadOnly = running;
		_dllDirBrowseButton.Enabled = !running;
		_configDirTextBox.ReadOnly = running;
		_configDirBrowseButton.Enabled = !running;
		_installButton.Text = running ? "Installing..." : "Install";
	}

	private void AppendLog(string message, Color color)
	{
		if (InvokeRequired)
		{
			Invoke(new Action(() => AppendLog(message, color)));

			return;
		}

		var timestamp = DateTime.Now.ToString("HH:mm:ss");
		_logTextBox.SelectionStart = _logTextBox.TextLength;
		_logTextBox.SelectionLength = 0;
		_logTextBox.SelectionColor = Color.Gray;
		_logTextBox.AppendText($"[{timestamp}] ");
		_logTextBox.SelectionColor = color;
		_logTextBox.AppendText(message + Environment.NewLine);
		_logTextBox.ScrollToCaret();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
		}

		base.Dispose(disposing);
	}
}
