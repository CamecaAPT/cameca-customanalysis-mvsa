using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MultivariateStatisticalAnalysis;

internal static class VoxelFileHelpers
{
    /// <summary>
    /// Parse uniformly distributed voxel centers and phase identifiers from an APT file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="voxelCentersSection"></param>
    /// <param name="phaseSection"></param>
    /// <param name="flipXY"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static (Vector3[] VoxelCenters, float[] Phases) ParseVoxelInfo(string filePath, string voxelCentersSection, string phaseSection)
    {
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        // Skip APT header
        stream.Seek(540, SeekOrigin.Begin);

        string[] sections = new string[] { voxelCentersSection, phaseSection };
        Dictionary<string, float[]> data = new();

        byte[] intBuffer = new byte[Marshal.SizeOf(typeof(int))];
        byte[] longBuffer = new byte[Marshal.SizeOf(typeof(long))];
        byte[] sectionNameBuffer = new byte[64];

        // Get section name (in case order changes) and metadata
        while (stream.Position < stream.Length)
        {
            stream.Seek(4, SeekOrigin.Current);
            // Section header size
            stream.Read(intBuffer, 0, intBuffer.Length);
            int headerSize = BitConverter.ToInt32(intBuffer);
            // Advance
            stream.Seek(4, SeekOrigin.Current);
            // Get section name
            stream.Read(sectionNameBuffer, 0, sectionNameBuffer.Length);
            string sectionName = System.Text.Encoding.Unicode.GetString(sectionNameBuffer).TrimEnd('\0');
            // Advance
            stream.Seek(56, SeekOrigin.Current);
            // Get record count
            stream.Read(longBuffer, 0, longBuffer.Length);
            long recordCount = BitConverter.ToInt64(longBuffer);
            // Get data bytes count
            stream.Read(longBuffer, 0, longBuffer.Length);
            long byteCount = BitConverter.ToInt64(longBuffer);
            // Advance past any extra data
            int extraSize = (headerSize - 148);
            stream.Seek(extraSize, SeekOrigin.Current);

            // Read section data
            var dataBuffer = new byte[byteCount];
            stream.Read(dataBuffer, 0, dataBuffer.Length);
            if (sections.Contains(sectionName))
            {
                data.Add(sectionName, MemoryMarshal.Cast<byte, float>(dataBuffer.AsSpan()).ToArray());
            }
        }

        if (data.Keys.Except(sections).Any()) { throw new InvalidOperationException($"Missing sections: only parsed [{string.Join(", ", data.Keys)}] - requires [{string.Join(", ", sections)}]"); }

        var centers = MemoryMarshal.Cast<float, Vector3>(data[voxelCentersSection]).ToArray();
        return (centers, data[phaseSection]);
    }

    /// <summary>
    /// Resolve minimum and maximum points that fully bound all voxel values
    /// </summary>
    /// <param name="centers"></param>
    /// <returns></returns>
    public static (Vector3 Min, Vector3 Max) GetVoxelExtants(Vector3[] centers)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (int i = 0; i < centers.Length; i++)
        {
            if (centers[i].X < min.X)
            {
                min = min with { X = centers[i].X, };
            }
            if (centers[i].Y < min.Y)
            {
                min = min with { Y = centers[i].Y, };
            }
            if (centers[i].Z < min.Z)
            {
                min = min with { Z = centers[i].Z, };
            }

            if (centers[i].X > max.X)
            {
                max = max with { X = centers[i].X, };
            }
            if (centers[i].Y > max.Y)
            {
                max = max with { Y = centers[i].Y, };
            }
            if (centers[i].Z > max.Z)
            {
                max = max with { Z = centers[i].Z, };
            }
        }

        return (min, max);
    }
}
