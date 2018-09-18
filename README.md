*This repository is a solution for infotecs.ru intership as example of my code*

This directory contains solution of the first task about Backup Macker.
For logging I used log4net NuGet package. For configuration I used Microsoft.Extensions.Configuration.Json and there are a lot of dependencies, so there are a lot of dynamic libraries next to executable, but usally they are included with .NET.

# Configuration
By default configuration of backups is stored under `bakup.json`, but you can specify configuration file path as first argument to program. All other arguments will be ignored. If program will be unable to found config file Backupper will exit with code 3 and won't do anything.

## Example of config

```json
{
  "backups_directory": "tmp",
  "source_directories": [
    "123"
  ],
  "log_level": "debug",
  "datetime_fmt": "MM-dd-yyyy__HH_mm_ss",
  "logs_directory": "Logs"
}
```

## Options


| Argument | Description | Required |
| -------- | ----------- |:--------:|
| `backups_directory` | Path of directory where to store backups. Will be created if able. | Yes |
| `source_directories` | Array of strings that contains paths to directories copy from. | No. Default is `[]` |
| `log_level` | Log level. Supports all log4net levels and all specified task requirements | No. Default is `Info` |
| `datetime_fmt` | Format of datetime which will be used in filenames. | No. Default is `MM-dd-yyyy__HH_mm_ss` |
| `logs_directory` | Directory where to store logs | No. Default is `Logs` |

# Exit codes

| Exit code | Reason |
|:---------:| ------ |
| 0 | Backup was succesfully made |
| 1 | No backup directory was specified |
| 2 | Unable to make backup |
| 3 | Unable to found config file |
| 4 | Incorrect source directory. More info in error log |

# Extra tasks

All extra tasks were done:
 - Config format is JSON
 - Able to specify more than one source directory
 - Every run creates log file, log filtering is supported

