using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

	public MeshRenderer meshRenderer;
	public MeshFilter meshFilter;

	int vertexIndex = 0;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();
	List<Vector2> uvs = new List<Vector2>();
	void Start()
	{
		for (int y = 0; y < VoxelData.ChunkHeigth; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					AddVoxelDataToChunk(new Vector3(x, y, z));
				}
			}
		}
		CreateMesh();
	}

	void AddVoxelDataToChunk(Vector3 pos)
	{
		for (int p = 0; p < 6; p++)
		{
			for (int i = 0; i < 6; i++)
			{

				int triangleIndex = VoxelData.voxelTris[p, i];
				vertices.Add(VoxelData.voxelVerts[triangleIndex] + pos);
				triangles.Add(vertexIndex);

				uvs.Add(VoxelData.voxelUvs[i]);

				vertexIndex++;
			}
		}
	}

	void CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();

		mesh.RecalculateNormals();

		meshFilter.mesh = mesh;

	}
}
