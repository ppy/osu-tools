// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using Alba.CsConsoleFormat;
using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;

namespace PerformanceCalculator
{
    [HelpOption("-?|-h|--help")]
    public abstract class ProcessorCommand
    {
        /// <summary>
        /// The console.
        /// </summary>
        public IConsole Console { get; private set; }

        [UsedImplicitly]
        [Option(Template = "-o|--output <file.txt>", Description = "Output results to text file.")]
        public string OutputFile { get; }

        public virtual void OnExecute(CommandLineApplication app, IConsole console)
        {
            Console = console;
            Execute();
        }

        public void OutputDocument(Document document)
        {
            // todo: make usable by other command
            using (var writer = new StringWriter())
            {
                ConsoleRenderer.RenderDocumentToText(document, new TextRenderTarget(writer), new Rect(0, 0, 250, Size.Infinity));

                var str = writer.GetStringBuilder().ToString();

                var lines = str.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                    lines[i] = lines[i].TrimEnd();
                str = string.Join('\n', lines);

                Console.Write(str);
                if (OutputFile != null)
                    File.WriteAllText(OutputFile, str);
            }
        }

        public virtual void Execute()
        {
        }
    }
}
