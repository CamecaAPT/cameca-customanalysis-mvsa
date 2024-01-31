using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MultivariateStatisticalAnalysis;

internal static class IonDataExtensions
{
    /// <summary>
    /// Load standard position and mass data from the IIonData structure as arrays of primitives (float[])
    /// </summary>
    /// <param name="ionData"></param>
    /// <returns></returns>
    public static (Vector3[] Position, float[] Mass, byte[] IonTypes) LoadStandardSectionsAsArray(this IIonData ionData)
    {
        // Our analysis philosophy is to generally compute on-demand
        // This is a little tricky to get at trivially, as our IIonData instance are accessed through a sequence of chunks of data
        // This is mainly to circumvent issues with hitting single object size caps in C# and limits on array length (i.e. support data sets with ion counts > Int32.MaxValue)
        // There should be some samples on the GitHub if necessary, but it's probably safe to just assume < 2 billion ions and load all into arrays
        // Note that this could fail on massive datasets if not eventually adjusted to support them

        Vector3[] positions = new Vector3[(int)ionData.IonCount];
        float[] masses = new float[(int)ionData.IonCount];
        byte[] ionTypes = new byte[(int)ionData.IonCount];
        int chunkOffset = 0;
        foreach (var chunk in ionData.CreateSectionDataEnumerable(IonDataSectionName.Position, IonDataSectionName.Mass, IonDataSectionName.IonType))
        {
            var chunkPos = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position);
            var chunkMas = chunk.ReadSectionData<float>(IonDataSectionName.Mass);
            var chunkTyp = chunk.ReadSectionData<byte>(IonDataSectionName.IonType);
            chunkPos.Span.CopyTo(positions.AsSpan().Slice(chunkOffset));
            chunkMas.Span.CopyTo(masses.AsSpan().Slice(chunkOffset));
            chunkTyp.Span.CopyTo(ionTypes.AsSpan().Slice(chunkOffset));
            chunkOffset += (int)chunk.Length;
        }
        return (positions, masses, ionTypes);
    }
}
