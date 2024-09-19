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
	//List<int> waterTriangles = new List<int>();
	Material[] materials = new Material[2];
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
	}

	public void Init()
	{
		chunkObject = new GameObject();
		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();

		materials[0] = World.Instance.material;
		materials[1] = World.Instance.transparentMaterial;
		//materials[2] = World.Instance.waterMaterial;

		meshRenderer.materials = materials;

		chunkObject.transform.SetParent(World.Instance.transform);
		chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
		chunkObject.name = "Chunk " + coord.x + ", " + coord.z;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);

        lock (World.Instance.ChunkUpdateThreadLock)
            World.Instance.chunksToUpdate.Add(this);

        if (World.Instance.settings.enableAnimatedChunks)
            chunkObject.AddComponent<ChunkLoadAnimation>();

        //DO NOT CHANGE
        position = Vector3Int.FloorToInt(chunkObject.transform.position);
	}

	

	public void UpdateChunk()
	{

		ClearMeshData();

		CalculateLight();

		for (int y = 0; y < VoxelData.ChunkHeight; y++)
		{
			for (int x = 0; x < VoxelData.ChunkWidth; x++)
			{
				for (int z = 0; z < VoxelData.ChunkWidth; z++)
				{
					if (World.Instance.blocktypes[chunkData.map[x, y, z].id].isSolid)
						UpdateMeshData(new Vector3Int(x, y, z));
				}
			}
		}
		lock (World.Instance.chunksToDraw)
		{
			World.Instance.chunksToDraw.Enqueue(this);
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
					VoxelState thisVoxel = chunkData.map[x, y, z];
					if (thisVoxel.id > 0 && World.Instance.blocktypes[thisVoxel.id].transparency < lightRay)
						lightRay = World.Instance.blocktypes[thisVoxel.id].transparency;

					thisVoxel.globalLightPercent = lightRay;
					chunkData.map[x, y, z] = thisVoxel;

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
					if (chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff)
					{
						chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;
						if (chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
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
		//waterTriangles.Clear(); // Clip 29
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
			if (chunkObject != null)
				chunkObject.SetActive(value);
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

		chunkData.map[xCheck, yCheck, zCheck].id = newID;
        World.Instance.worldData.AddToModifiedChunkList(chunkData);

        lock (World.Instance.ChunkUpdateThreadLock)
		{
			World.Instance.AddChunkToUpdate(this, true);
			UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
		}

		// Clip 27
		// int xCheck = pos.x;
		// int yCheck = pos.y;
		// int zCheck = pos.z;

		// xCheck -= position.x;
		// zCheck -= position.z;

		// chunkData.ModifyVoxel(new Vector3Int(xCheck, yCheck, zCheck), newID, World.Instance._player.orientation);
		// UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
	}

	public void UpdateSurroundingVoxels(int x, int y, int z)
	{
		Vector3 thisVoxel = new Vector3(x, y, z);
		for (int p = 0; p < 6; p++)
		{
			Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

			if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
			{
				World.Instance.AddChunkToUpdate(World.Instance.GetChunkFromVector3(currentVoxel + position), true);
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
			return World.Instance.GetVoxelState(pos + position);
		}

		return chunkData.map[x, y, z];
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

		byte blockID = chunkData.map[x, y, z].id;
		//VoxelState voxel = chunkData.map[x, y, z]; // - clip 26

		// Clip 27
		// float rot = 0f;
		// switch(voxel.orientation) {
		// 	case 0:
		// 		rot = 180f;
		// 		break;
		// 	case 5: 
		// 		rot = 270f;
		// 		break;
		// 	case 1: 
		// 		rot = 0f;
		// 		break;
		// 	default: 
		// 		rot = 90f;
		// 		break;
		// }

		for (int p = 0; p < 6; p++)
		{
			// Clip 27
			// ----- 
			// int translatedP = p;

			// if (voxel.orientation != 1) {

			// 	if (voxel.orientation == 0) {
			// 		if (p == 0) translatedP = 1;
			// 		else if (p == 1) translatedP = 0;
			// 		else if (p == 4) translatedP = 5;
			// 		else if (p == 5) translatedP = 4;
			// 	}
			// 	else if (voxel.orientation == 5) {
			// 		if (p == 0) translatedP = 5;
			// 		else if (p == 1) translatedP = 4;
			// 		else if (p == 4) translatedP = 0;
			// 		else if (p == 5) translatedP = 1;
			// 	} 
			// 	else if (voxel.orientation == 4) {
			// 		if (p == 0) translatedP = 4;
			// 		else if (p == 1) translatedP = 5;
			// 		else if (p == 4) translatedP = 1;
			// 		else if (p == 5) translatedP = 0;
			// 	} 
			// }
			// -----

			// clip 26
			// ---------------------------------
			// VoxelState neighbour = chunkData.map[x,y,z].neighbours[p];
			// //VoxelState neighbour = chunkData.map[x,y,z].neighbours[translatedP]; // Clip 27
			// if (neighbour != null && neighbour.properties.renderNeighborFaces) {
			// 	float lightLevel = neighbour.lightAsFloat;
			// 	int faveVertCount = 0;
				
			// 	for(int i = 0; i < voxel.properties.meshData.faces[p].vertData.Length; i++) {
					
			// 		VertData vertData = voxel.properties.meshData.faces[p].GetVertData(i);
			// 		vertices.Add(pos + voxel.properties.meshData.faces[p].vertData[i].GetRotatedPosition(new Vector3(0, rot, 0)));
			// 		normals.Add(VoxelData.faceChecks[p]);
			// 		colors.Add(new Color(0,0,0, lightLevel));
			// 		AddTexture(voxel.properties.GetTextureID(p), vertData.uv);
			// 		faveVertCount++;

			// 	}

			// 	if (!voxel.properties.renderNeighborFaces) {
			// 		for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
			// 		{
			// 			triangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
			// 		}
			// 	}
			// 	else {
			// 		for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
			// 		{
			// 			transparentTriangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
			// 		}
			// 	}

			// 	vertexIndex += faveVertCount;
			// }
			// ------------------------------

			// Delete all of the below code accordinng to clip 26 - 13:32
			// Delete from here
			
			VoxelState neighbor = CheckVoxel(pos + VoxelData.faceChecks[p]);
			// Vid 28 - 15:10 min
			if (neighbor != null && World.Instance.blocktypes[neighbor.id].renderNeighborFaces)
			{
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
				vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

				for (int i = 0; i < 4; i++)
					normals.Add(VoxelData.faceChecks[p]);

				AddTexture(World.Instance.blocktypes[blockID].GetTextureID(p));

				float lightLevel = neighbor.globalLightPercent;

				colors.Add(new Color(0, 0, 0, lightLevel));
				colors.Add(new Color(0, 0, 0, lightLevel));
				colors.Add(new Color(0, 0, 0, lightLevel));
				colors.Add(new Color(0, 0, 0, lightLevel));

				if (!World.Instance.blocktypes[neighbor.id].renderNeighborFaces)
				{
					triangles.Add(vertexIndex);
					triangles.Add(vertexIndex + 1);
					triangles.Add(vertexIndex + 2);
					triangles.Add(vertexIndex + 2);
					triangles.Add(vertexIndex + 1);
					triangles.Add(vertexIndex + 3);
				}
				else
				{
					transparentTriangles.Add(vertexIndex);
					transparentTriangles.Add(vertexIndex + 1);
					transparentTriangles.Add(vertexIndex + 2);
					transparentTriangles.Add(vertexIndex + 2);
					transparentTriangles.Add(vertexIndex + 1);
					transparentTriangles.Add(vertexIndex + 3);
				}

				// Vid 28 - 7 min

				vertexIndex += 4;
			}
			
			// Delete to here
		}
	}

	public void CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();

		mesh.subMeshCount = 2;
		mesh.SetTriangles(triangles.ToArray(), 0);
		mesh.SetTriangles(transparentTriangles.ToArray(), 1);
		//mesh.SetTriangles(waterTriangles.ToArray(), 2);

		//mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.colors = colors.ToArray();
		mesh.normals = normals.ToArray();

		meshFilter.mesh = mesh;
	}

	// clip 26
	// void AddTexture(int textureID, Vector2 uv)
	// {
	// 	float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
	// 	float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

	// 	x *= VoxelData.NormalizedBlockTextureSize;
	// 	y *= VoxelData.NormalizedBlockTextureSize;

	// 	y = 1f - y - VoxelData.NormalizedBlockTextureSize;

	// 	// Get rid of this according to clip 26 - 17:39
	// 	// uvs.Add(new Vector2(x, y));
	// 	// uvs.Add(new Vector2(x, y + VoxelData.NormalizeBlockTextureSize));
	// 	// uvs.Add(new Vector2(x + VoxelData.NormalizeBlockTextureSize, y));
	// 	// uvs.Add(new Vector2(x + VoxelData.NormalizeBlockTextureSize, y + VoxelData.NormalizeBlockTextureSize));

	// 	x += VoxelData.NormalizedBlockTextureSize * uv.x;
	// 	y += VoxelData.NormalizedBlockTextureSize * uv.y;

	// 	uvs.Add(new Vector2(x,y));
	// }

	void AddTexture(int textureID)
	{
		float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
		float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

		x *= VoxelData.NormalizedBlockTextureSize;
		y *= VoxelData.NormalizedBlockTextureSize;

		y = 1f - y - VoxelData.NormalizedBlockTextureSize;

		uvs.Add(new Vector2(x, y));
		uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
		uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
		uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
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

	// public BlockType properties
	// {

	// 	get { return World.Instance.blockTypes[id]; }

	// }

}