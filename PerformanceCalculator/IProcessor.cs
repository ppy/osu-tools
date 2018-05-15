// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-tools/master/LICENCE

namespace PerformanceCalculator
{
    /// <summary>
    /// Interface for the processors of all <see cref="ProcessorCommand"/>.
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        /// Processes the command.
        /// </summary>
        void Execute();
    }
}
