// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using McMaster.Extensions.CommandLineUtils;
using osu.Framework.Platform;
using osu.Game;

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
            
            using (var host = new HeadlessGameHost("performance"))
            {
                var game = new OsuGameBase();
                game.OnLoadComplete += _ => game.Add(CreateProcessor());

                host.Run(game);
            }
        }

        protected abstract Processor CreateProcessor();
    }
}
