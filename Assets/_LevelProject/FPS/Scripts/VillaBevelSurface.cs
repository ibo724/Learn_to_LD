using UnityEngine;

/// <summary>Projects cosmetic marks onto the unit bevel hull while retaining simple box collision.</summary>
[DisallowMultipleComponent]
public sealed class VillaBevelSurface : MonoBehaviour
{
    private const float Inset=.06f;
    private static readonly Vector3[] PlaneNormals=CreateNormals();

    private static Vector3[] CreateNormals()
    {
        Vector3[] normals=new Vector3[26];
        int index=0;
        for (int x=-1;x<=1;x++)
            for (int y=-1;y<=1;y++)
                for (int z=-1;z<=1;z++)
                    if (x!=0 || y!=0 || z!=0) normals[index++]=new Vector3(x,y,z);
        return normals;
    }

    public bool TryProject(Vector3 point,Vector3 normal,out Vector3 projectedPoint,out Vector3 projectedNormal)
    {
        projectedPoint=point; projectedNormal=normal;
        if (!VillaQueries.IsFinite(point) || !VillaQueries.IsFinite(normal) || normal.sqrMagnitude<.001f) return false;
        normal.Normalize();
        Vector3 origin=transform.InverseTransformPoint(point+normal*.02f);
        Vector3 direction=transform.InverseTransformVector(-normal);
        float entry=0f,exit=float.PositiveInfinity;
        Vector3 entryNormal=Vector3.zero;
        foreach (Vector3 plane in PlaneNormals)
        {
            int axes=(plane.x!=0 ? 1 : 0)+(plane.y!=0 ? 1 : 0)+(plane.z!=0 ? 1 : 0);
            float limit=axes==1 ? .5f : axes==2 ? 1f-Inset : 1.5f-2f*Inset;
            float distance=limit-Vector3.Dot(plane,origin);
            float slope=Vector3.Dot(plane,direction);
            if (Mathf.Abs(slope)<.000001f)
            {
                if (distance<-.000001f) return false;
                continue;
            }
            float crossing=distance/slope;
            if (slope<0f)
            {
                if (crossing>entry) { entry=crossing; entryNormal=plane; }
            }
            else exit=Mathf.Min(exit,crossing);
            if (entry>exit) return false;
        }
        if (entryNormal.sqrMagnitude<.001f || float.IsInfinity(entry)) return false;
        projectedPoint=transform.TransformPoint(origin+direction*entry);
        projectedNormal=transform.worldToLocalMatrix.transpose.MultiplyVector(entryNormal).normalized;
        return VillaQueries.IsFinite(projectedPoint) && VillaQueries.IsFinite(projectedNormal);
    }
}
