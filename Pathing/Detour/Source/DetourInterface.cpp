#include "DetourInterface.hpp"
#include <cfloat>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <DetourAlloc.h>
#include <DetourCommon.h>
#include <DetourNavMesh.h>
#include <DetourNavMeshQuery.h>
#include <DetourStatus.h>
#include <functional>
#include <random>
#include <vector>

// RAII helper
struct RAII
{
	std::function<void()> cleaner;
	RAII(std::function<void()> cleaner) : cleaner(cleaner) {}
	~RAII() { this->cleaner(); }
};

// missing from Detour?
struct dtNavMeshSetHeader
{
	std::int32_t magic;
	std::int32_t version;
	std::int32_t numTiles;
	dtNavMeshParams params;
};

struct dtNavMeshTileHeader
{
	dtTileRef ref;
	std::int32_t size;
};

DLLEXPORT bool LoadNavMesh(char const *file, dtNavMesh **const mesh)
{
	// load the file
	auto fp = std::fopen(file, "rb");
	if (!fp)
		return false;

	// scope for fp closing
	{
		auto _fpRAII = RAII([=]
							{ std::fclose(fp); });

		dtNavMeshSetHeader header;
		fread(&header, sizeof(header), 1, fp);

		if (header.magic != 0x4d534554 || header.version != 1)
			return false;

		// init mesh and query
		*mesh = dtAllocNavMesh();
		auto status = (*mesh)->init(&header.params);
		if (dtStatusFailed(status))
		{
			dtFreeNavMesh(*mesh);
			*mesh = nullptr;
			return false;
		}
		if (header.numTiles > 0)
		{
			auto tileIdx = 0;
			while (tileIdx < header.numTiles)
			{
				dtNavMeshTileHeader tileHeader;
				fread(&tileHeader, sizeof(tileHeader), 1, fp);
				void *data;
				if (tileHeader.ref == 0 || tileHeader.size == 0 || (data = dtAlloc(tileHeader.size, DT_ALLOC_PERM)) == 0)
					break;
				memset(data, 0, tileHeader.size);
				fread(data, tileHeader.size, 1, fp);
				(*mesh)->addTile((unsigned char *)data, tileHeader.size, 1, tileHeader.ref, nullptr);
				tileIdx += 1;
			}
		}
	}
	return true;
}

DLLEXPORT bool FreeNavMesh(dtNavMesh *meshPtr)
{
	if (meshPtr)
		dtFreeNavMesh(meshPtr);
	return true;
}

DLLEXPORT bool CreateNavMeshQuery(dtNavMesh *mesh, dtNavMeshQuery **const query)
{
	*query = dtAllocNavMeshQuery();
	auto status = (*query)->init(mesh, MAX_NODES);
	if (dtStatusFailed(status))
	{
		dtFreeNavMeshQuery(*query);
		*query = nullptr;
		return false;
	}
	return true;
}

DLLEXPORT bool FreeNavMeshQuery(dtNavMeshQuery *queryPtr)
{
	if (queryPtr)
		dtFreeNavMeshQuery(queryPtr);
	return true;
}

