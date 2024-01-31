using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MultivariateStatisticalAnalysis;

internal static class DataFormatExtensions
{
    /// <summary>
    /// Decompose a Vector3 array into 3 float arrays of equal length representing X, Y, and Z components
    /// </summary>
    /// <param name="positions"></param>
    /// <returns></returns>
    public static (float[] X, float[] Y, float[] Z) Decompose(this Vector3[] positions)
    {
        var x = new float[positions.Length];
        var y = new float[positions.Length];
        var z = new float[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            x[i] = positions[i].X;
            y[i] = positions[i].Y;
            z[i] = positions[i].Z;
        }
        return (x, y, z);
    }
}
