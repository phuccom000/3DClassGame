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
	List<int> waterTriangles = new List<int>();
	Material[] materials = new Material[3];
	List<Vector2> uvs = new List<Vector2>();
	List<Color> colors = new List<Color>();
	List<Vector3> normals = new List<Vector3>();

	public Vector3Int position;

	private bool _isActive;
	// clip 26
	ChunkData chunkData;


	public Chunk(ChunkCoord _coord)
	{
		coord = _coord;

		chunkObject = new GameObject();
		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();

		materials[0] = World.Instance.material;
		materials[1] = World.Instance.transparentMaterial;
		materials[2] = World.Instance.waterMaterial;

		meshRenderer.materials = materials;

		chunkObject.transform.SetParent(World.Instance.transform);
		chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
		chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

		//DO NOT CHANGE
		position = Vector3Int.FloorToInt(chunkObject.transform.position);

		chunkData = World.Instance.worldData.RequestChunk(new Vector2Int(position.x, position.z), true);
		chunkData.chunk = this;

		World.Instance.chunksToUpdate.Add(this);

		if (World.Instance.settings.enableAnimatedChunks)
			chunkObject.AddComponent<ChunkLoadAnimation>();
	}



	public void UpdateChunk()
	{
		ClearMeshData();

		for (int y = 0; y < VoxelData.ChunkHeight; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					if (chunkData.map[x, y, z].properties.isSolid)
						UpdateMeshData(new Vector3Int(x, y, z));
				}
			}
		}

		World.Instance.chunksToDraw.Enqueue(this);

	}

	void ClearMeshData()
	{
		vertexIndex = 0;
		vertices.Clear();
		triangles.Clear();
		transparentTriangles.Clear();
		waterTriangles.Clear();
		uvs.Clear();
		colors.Clear();
		normals.Clear();
	}
	public bool isActive
	{
		get { return _isActive; }
		set
		{
			_isActive = value;
			chunkObject?.SetActive(value);
		}
	}

	public void EditVoxel(Vector3Int pos, byte newID)
	{
		int xCheck = pos.x;
		int yCheck = pos.y;
		int zCheck = pos.z;

		xCheck -= position.x;
		zCheck -= position.z;

		chunkData.ModifyVoxel(new Vector3Int(xCheck, yCheck, zCheck), newID, World.Instance._player.orientation);

		UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
	}

	public void UpdateSurroundingVoxels(int x, int y, int z)
	{
		Vector3 thisVoxel = new Vector3(x, y, z);
		for (int p = 0; p < 6; p++)
		{
			Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

			if (!chunkData.IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
			{
				World.Instance.AddChunkToUpdate(World.Instance.GetChunkFromVector3(currentVoxel + position), true);
			}
		}
	}

	public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
	{
		Vector3Int posCheck = Vector3Int.FloorToInt(pos);

		posCheck.x -= position.x;
		posCheck.z -= position.z;

		return chunkData.map[posCheck.x, posCheck.y, posCheck.z];
	}



	void UpdateMeshData(Vector3Int pos)
	{
		int x = pos.x;
		int y = pos.y;
		int z = pos.z;

		VoxelState voxel = chunkData.map[x, y, z];

		float rot = 0f;
		rot = voxel.orientation switch
		{
			0 => 180f,
			5 => 270f,
			1 => 0f,
			_ => 90f,
		};

		for (int p = 0; p < 6; p++)
		{
			int translatedP = p;

			if (voxel.orientation != 1)
			{
				if (voxel.orientation == 0)
				{
					if (p == 0) translatedP = 1;
					else if (p == 1) translatedP = 0;
					else if (p == 4) translatedP = 5;
					else if (p == 5) translatedP = 4;
				}
				else if (voxel.orientation == 5)
				{
					if (p == 0) translatedP = 5;
					else if (p == 1) translatedP = 4;
					else if (p == 4) translatedP = 0;
					else if (p == 5) translatedP = 1;
				}
				else if (voxel.orientation == 4)
				{
					if (p == 0) translatedP = 4;
					else if (p == 1) translatedP = 5;
					else if (p == 4) translatedP = 1;
					else if (p == 5) translatedP = 0;
				}
			}

			VoxelState neighbour = chunkData.map[x, y, z].neighbours[translatedP];
			if (neighbour != null && neighbour.properties.renderNeighborFaces && !(voxel.properties.isWater && chunkData.map[x, y + 1, z].properties.isWater))
			{
				float lightLevel = neighbour.lightAsFloat;
				int faceVertCount = 0;

				for (int i = 0; i < voxel.properties.meshData.faces[p].vertData.Length; i++)
				{
					VertData vertData = voxel.properties.meshData.faces[p].GetVertData(i);
					vertices.Add(pos + vertData.GetRotatedPosition(new Vector3(0, rot, 0)));
					normals.Add(VoxelData.faceChecks[p]);
					colors.Add(new Color(0, 0, 0, lightLevel));
					if (voxel.properties.isWater)
						uvs.Add(voxel.properties.meshData.faces[p].vertData[i].uv);
					else
						AddTexture(voxel.properties.GetTextureID(p), vertData.uv);
					faceVertCount++;
				}

				if (!voxel.properties.renderNeighborFaces)
				{
					for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
					{
						triangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
					}
				}
				else
				{
					if (voxel.properties.isWater)
					{
						for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
							waterTriangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
					}
					else
					{
						for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
							transparentTriangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
					}
				}

				vertexIndex += faceVertCount;
			}
		}
	}

	public void CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();

		mesh.subMeshCount = 3;
		mesh.SetTriangles(triangles.ToArray(), 0);
		mesh.SetTriangles(transparentTriangles.ToArray(), 1);
		mesh.SetTriangles(waterTriangles.ToArray(), 2);

		//mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.colors = colors.ToArray();
		mesh.normals = normals.ToArray();

		meshFilter.mesh = mesh;
	}

	void AddTexture(int textureID, Vector2 uv)
	{
		float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
		float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

		x *= VoxelData.NormalizedBlockTextureSize;
		y *= VoxelData.NormalizedBlockTextureSize;

		y = 1f - y - VoxelData.NormalizedBlockTextureSize;

		x += VoxelData.NormalizedBlockTextureSize * uv.x;
		y += VoxelData.NormalizedBlockTextureSize * uv.y;

		uvs.Add(new Vector2(x, y));
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

		x = Mathf.FloorToInt(xCheck / VoxelData.ChunkWidth);
		z = Mathf.FloorToInt(zCheck / VoxelData.ChunkWidth);
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