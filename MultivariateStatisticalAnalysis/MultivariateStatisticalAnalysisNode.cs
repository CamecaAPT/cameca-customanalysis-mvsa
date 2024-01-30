using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace MultivariateStatisticalAnalysis;

[DefaultView(MultivariateStatisticalAnalysisViewModel.UniqueId, typeof(MultivariateStatisticalAnalysisViewModel))]
internal class MultivariateStatisticalAnalysisNode : AnalysisFilterNodeBase
{
    public const string UniqueId = "MultivariateStatisticalAnalysis.MultivariateStatisticalAnalysisNode";

    private PhaseMapper? phaseMapper = null;

    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Multivariate Statistical Analysis");

    public MultivariateStatisticalAnalysisNode(IAnalysisFilterNodeBaseServices services)
        : base(services)
    {
    }

    protected override IEnumerable<ReadOnlyMemory<ulong>> GetIndicesDelegate(IIonData ownerIonData, IProgress<double>? progress, CancellationToken token)
    {
        if (phaseMapper is null || Properties is not MultivariateStatisticalAnalysisProperties { Phase: { } phase }) yield break;

        // Our analysis philosophy is to generally compute on-demand
        // Ideally, this would be the spot where the input data could be passed to MATLAB, and MATLAB could return a phase identifier array or other data to be used for the filtering

        /*
        using (dynamic eng = MATLABEngine.StartMATLAB())
        {
            RunOptions opts = new RunOptions() { Nargout = 0 };
            eng.callAPTgui(opts);
        }
        MATLABEngine.TerminateEngineClient();
        */

        // And at this point we have the phase information for applying back to the ions

        // Our IIonData instance are accessed through a sequence of chunks of data
        // This is mainly to circumvent issues with hitting single object size caps in C# and limits on array length (i.e. support data sets with ion counts > Int32.MaxValue)
        ulong index = 0L;
        foreach (var chunk in ownerIonData.CreateSectionDataEnumerable(IonDataSectionName.Position))
        {
            var indexBuffer = new ulong[chunk.Length];
            int bufferIndex = 0;
            var positions = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position).Span;
            for (var i = 0; i < chunk.Length; i++)
            {
                // For each ion in the chuck, determine if we want to include it in filtered data by comparing the mapped phase by voxel to the selected phase
                // If external MATLAB functions are called in this filter function first, then this PhaseMapper object can probably be replace eventually by
                // directly returned data structures from the external function call
                if (phaseMapper.GetPhase(positions[i]) == phase)
                {
                    indexBuffer[bufferIndex++] = index;
                }
                index++;
            }
            // Each array of indices for the currently considered chunk of ions can be yielded back to AP Suite one at a time
            yield return indexBuffer[..bufferIndex];
        }
    }

    internal void LoadFromVoxelFile(string voxelFilePath, Vector3 voxelSize, bool flipXY = false)
    {
        // Additional validation
        if (!(voxelSize.X > 0f && voxelSize.Y > 0f && voxelSize.Z > 0f && File.Exists(voxelFilePath)))
            return;

        // This could eventually be replaced with direct returning of data from external MATLAB call instead of loading special APT file
        var (centers, phases) = VoxelFileHelpers.ParseVoxelInfo(voxelFilePath, IonDataSectionName.Position, IonDataSectionName.Mass);
        // Validate
        if (centers.Length != phases.Length) throw new InvalidOperationException("Voxel centers and phase arrays must be the same length");

        // TODO: Remove temporary - only to correct for an XY reflected initial test file
        if (flipXY)
        {
            var transform = new Matrix4x4 { M12 = 1f, M21 = 1f, M33 = 1f };
            centers = centers.Select(x => Vector3.Transform(x, transform)).ToArray();
        }

        var (min, max) = VoxelFileHelpers.GetVoxelExtants(centers);

        // min and max of bounds of voxel centers. To get real ion position range min and max, adjust by half voxel size
        var minEdge = min - (voxelSize / 2);
        var maxEdge = max + (voxelSize / 2);

        // Compute number of bins in each direction
        int xBins = (int)MathF.Round(maxEdge.X - minEdge.X);
        int yBins = (int)MathF.Round(maxEdge.Y - minEdge.Y);
        int zBins = (int)MathF.Round(maxEdge.Z - minEdge.Z);

        // Create the storage class that can map each ion position to a phase and populated values using the voxel centers and phase data
        var phaseMapper = new PhaseMapper(minEdge, voxelSize, xBins, yBins, zBins);
        for (var i = 0; i < centers.Length; i++)
        {
            var center = centers[i];
            phaseMapper.SetPhase(centers[i], phases[i]);
        }
        // Store the object on the node: By storing on the Node, we don't need to keep the file loading UI open.
        this.phaseMapper = phaseMapper;
    }

    /// <summary>
    /// Instantiates properties panel object and registers the change callback function
    /// </summary>
    /// <param name="eventArgs"></param>
    protected override void OnAdded(NodeAddedEventArgs eventArgs)
    {
        base.OnAdded(eventArgs);
        var properties = new MultivariateStatisticalAnalysisProperties();
        properties.PropertyChanged += Properties_PropertyChanged;
        Properties = properties;
    }

    /// <summary>
    /// Callback on changes to the Properties panel to invalidate the data filer when selected phase changes, forcing an update of the filter
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Properties_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MultivariateStatisticalAnalysisProperties.Phase))
        {
            DataStateIsValid = false;
        }
    }

    /// <summary>
    /// Essentially a reversable 3D voxel map - Given a Vector3, maps it to a voxel bin and returns the value
    /// </summary>
    private class PhaseMapper
    {
        private readonly float[] map;
        private readonly Vector3 minEdge;
        private readonly Vector3 voxelSize;
        private readonly int xBins;
        private readonly int yBins;
        private readonly int zBins;
        private readonly Matrix4x4 transformation;

        public PhaseMapper(Vector3 minEdge, Vector3 voxelSize, int xBins, int yBins, int zBins)
        {
            map = new float[xBins * yBins * zBins];
            this.minEdge = minEdge;
            this.voxelSize = voxelSize;
            this.xBins = xBins;
            this.yBins = yBins;
            this.zBins = zBins;

            var scale = Matrix4x4.CreateScale(1f / voxelSize.X, 1f / voxelSize.Y, 1f / voxelSize.Z);
            transformation = scale
                * Matrix4x4.CreateTranslation(-(Vector3.Transform(minEdge, scale)));
        }

        public void SetPhase(Vector3 position, float phase) => map[ToBin(position)] = phase;

        public float GetPhase(Vector3 position)
        {
            int bin = ToBin(position);
            return map[bin];
        }

        private int ToBin(Vector3 position)
        {
            var normalized = Vector3.Transform(position, transformation);
            int xFloor = (int)normalized.X;
            int yFloor = (int)normalized.Y;
            int zFloor = (int)normalized.Z;
            return xFloor + (yFloor * xBins) + (zFloor * xBins * yBins);
        }
    }
}