using Cameca.CustomAnalysis.Utilities;

namespace MultivariateStatisticalAnalysis;

internal class MultivariateStatisticalAnalysisViewModel : AnalysisViewModelBase<MultivariateStatisticalAnalysisNode>
{
    public const string UniqueId = "MultivariateStatisticalAnalysis.MultivariateStatisticalAnalysisViewModel";

    public MultivariateStatisticalAnalysisViewModel(IAnalysisViewModelBaseServices services)
        : base(services)
    {
    }
}