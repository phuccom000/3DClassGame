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
	List<int> transparentTriangles = new List<int>();
	Material[] materials = new Material[2];
	List<Vector2> uvs = new List<Vector2>();
	List<Color> colors = new List<Color>();

	public Vector3Int position;
	public VoxelState[,,] voxelMap = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
	public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

	[SerializeField] World world;

	private bool _isActive;
	public bool isVoxelMapPopulated = false;


	public Chunk(ChunkCoord _coord, World _world)
	{
		world = _world;
		coord = _coord;

	}

	public void Init()
	{
		chunkObject = new GameObject();
		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();

		//materials[0] = world.material;
		//materials[1] = world.transparentMaterial;
		meshRenderer.material = world.material;

		chunkObject.transform.SetParent(world.transform);
		chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
		chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

		position.x = Mathf.FloorToInt(chunkObject.transform.position.x);
		position.y = Mathf.FloorToInt(chunkObject.transform.position.y);
		position.z = Mathf.FloorToInt(chunkObject.transform.position.z);


		PopulateVoxelMap();
	}

	void PopulateVoxelMap()
	{
		for (int y = 0; y < VoxelData.ChunkHeight; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));
				}
			}
		}

		isVoxelMapPopulated = true;
		lock (world.ChunkUpdateThreadLock)
		{
			world.chunksToUpdate.Add(this);
		}
	}

	public void UpdateChunk()
	{

		while (modifications.Count > 0)
		{
			VoxelMod v = modifications.Dequeue();
			Vector3 pos = v.position -= position;
			voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;
		}

		ClearMeshData();

		CalculateLight();

		for (int y = 0; y < VoxelData.ChunkHeight; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					if (world.blockTypes[voxelMap[x, y, z].id].isSolid)
						UpdateMeshData(new Vector3Int(x, y, z));
				}
			}
		}
		lock (world.chunksToDraw)
		{
			world.chunksToDraw.Enqueue(this);
		}

	}

	void CalculateLight()
	{
		Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();
		for (int x = 0; x < VoxelData.ChunkWidth; x++)
		{
			for (int z = 0; z < VoxelData.ChunkWidth; z++)
			{
				float lightRay = 1f;
				for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--)
				{
					VoxelState thisVoxel = voxelMap[x, y, z];
					if (thisVoxel.id > 0 && world.blockTypes[thisVoxel.id].transparency < lightRay)
						lightRay = world.blockTypes[thisVoxel.id].transparency;

					thisVoxel.globalLightPercent = lightRay;
					voxelMap[x, y, z] = thisVoxel;

					if (lightRay > VoxelData.lightFalloff)
						litVoxels.Enqueue(new Vector3Int(x, y, z));
				}

			}
		}
		while (litVoxels.Count > 0)
		{
			Vector3Int v = litVoxels.Dequeue();
			for (int p = 0; p < 6; p++)
			{
				Vector3 currentVoxel = v + VoxelData.faceChecks[p];
				Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

				if (IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z))
				{
					if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff)
					{
						voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;
						if (voxelMap[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
							litVoxels.Enqueue(neighbor);
					}

				}
			}
		}
	}

	void ClearMeshData()
	{
		vertexIndex = 0;
		vertices.Clear();
		triangles.Clear();
		transparentTriangles.Clear();
		uvs.Clear();
		colors.Clear();
	}
	public bool isActive
	{
		get { return _isActive; }
		set
		{
			_isActive = value;
			if (chunkObject != null)
				chunkObject.SetActive(value);
		}
	}


	public bool isEditable
	{
		get
		{
			if (!isVoxelMapPopulated)
				return false;
			else
				return true;
		}
	}

	bool IsVoxelInChunk(int x, int y, int z)
	{
		if (x < 0 || x >= VoxelData.ChunkWidth || y < 0 || y >= VoxelData.ChunkHeight || z < 0 || z >= VoxelData.ChunkWidth)
		{
			return false;
		}
		else return true;
	}

	public void EditVoxel(Vector3Int pos, byte newID)
	{
		int xCheck = pos.x;
		int yCheck = pos.y;
		int zCheck = pos.z;

		xCheck -= position.x;
		zCheck -= position.z;

		voxelMap[xCheck, yCheck, zCheck].id = newID;

		lock (world.ChunkUpdateThreadLock)
		{
			world.chunksToUpdate.Insert(0, this);
			UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
		}
	}

	public void UpdateSurroundingVoxels(int x, int y, int z)
	{
		Vector3 thisVoxel = new Vector3(x, y, z);
		for (int p = 0; p < 6; p++)
		{
			Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

			if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
			{
				world.chunksToUpdate.Insert(0, world.GetChunkFromVector3(currentVoxel + position));
			}
		}
	}
	public VoxelState CheckVoxel(Vector3Int pos)
	{
		int x = pos.x;
		int y = pos.y;
		int z = pos.z;

		if (!IsVoxelInChunk(x, y, z))
		{
			return world.GetVoxelState(pos + position);
		}

		return voxelMap[x, y, z];
	}

	public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
	{
		Vector3Int posCheck = Vector3Int.FloorToInt(pos);

		posCheck.x -= position.x;
		posCheck.z -= position.z;

		return voxelMap[posCheck.x, posCheck.y, posCheck.z];
	}



	void UpdateMeshData(Vector3Int pos)
	{
		int x = pos.x;
		int y = pos.y;
		int z = pos.z;

		byte blockID = voxelMap[x, y, z].id;
		//bool isTransparent = world.blockTypes[blockID].renderNeighborFaces;
		for (int p = 0; p < 6; p++)
		{
			VoxelState neighbor = CheckVoxel(pos + VoxelData.faceChecks[p]);
			if (neighbor != null && world.blockTypes[neighbor.id].renderNeighborFaces)
			{
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

				AddTexture(world.blockTypes[blockID].GetTextureID(p));

				float lightLevel = neighbor.globalLightPercent;

				colors.Add(new Color(0, 0, 0, lightLevel));
				colors.Add(new Color(0, 0, 0, lightLevel));
				colors.Add(new Color(0, 0, 0, lightLevel));
				colors.Add(new Color(0, 0, 0, lightLevel));

				// if (!isTransparent)
				// {
				triangles.Add(vertexIndex);
				triangles.Add(vertexIndex + 1);
				triangles.Add(vertexIndex + 2);
				triangles.Add(vertexIndex + 2);
				triangles.Add(vertexIndex + 1);
				triangles.Add(vertexIndex + 3);
				// }
				// else
				// {
				// 	transparentTriangles.Add(vertexIndex);
				// 	transparentTriangles.Add(vertexIndex + 1);
				// 	transparentTriangles.Add(vertexIndex + 2);
				// 	transparentTriangles.Add(vertexIndex + 2);
				// 	transparentTriangles.Add(vertexIndex + 1);
				// 	transparentTriangles.Add(vertexIndex + 3);
				// }
				vertexIndex += 4;
			}
		}
	}

	public void CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();

		// mesh.subMeshCount = 2;
		// mesh.SetTriangles(triangles.ToArray(), 0);
		// mesh.SetTriangles(transparentTriangles.ToArray(), 1);
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.colors = colors.ToArray();
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

	public ChunkCoord()
	{
		x = 0;
		z = 0;
	}

	public ChunkCoord(int _x, int _z)
	{
		x = _x;
		z = _z;
	}

	public ChunkCoord(Vector3 pos)
	{
		int xCheck = Mathf.FloorToInt(pos.x);
		int zCheck = Mathf.FloorToInt(pos.z);

		x = xCheck / VoxelData.ChunkWidth;
		z = zCheck / VoxelData.ChunkWidth;
	}

	public bool Equals(ChunkCoord other)
	{

		if (other == null)
		{
			return false;
		}
		else if (other.x == x && other.z == z)
			return true;
		else
			return false;
	}


}

public class VoxelState
{
	public byte id;
	public float globalLightPercent;

	public VoxelState()
	{
		id = 0;
		globalLightPercent = 0f;
	}

	public VoxelState(byte _id)
	{
		id = _id;
		globalLightPercent = 0f;
	}

}