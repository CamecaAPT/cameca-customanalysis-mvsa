using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace MultivariateStatisticalAnalysis;

[DefaultView(MultivariateStatisticalAnalysisViewModel.UniqueId, typeof(MultivariateStatisticalAnalysisViewModel))]
internal class MultivariateStatisticalAnalysisNode : AnalysisFilterNodeBase
{
    public const string UniqueId = "MultivariateStatisticalAnalysis.MultivariateStatisticalAnalysisNode";

    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Multivariate Statistical Analysis");

    public MultivariateStatisticalAnalysisNode(IAnalysisFilterNodeBaseServices services)
        : base(services)
    {
    }

    protected override void OnAdded(NodeAddedEventArgs eventArgs)
    {
        base.OnAdded(eventArgs);
        var properties = new MultivariateStatisticalAnalysisProperties();
        properties.PropertyChanged += Properties_PropertyChanged;
        Properties = properties;
    }

    private void Properties_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MultivariateStatisticalAnalysisProperties.Phase))
        {
            DataStateIsValid = false;
        }
    }

    private PhaseMapper? phaseMapper = null;

    protected override IEnumerable<ReadOnlyMemory<ulong>> GetIndicesDelegate(IIonData ownerIonData, IProgress<double>? progress, CancellationToken token)
    {
        if (phaseMapper is null || Properties is not MultivariateStatisticalAnalysisProperties properties) yield break;
        float selectedPhase = properties.Phase;

        ulong index = 0L;
        foreach (var chunk in ownerIonData.CreateSectionDataEnumerable(IonDataSectionName.Position))
        {
            var indexBuffer = new ulong[chunk.Length];
            int bufferIndex = 0;

            var positions = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position).Span;
            for (var i = 0; i < chunk.Length; i++)
            {
                if (phaseMapper.GetPhase(positions[i]) == selectedPhase)
                {
                    indexBuffer[bufferIndex++] = index;
                }
                index++;
            }
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


        var minEdge = min - (voxelSize / 2);
        var maxEdge = max + (voxelSize / 2);

        int xBins = (int)MathF.Round(maxEdge.X - minEdge.X);
        int yBins = (int)MathF.Round(maxEdge.Y - minEdge.Y);
        int zBins = (int)MathF.Round(maxEdge.Z - minEdge.Z);

        var phaseMapper = new PhaseMapper(minEdge, voxelSize, xBins, yBins, zBins);
        for (var i = 0; i < centers.Length; i++)
        {
            var center = centers[i];
            phaseMapper.SetPhase(centers[i], phases[i]);
        }
        this.phaseMapper = phaseMapper;
    }

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