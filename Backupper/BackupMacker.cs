using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace Backupper
{
	/// <summary>
	/// Class for managing backups
	/// </summary>
	public sealed class BackupMacker
	{
		private readonly List<string> _sourceDirectories;
		private string _dateTimeFormat;
		/// <summary>
		/// BackupMacker's logger
		/// </summary>
		private ILog _logger;

		/// <summary>
		/// Directory where backups will be stored
		/// </summary>
		public string BackupsDirectory { get; }
		/// <summary>
		/// Collection of source directories
		/// </summary>
		public IReadOnlyCollection<string> SourceDirectories => new List<string>(_sourceDirectories);

		/// <summary>
		/// Datetime format to use in directories names
		/// </summary>
		public string DateTimeFormat
		{
			get => _dateTimeFormat;
			set => _dateTimeFormat = value ?? throw new ArgumentNullException();
		}

		/// <summary>
		/// Creates new BackupMacker for specified backups directory
		/// </summary>
		/// <param name="backupsDirectory">Directory to ave backups</param>
		public BackupMacker(string backupsDirectory)
		{
			_logger = LogManager.GetLogger(typeof(BackupMacker));
			_sourceDirectories = new List<string>();

			BackupsDirectory = backupsDirectory ?? throw new ArgumentNullException(nameof(backupsDirectory));
		}

		/// <summary>
		/// Adds source directory to list
		/// </summary>
		/// <param name="sourceDirectory">Directory from where to copy</param>
		public void AddSourceDirecotry(string sourceDirectory)
		{
			if (sourceDirectory is null)
			{
				throw new ArgumentNullException(nameof(sourceDirectory));
			}

			if (_sourceDirectories.Contains(sourceDirectory))
			{
				throw new ArgumentException("This source directory already exists", nameof(sourceDirectory));
			}

			_sourceDirectories.Add(sourceDirectory);
		}

		/// <summary>
		/// Removes directory from directories list
		/// </summary>
		/// <param name="directory">Directory to remove</param>
		public void RemoveDirectory(string directory)
		{
			_sourceDirectories.Remove(directory);
		}

		/// <summary>
		/// Makes backup at <see cref="BackupsDirectory"/> and copies all files from <see cref="SourceDirectories"/>
		/// </summary>
		public void MakeBackup()
		{
			if (_sourceDirectories.Count == 0)
			{
				_logger.Info("Nothing to backup");
				return;
			}

			string destinationDirictory = Path.Combine(BackupsDirectory, DateTime.Now.ToString(DateTimeFormat));
			_logger.Info($"Backing up to \"{destinationDirictory}\"");
			_logger.Info($"Checking destination directory for existance and creating if it necessary..");

			try
			{
				Directory.CreateDirectory(destinationDirictory);
			}
			catch (SystemException ex)
			{
				_logger.Error($"Unable to create destination directory due to system error. Aborting.", ex);
				throw;
			}

			foreach (string sourceDirectory in _sourceDirectories)
			{
				_logger.Info($"Copying {sourceDirectory}...");
				try
				{
					CopyDirectory(sourceDirectory, destinationDirictory);
				}
				catch (DirectoryNotFoundException ex)
				{
					_logger.Error($"Unable to copy {sourceDirectory}. Aborting", ex);
					throw;
				}
			}

			_logger.Info("Done");
		}

		/// <summary>
		/// Recusively copies directory 
		/// </summary>
		/// <param name="sourceDirectoryPath">Directory to copy from</param>
		/// <param name="destinationDirictoryPath">Destination directory</param>
		private void CopyDirectory(string sourceDirectoryPath, string destinationDirictoryPath)
		{
			_logger.Debug($"Entering {sourceDirectoryPath}");
			var sourceDirectory = new DirectoryInfo(sourceDirectoryPath);

			if (!sourceDirectory.Exists)
			{
				throw new DirectoryNotFoundException("Source directory does not exist or could not be found.");
			}

			DirectoryInfo[] dirs = sourceDirectory.GetDirectories();
			if (!Directory.Exists(destinationDirictoryPath))
			{
				_logger.Debug($"Creating {destinationDirictoryPath}...");
				Directory.CreateDirectory(destinationDirictoryPath);
			}

			FileInfo[] files = sourceDirectory.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destinationDirictoryPath, file.Name);
				_logger.Debug($"Copying {file.FullName}...");
				try
				{
					file.CopyTo(temppath, false);
				}
				catch (SystemException ex)
				{
					_logger.Error($"Unable to copy {file.FullName}. Skipping.", ex);
				}
			}

			foreach (DirectoryInfo subdir in dirs)
			{
				string temppath = Path.Combine(destinationDirictoryPath, subdir.Name);
				CopyDirectory(subdir.FullName, temppath);
			}
		}
	}
}