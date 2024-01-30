using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Ioc;
using Prism.Modularity;

namespace MultivariateStatisticalAnalysis;

/// <summary>
/// Public <see cref="IModule"/> implementation is the entry point for AP Suite to discover and configure the custom analysis
/// </summary>
public class MultivariateStatisticalAnalysisModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.AddCustomAnalysisUtilities(options => options.UseStandardBaseClasses = true);

        containerRegistry.Register<object, MultivariateStatisticalAnalysisNode>(MultivariateStatisticalAnalysisNode.UniqueId);
        containerRegistry.RegisterInstance(MultivariateStatisticalAnalysisNode.DisplayInfo, MultivariateStatisticalAnalysisNode.UniqueId);
        containerRegistry.Register<IAnalysisMenuFactory, MultivariateStatisticalAnalysisNodeMenuFactory>(nameof(MultivariateStatisticalAnalysisNodeMenuFactory));
        containerRegistry.Register<object, MultivariateStatisticalAnalysisViewModel>(MultivariateStatisticalAnalysisViewModel.UniqueId);
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();

        extensionRegistry.RegisterAnalysisView<MultivariateStatisticalAnalysisView, MultivariateStatisticalAnalysisViewModel>(AnalysisViewLocation.Default);
    }
}