DLLEXPORT dtStatus PathStraight(dtNavMeshQuery *query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], dtStraightPathOptions pathOptions, int *pointCount, float *pointBuffer, dtPolyFlags *pointFlags)
{
	*pointCount = 0;

	dtPolyRef startRef;
	dtPolyRef endRef;
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);

	dtStatus status;

	if (dtStatusSucceed(status = query->findNearestPoly(start, polyPickExt, &filter, &startRef, nullptr)) &&
		dtStatusSucceed(status = query->findNearestPoly(end, polyPickExt, &filter, &endRef, nullptr)))
	{
		int npolys = 0;
		dtPolyRef polys[MAX_POLY];
		dtStatus pathStatus = query->findPath(startRef, endRef, start, end, &filter, polys, &npolys, MAX_POLY);

		if (npolys <= 0)
			return pathStatus;

		if (dtStatusSucceed(pathStatus))
		{
			// Partial if findPath said so, OR if the last polygon found is not the target endRef.
			bool isPartialPath = (pathStatus & DT_PARTIAL_RESULT) != 0;
			if (polys[npolys - 1] != endRef)
				isPartialPath = true;

			dtPolyRef straightPathPolys[MAX_POLY];
			unsigned char straightPathFlags[MAX_POLY];
			status = query->findStraightPath(start, end, polys, npolys, pointBuffer, straightPathFlags, straightPathPolys, pointCount, MAX_POLY, pathOptions);

			if (dtStatusSucceed(status) && (*pointCount > 0))
			{
				for (int i = 0; i < *pointCount; ++i)
				{
					dtPolyRef ref = straightPathPolys[i];

					// Fall back to closest known corridor poly if ref is null.
					if (ref == 0)
						ref = (i == 0) ? polys[0] : (i >= npolys ? polys[npolys - 1] : polys[i]);

					unsigned short flags = 0;
					if (ref != 0)
						query->getAttachedNavMesh()->getPolyFlags(ref, &flags);

					pointFlags[i] = (dtPolyFlags) flags;
				}

				return isPartialPath ? (DT_SUCCESS | DT_PARTIAL_RESULT) : DT_SUCCESS;
			}
		}
		else
			status = pathStatus;
	}

	return status;
}

DLLEXPORT dtStatus MoveAlongSurface(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);

	dtPolyRef startRef;
	dtStatus status = query->findNearestPoly(start, polyPickExt, &filter, &startRef, nullptr);

	if (dtStatusFailed(status))
		return status;

	float resultPos[3];
	dtPolyRef visited[16];
	int nvisited = 0;

	status = query->moveAlongSurface(startRef, start, end, &filter, resultPos, visited, &nvisited, 16);

	if (dtStatusSucceed(status))
		dtVcopy(outputVector, resultPos);

	return status;
}

thread_local std::mt19937 rngMt = std::mt19937(std::random_device{}());
thread_local std::uniform_real_distribution<float> rng(0.0f, 1.0f);

float frand()
{
	return rng(rngMt);
}

DLLEXPORT dtStatus FindRandomPointAroundCircle(dtNavMeshQuery *query, float center[], float radius, float polyPickExt[], dtPolyFlags queryFilter[], float *outputVector)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	dtPolyRef centerRef;
	auto status = query->findNearestPoly(center, polyPickExt, &filter, &centerRef, nullptr);
	if (dtStatusSucceed(status))
	{
		dtPolyRef outRef;
		status = query->findRandomPointAroundCircle(centerRef, center, radius, &filter, frand, &outRef, outputVector);
	}
	return status;
}

DLLEXPORT dtStatus FindClosestPoint(dtNavMeshQuery *query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], float *outputVector)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	dtPolyRef centerRef;
	auto status = query->findNearestPoly(center, polyPickExt, &filter, &centerRef, nullptr);
	if (dtStatusSucceed(status))
		status = query->closestPointOnPoly(centerRef, center, outputVector, nullptr);
	return status;
}

