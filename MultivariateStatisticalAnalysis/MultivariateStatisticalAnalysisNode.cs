using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;

namespace MultivariateStatisticalAnalysis;

[DefaultView(MultivariateStatisticalAnalysisViewModel.UniqueId, typeof(MultivariateStatisticalAnalysisViewModel))]
internal class MultivariateStatisticalAnalysisNode : StandardAnalysisNodeBase
{
    public const string UniqueId = "MultivariateStatisticalAnalysis.MultivariateStatisticalAnalysisNode";
    
    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Multivariate Statistical Analysis");

    public MultivariateStatisticalAnalysisNode(IStandardAnalysisNodeBaseServices services)
        : base(services)
    {
    }
}