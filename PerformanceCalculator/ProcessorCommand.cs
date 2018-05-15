// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator
{
    public abstract class ProcessorCommand : CommandBase
    {
        /// <summary>
        /// The console.
        /// </summary>
        public IConsole Console { get; private set; }

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
