using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
	public ChunkCoord coord;
	GameObject chunkObject;
	MeshRenderer meshRenderer;
	MeshFilter meshFilter;

	int vertexIndex = 0;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();
	List<Vector2> uvs = new List<Vector2>();
	byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
	[SerializeField] World world;

	public Chunk(World _world, ChunkCoord _coord)
	{
		world = _world;
		coord = _coord;

		chunkObject = new GameObject();
		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();

		meshRenderer.material = world.material;
		chunkObject.transform.SetParent(world.transform);
		chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
		chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

		PopulateVoxelMap();
		CreateMeshData();
		CreateMesh();
	}
	void CreateMeshData()
	{
		for (int y = 0; y < VoxelData.ChunkHeight; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					AddVoxelDataToChunk(new Vector3(x, y, z));
				}
			}
		}
	}

	bool CheckVoxel(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.x);
		int y = Mathf.FloorToInt(pos.y);
		int z = Mathf.FloorToInt(pos.z);

		if (x < 0 || x >= VoxelData.ChunkWidth || y < 0 || y >= VoxelData.ChunkHeight || z < 0 || z >= VoxelData.ChunkWidth)
		{
			return false;
		}

		return world.blockTypes[voxelMap[x, y, z]].isSolid;
	}

	void PopulateVoxelMap()
	{
		for (int y = 0; y < VoxelData.ChunkHeight; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					voxelMap[x, y, z] = 2;
				}
			}
		}
	}
	void AddVoxelDataToChunk(Vector3 pos)
	{
		for (int p = 0; p < 6; p++)
		{
			if (!CheckVoxel(pos + VoxelData.faceChecks[p]))
			{
				int blockID = voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];

				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

				AddTexture(world.blockTypes[blockID].GetTextureID(p));

				triangles.Add(vertexIndex);
				triangles.Add(vertexIndex + 1);
				triangles.Add(vertexIndex + 2);
				triangles.Add(vertexIndex + 2);
				triangles.Add(vertexIndex + 1);
				triangles.Add(vertexIndex + 3);

				vertexIndex += 4;
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

	void AddTexture(int textureID)
	{
		float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
		float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

		x *= VoxelData.NormalizeBlockTextureSize;
		y *= VoxelData.NormalizeBlockTextureSize;

		y = 1f - y - VoxelData.NormalizeBlockTextureSize;

		uvs.Add(new Vector2(x, y));
		uvs.Add(new Vector2(x, y + VoxelData.NormalizeBlockTextureSize));
		uvs.Add(new Vector2(x + VoxelData.NormalizeBlockTextureSize, y));
		uvs.Add(new Vector2(x + VoxelData.NormalizeBlockTextureSize, y + VoxelData.NormalizeBlockTextureSize));
	}
}

public class ChunkCoord
{
	public int x;
	public int z;

	public ChunkCoord(int _x, int _z)
	{
		x = _x;
		z = _z;
	}

}