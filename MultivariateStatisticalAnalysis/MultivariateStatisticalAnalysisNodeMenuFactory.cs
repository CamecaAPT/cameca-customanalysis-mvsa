using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace MultivariateStatisticalAnalysis;

internal class MultivariateStatisticalAnalysisNodeMenuFactory : AnalysisMenuFactoryBase
{
    public MultivariateStatisticalAnalysisNodeMenuFactory(IEventAggregator eventAggregator)
        : base(eventAggregator)
    {
    }

    protected override INodeDisplayInfo DisplayInfo => MultivariateStatisticalAnalysisNode.DisplayInfo;
    protected override string NodeUniqueId => MultivariateStatisticalAnalysisNode.UniqueId;
    public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}