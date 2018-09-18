using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;

namespace Backupper
{
	public class Program
	{
		private static IConfigurationRoot Configuration { get; set; }
		private static string DateFormat { get; set; }

		private static int Main(string[] args)
		{
			IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(args.Length > 0 ? args[0] : "backup.json");

			try
			{
				Configuration = configurationBuilder.Build();
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("Config file is not found");
				return 3;
			}

			DateFormat = Configuration["datetime_fmt"] ?? "MM-dd-yyyy__HH_mm_ss";
			SetupLogging();

			ILog rootLogger = LogManager.GetLogger(typeof(Program));

			if (!(Configuration["backups_directory"] is string backupsDirectory))
			{
				rootLogger.Error("No backups directory as specified. Aborting.");
				return 1;
			}

			var b = new BackupMacker(backupsDirectory)
			{
				DateTimeFormat = DateFormat
			};

			IEnumerable<string> sourceDirectories =
				from keyValue in Configuration.GetSection("source_directories").GetChildren()
				select keyValue.Value;


			foreach (string sourceDirectory in sourceDirectories)
			{
				if (IsSubDirectory(backupsDirectory, sourceDirectory)) {
					rootLogger.Error($"Destination directory is inside of source directory: {sourceDirectory}");
					return 4;
				}

				try
				{
					b.AddSourceDirecotry(sourceDirectory);
				}
				catch (ArgumentException e)
				{
					rootLogger.Error($"Unable to add new source directory ({sourceDirectory}): {e}");
					return 4;
				}
			}

			try
			{
				b.MakeBackup();
			}
			catch (Exception ex)
			{
				rootLogger.Error($"Unable to make backup", ex);
				return 2;
			}

			return 0;
		}

		/// <summary>
		/// Recursively checks is directory is inside another directory
		/// </summary>
		/// <param name="subdirectory">Directeory if which parent directory we will look for</param>
		/// <param name="parentDirectory">Directory in which subdirectory could be</param>
		/// <returns>true if <paramref name="subdirectory"/> is inside <paramref name="parentDirectory"/> otherwise false</returns>
		private static bool IsSubDirectory(string subdirectory, string parentDirectory)
		{
			DirectoryInfo parent = new DirectoryInfo(parentDirectory);
			DirectoryInfo subdir = new DirectoryInfo(subdirectory);

			while (subdir.Parent != null)
			{
				if (subdir.Parent.FullName == parent.FullName)
				{
					return true;
				}
				else
				{
					subdir = subdir.Parent;
				}
			}

			return false;
		}

		/// <summary>
		/// Sets up logging to files
		/// </summary>
		private static void SetupLogging()
		{
			var hierarchy = (Hierarchy)LogManager.GetRepository(typeof(Program).Assembly);
			var tracer = new TraceAppender();
			var filter = new LevelMatchFilter();
			var layout = new PatternLayout("%date{MMM/dd/yyyy HH:mm:ss,fff} [%thread] %-5level %logger %ndc – " +
										   "%message%newline");

			tracer.Layout = layout;
			tracer.ActivateOptions();
			hierarchy.Root.AddAppender(tracer);

			var roller = new RollingFileAppender
			{
				File = Path.Combine(Configuration["logs_directory"] ?? "Logs", $"{DateTime.Now.ToString(DateFormat)}.log"),
				ImmediateFlush = true,
				AppendToFile = true,
				StaticLogFileName = true,
				RollingStyle = RollingFileAppender.RollingMode.Once,
				LockingModel = new FileAppender.MinimalLock()
			};
			roller.AddFilter(filter);
			roller.Layout = layout;
			roller.ActivateOptions();
			hierarchy.Root.AddAppender(roller);

			var consoleAppender = new ConsoleAppender { Layout = layout };
			consoleAppender.ActivateOptions();
			hierarchy.Root.AddAppender(consoleAppender);

			Level assumedLevel = hierarchy.LevelMap[Configuration["log_level"] ?? "Info"];
			hierarchy.Root.Level = assumedLevel ?? Level.Info;

			hierarchy.Configured = true;

			if (assumedLevel is null)
			{
				LogManager.GetLogger(typeof(Program)).Error("Unable to set log level. Will use INFO.", null);
			}
		}
	}
}