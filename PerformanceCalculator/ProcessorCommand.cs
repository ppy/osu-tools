// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator
{
    public abstract class ProcessorCommand : CommandBase
    {
        /// <summary>
        /// The console.
        /// </summary>
        public IConsole Console { get; private set; }

        [UsedImplicitly]
        [Option(Template = "-o|--output <file.txt>", Description = "Output results to text file.")]
        public string OutputFile { get; }

        public void OnExecute(CommandLineApplication app, IConsole console)
        {
            Console = console;
            CreateProcessor().Execute();
        }

        /// <summary>
        /// Creates the <see cref="IProcessor"/> to process this <see cref="ProcessorCommand"/>.
        /// </summary>
        /// <returns>The <see cref="IProcessor"/>.</returns>
        protected abstract IProcessor CreateProcessor();
    }
}
