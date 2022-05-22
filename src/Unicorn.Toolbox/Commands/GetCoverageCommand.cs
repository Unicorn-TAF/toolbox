using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Coverage;

namespace Unicorn.Toolbox.Commands
{
    public class GetCoverageCommand : CommandBase
    {
        private readonly SpecsCoverage _coverage;
        private readonly Analyzer _analyzer;

        public GetCoverageCommand(SpecsCoverage coverage, Analyzer analyzer)
        {
            _coverage = coverage;
            _analyzer = analyzer;
        }

        public override void Execute(object parameter) =>
            _coverage.Analyze(_analyzer.Data.FilteredInfo);
    }
}