DLLEXPORT dtStatus FindClosestPointInBox(dtNavMeshQuery *query, float boxCenter[], float boxExtents[], float referencePos[], dtPolyFlags queryFilter[], float *outputVector)
{
	const int MAX_POLYS = 32; // This limit can easily be reached, but this function shouldn't be used for large boxes.

	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);

	dtPolyRef polys[MAX_POLYS];
	int polyCount = 0;

	// Find all polygons overlapping the box.
	dtStatus status = query->queryPolygons(boxCenter, boxExtents, &filter, polys, &polyCount, MAX_POLYS);

	// Preserve detail flags from the query status.
	dtStatus detailFlags = status & DT_STATUS_DETAIL_MASK;

	if (dtStatusSucceed(status) && polyCount > 0)
	{
		float minDistSq = FLT_MAX;
		bool found = false;
		float tempVec[3];

		// Small tolerance to handle potential floating point errors on the edges.
		const float EPSILON = 1e-4f;

		for (int i = 0; i < polyCount; ++i)
		{
			// Find closest point on this specific polygon.
			query->closestPointOnPoly(polys[i], referencePos, tempVec, nullptr);

			// Ensure point is actually inside the box.
			bool isInsideBox = true;
			for (int axis = 0; axis < 3; ++axis)
			{
				float minBound = boxCenter[axis] - boxExtents[axis] - EPSILON;
				float maxBound = boxCenter[axis] + boxExtents[axis] + EPSILON;

				if (tempVec[axis] < minBound || tempVec[axis] > maxBound)
				{
					isInsideBox = false;
					break;
				}
			}

			if (!isInsideBox)
				continue; // Point is on a valid polygon, but the point itself is outside our volume.

			// Check distance.
			float dx = tempVec[0] - referencePos[0];
			float dy = tempVec[1] - referencePos[1];
			float dz = tempVec[2] - referencePos[2];
			float dSq = dx * dx + dy * dy + dz * dz;

			if (dSq < minDistSq)
			{
				minDistSq = dSq;
				outputVector[0] = tempVec[0];
				outputVector[1] = tempVec[1];
				outputVector[2] = tempVec[2];
				found = true;
			}
		}

		if (found)
			return DT_SUCCESS | detailFlags;
	}

	return DT_FAILURE | detailFlags;
}

DLLEXPORT dtStatus HasLineOfSight(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], bool *hasLos, float *outputVector)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);

	dtPolyRef startRef;
	dtStatus status = query->findNearestPoly(start, polyPickExt, &filter, &startRef, nullptr);

	if (dtStatusFailed(status))
		return status;

	dtRaycastHit raycastHit{};
	status = query->raycast(startRef, start, end, &filter, 0, &raycastHit, 0);

	if (dtStatusSucceed(status))
	{
		outputVector[0] = start[0] + (end[0] - start[0]) * raycastHit.t;
		outputVector[1] = start[1] + (end[1] - start[1]) * raycastHit.t;
		outputVector[2] = start[2] + (end[2] - start[2]) * raycastHit.t;
		*hasLos = raycastHit.t > 1.0f - 1e-4f;
	}

	return status;
}

DLLEXPORT dtStatus UpdateFlags(dtNavMesh* navMesh, dtPolyRef polyRefs[], int polyCount,  unsigned short flagsToRemove, unsigned short flagsToAdd)
{
	if (polyCount <= 0)
		return DT_SUCCESS;

	dtStatus status;
	std::vector<unsigned short> originalFlags;
	originalFlags.reserve(polyCount);

	for (int i = 0; i < polyCount; ++i)
	{
		unsigned short flags;
		status = navMesh->getPolyFlags(polyRefs[i], &flags);

		if (dtStatusFailed(status))
			return status;

		originalFlags.push_back(flags);
	}

	for (int i = 0; i < polyCount; ++i)
	{
		status = navMesh->setPolyFlags(polyRefs[i], (originalFlags[i] & ~flagsToRemove) | flagsToAdd);

		if (dtStatusFailed(status))
		{
			// Best-effort rollback.
			for (int j = 0; j < i; ++j)
				navMesh->setPolyFlags(polyRefs[j], originalFlags[j]);

			return status;
		}
	}

	return status;
}

DLLEXPORT dtStatus GetPolyAt(dtNavMeshQuery *query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], dtPolyRef polyRef[], float *point)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	return query->findNearestPoly(center, polyPickExt, &filter, polyRef, point);
}

DLLEXPORT dtStatus GetPolysInBox(dtNavMeshQuery *query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], dtPolyRef polyRefs[], int *polyCount, int maxPolys)
{
	dtQueryFilter filter;
	filter.setIncludeFlags(queryFilter[0]);
	filter.setExcludeFlags(queryFilter[1]);
	return query->queryPolygons(center, polyPickExt, &filter, polyRefs, polyCount, maxPolys);
}
