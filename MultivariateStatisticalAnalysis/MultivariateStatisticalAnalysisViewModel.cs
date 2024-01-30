using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.IO;
using System.Numerics;
using System.Windows.Threading;

namespace MultivariateStatisticalAnalysis;

internal class MultivariateStatisticalAnalysisViewModel : AnalysisViewModelBase<MultivariateStatisticalAnalysisNode>
{
    public const string UniqueId = "MultivariateStatisticalAnalysis.MultivariateStatisticalAnalysisViewModel";

    public RelayCommand BrowseVoxelFileCommand { get; }
    public RelayCommand LoadFromVoxelFileCommand { get; }

    private string? voxelFilePath = null;
    public string? VoxelFilePath
    {
        get => voxelFilePath;
        set => SetProperty(ref voxelFilePath, value);
    }

    private float voxelDimensionX = 1f;
    public float VoxelDimensionX
    {
        get => voxelDimensionX;
        set => SetProperty(ref voxelDimensionX, value);
    }

    private float voxelDimensionY = 1f;
    public float VoxelDimensionY
    {
        get => voxelDimensionY;
        set => SetProperty(ref voxelDimensionY, value);
    }

    private float voxelDimensionZ = 1f;
    public float VoxelDimensionZ
    {
        get => voxelDimensionZ;
        set => SetProperty(ref voxelDimensionZ, value);
    }
    
    private bool flipXY = false;
    public bool FlipXY
    {
        get => flipXY;
        set => SetProperty(ref flipXY, value);
    }
    
    public MultivariateStatisticalAnalysisViewModel(IAnalysisViewModelBaseServices services)
        : base(services)
    {
        BrowseVoxelFileCommand = new RelayCommand(BrowseVoxelFileHandler);
        LoadFromVoxelFileCommand = new RelayCommand(LoadFromVoxelFile);
    }

    private void LoadFromVoxelFile()
    {
        if (VoxelDimensionX > 0f && voxelDimensionY > 0f && VoxelDimensionZ > 0f && File.Exists(VoxelFilePath))
        {
            Node?.LoadFromVoxelFile(VoxelFilePath, new Vector3(VoxelDimensionX, voxelDimensionY, voxelDimensionZ), flipXY: FlipXY);
        }
    }

    private void BrowseVoxelFileHandler()
    {
        var openFileDialog = new OpenFileDialog
        {
            DefaultExt = "*.apt",
            Filter = "APT File (.apt)|*.apt",
        };
        if (openFileDialog.ShowDialog() == true)
        {
            VoxelFilePath = openFileDialog.FileName;
        }
    }
}