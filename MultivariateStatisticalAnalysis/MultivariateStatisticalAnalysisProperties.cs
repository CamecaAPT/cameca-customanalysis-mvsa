using CommunityToolkit.Mvvm.ComponentModel;

namespace MultivariateStatisticalAnalysis;

public class MultivariateStatisticalAnalysisProperties : ObservableObject
{
    private float phase = 0f;
    public float Phase
    {
        get => phase;
        set => SetProperty(ref phase, value);
    }
}