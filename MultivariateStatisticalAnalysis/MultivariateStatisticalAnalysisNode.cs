using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.HighPerformance;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace MultivariateStatisticalAnalysis;

[DefaultView(MultivariateStatisticalAnalysisViewModel.UniqueId, typeof(MultivariateStatisticalAnalysisViewModel))]
internal class MultivariateStatisticalAnalysisNode : AnalysisFilterNodeBase
{
    public const string UniqueId = "MultivariateStatisticalAnalysis.MultivariateStatisticalAnalysisNode";
    private readonly IMassSpectrumRangeManagerProvider rangeManagerProvider;
    private PhaseMapper? phaseMapper = null;
    private IMassSpectrumRangeManager? rangeManager;

    public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("Multivariate Statistical Analysis");

    public MultivariateStatisticalAnalysisNode(IAnalysisFilterNodeBaseServices services, IMassSpectrumRangeManagerProvider rangeManagerProvider)
        : base(services)
    {
        this.rangeManagerProvider = rangeManagerProvider;
    }

    protected override IEnumerable<ReadOnlyMemory<ulong>> GetIndicesDelegate(IIonData ownerIonData, IProgress<double>? progress, CancellationToken token)
    {
        if (phaseMapper is null || Properties is not MultivariateStatisticalAnalysisProperties { Phase: { } phase }) yield break;

        // Ideally, this would be the spot where the input data could be passed to MATLAB, and MATLAB could return a phase identifier array or other data to be used for the filtering
        (Vector3[] positions, float[] masses, byte[] ionTypes) = ownerIonData.LoadStandardSectionsAsArray();
        (float[] xPositions, float[] yPositions, float[] zPositions) = positions.Decompose();
        // represents minimum and maximum boundary containing all positions
        var extents = ownerIonData.Extents;
        var ionTypeInfo = ownerIonData.Ions;
        var ranges = GetRanges(ionTypeInfo);

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        /*
        At this point we have:
         xPositions: float[] the X position of each ion
         yPositions: float[] the X position of each ion
         ZPositions: float[] the X position of each ion
         masses: float[] representing the mass/charge ratio of each ion
         extents: object containing a min and max vector3 defining the boundary of the position data
         ionTypes: byte[] representing the assigned ion type for each ion (or 255 if unassigned)
                   (note that due to the AP Suite "spatial ranging" feature, this can sometimes be different than directly applying the range information to the masses array)
         ionTypeInfo: an array of object containing ion type information to be used with the ionTypes array
                      The value of each byte in the ionTypes array (<255) corresponds to the index in this ionTypeInfo array
                      e.g. The name of the first ion in the dataset would given by `ionTypeInfo[ionTypes[0]].Name` although do note generally to filter out 255 unassigned ions first
                      The actual objects in ionTypeInfo are instance of IIonTypeInfo, and this can be used elsewhere for other information
         ranges: an array that should be similar to a RRNG file. Each list element is a tuple (string Name, double Min, double Max)
                 where Name is the name of the ion for the range, and Min/Max define low and high bounds of the range.
                 Ordered in ascending order by Min bound or range.

        All of this put together should roughly be essentially the same amount of information that can be parsed from directly loading an APT file and corresponding RRNG file
        */


        // Here's an example of where we could call into MATLAB tools. Hopefully the above data objects are converted into sufficiently simply primitive representations that can be passed to MATLAB

        /*
        using (dynamic eng = MATLABEngine.StartMATLAB())
        {
            RunOptions opts = new RunOptions() { Nargout = 0 };

            // positions and masses could be converted MATLAB types and passed to the function

            eng.callAPTgui(opts);

            // A lot of different things could be returned, but for the purpose of this example
            // probably just return voxel centers and phase. Ideally voxel metadata as well such as size and extants
            // Then the LoadFromVoxelFile method can essentially be replaced with this information
        }
        MATLABEngine.TerminateEngineClient();
        //*/

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


        // And at this point we have the phase information for applying back to the ions
        // This function just expect an ascending array of ion indices from the original dataset to return for further analysis.
        // I'm using an example of selecting a specific phase
        ulong index = 0L;
        // I'll just allocate a large enough array, then return only what we need
        var filteredIndices = new ulong[positions.Length];
        int filterCount = 0;
        for (var i = 0; i < positions.Length; i++)
        {
            // For each ion in the chuck, determine if we want to include it in filtered data by comparing the mapped phase by voxel to the selected phase
            // If external MATLAB functions are called in this filter function first, then this PhaseMapper object can probably be replace eventually by
            // directly returned data structures from the external function call
            if (phaseMapper.GetPhase(positions[i]) == phase)
            {
                filteredIndices[filterCount++] = index;
            }
            index++;
        }
        yield return filteredIndices[..filterCount];
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
        DataStateIsValid = false;
    }

    /// <summary>
    /// Instantiates properties panel object and registers the change callback function
    /// </summary>
    /// <param name="eventArgs"></param>
    protected override void OnAdded(NodeAddedEventArgs eventArgs)
    {
        base.OnAdded(eventArgs);
        rangeManager = rangeManagerProvider.Resolve(InstanceId);
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

    private (string Name, double Min, double Max)[] GetRanges(IReadOnlyCollection<IIonTypeInfo> ionTypeInfo)
    {
        if (rangeManager is null) return Array.Empty<(string Name, double Min, double Max)>();
        var typeRanges = rangeManager.GetRanges();
        IEnumerable<(string Name, double Min, double Max)> basicRanges = new List<(string Name, double Min, double Max)>();
        foreach (var info in ionTypeInfo)
        {
            basicRanges = basicRanges.Concat(typeRanges[info.Formula].Ranges.Select(r => (info.Name, r.Min, r.Max)));
        }
        return basicRanges.OrderBy(r => r.Min).ToArray();
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