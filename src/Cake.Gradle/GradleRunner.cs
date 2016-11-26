﻿using System;
using System.Collections.Generic;
using System.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Tooling;

namespace Cake.Gradle
{
    public class GradleRunner : Tool<GradleRunnerSettings>, IGradleRunnerConfiguration, IGradleRunnerCommands
    {
        private const string TasksSeparator = " ";

        private readonly Verbosity _cakeVerbosityLevel;
        private GradleLogLevel? _logLevel;
        private string _tasks = string.Empty;
        private string _arguments = string.Empty;
        private DirectoryPath _workingDirectoryPath;
        private readonly IFileSystem _fileSystem;

        public GradleRunner(IFileSystem fileSystem, ICakeEnvironment environment, IProcessRunner processRunner,
            IToolLocator tools, Verbosity cakeVerbosityLevel = Verbosity.Normal)
            : base(fileSystem, environment, processRunner, tools)
        {
            _cakeVerbosityLevel = cakeVerbosityLevel;
            _fileSystem = fileSystem;
        }

        protected override string GetToolName()
        {
            return "Gradle Runner";
        }

        /// <summary>
        /// Gets the name of the tool executable, prefers wrapper over plain Gradle.
        /// </summary>
        /// <returns>The tool executable name</returns>
        protected override IEnumerable<string> GetToolExecutableNames()
        {
            yield return "gradlew";
            yield return "gradlew.bat";
            yield return "gradle";
        }

        /// <summary>
        /// Sets the npm logging level
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public IGradleRunnerCommands WithLogLevel(GradleLogLevel logLevel)
        {
            _logLevel = logLevel;
            return this;
        }

        public IGradleRunnerCommands WithTask(string task)
        {
            _tasks += task + TasksSeparator;
            return this;
        }

        public IGradleRunnerConfiguration WithTask(params string[] tasks)
        {
            _tasks += string.Join(TasksSeparator, tasks) + TasksSeparator;
            return this;
        }

        public IGradleRunnerCommands WithArguments(string arguments)
        {
            _arguments = arguments;
            return this;
        }

        public IGradleRunnerCommands FromPath(DirectoryPath path)
        {
            _workingDirectoryPath = path;
            return this;
        }

        public IGradleRunnerCommands Run()
        {
            var settings = new GradleRunnerSettings();
            var args = GetGradleArguments();
            Run(settings, args);
            return this;
        }

        private ProcessArgumentBuilder GetGradleArguments()
        {
            // USAGE: gradle [option...] [task...]
            var args = new ProcessArgumentBuilder();
            AppendLogLevel(args);
            if (!string.IsNullOrWhiteSpace(_arguments)) { 
                args.Append(_arguments.Trim());
            }
            if (!string.IsNullOrWhiteSpace(_tasks))
            {
                args.Append(_tasks.Trim());
            }
            return args;
        }

        private void AppendLogLevel(ProcessArgumentBuilder args)
        {
            if (!_logLevel.HasValue)
            {
                switch (_cakeVerbosityLevel)
                {
                    case Verbosity.Quiet:
                        _logLevel = GradleLogLevel.Quiet;
                        break;
                    case Verbosity.Minimal:
                        _logLevel = GradleLogLevel.LifecycleAndHigher;
                        break;
                    case Verbosity.Normal:
                        _logLevel = GradleLogLevel.LifecycleAndHigher;
                        break;
                    case Verbosity.Verbose:
                        _logLevel = GradleLogLevel.Info;
                        break;
                    case Verbosity.Diagnostic:
                        _logLevel = GradleLogLevel.Debug;
                        break;
                    default:
                        throw new ArgumentException("Unsupported Cake Verbosity: " + _cakeVerbosityLevel);
                }
            }

            switch (_logLevel)
            {
                case GradleLogLevel.Quiet:
                    args.Append("--quiet");
                    break;
                case GradleLogLevel.Debug:
                    args.Append("--debug");
                    break;
                case GradleLogLevel.Info:
                    args.Append("--info");
                    break;
                case GradleLogLevel.LifecycleAndHigher:
                    // no logging option
                    break;
                default:
                    throw new ArgumentException("Unsupported gradle log level: " + _logLevel);
            }
        }

        protected override DirectoryPath GetWorkingDirectory(GradleRunnerSettings settings)
        {
            Console.WriteLine("dir: "+_workingDirectoryPath);
            if (_workingDirectoryPath == null) return base.GetWorkingDirectory(settings);
            if (!_fileSystem.Exist(_workingDirectoryPath))
                throw new DirectoryNotFoundException(
                    $"Working directory path not found [{_workingDirectoryPath.FullPath}]");
            return _workingDirectoryPath;
        }
    }
}