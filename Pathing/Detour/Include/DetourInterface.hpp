#pragma once

#include "DetourCommon.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"

#ifdef _WIN32
#	define DLLEXPORT extern "C" __declspec(dllexport)
#else
#	define DLLEXPORT extern "C"
#endif

constexpr auto MAX_POLY = 256;
constexpr auto MAX_NODES = 4096;

enum dtPolyFlags : unsigned short
{
	WALK = 0x01,        // Ability to walk (ground, grass, road)
	SWIM = 0x02,        // Ability to swim (water).
	DOOR = 0x04,        // Ability to move through doors.
	JUMP = 0x08,        // Ability to jump.
	DISABLED = 0x10,    // Disabled polygon
	ALL = 0xffff        // All abilities.
};

DLLEXPORT bool LoadNavMesh(char const* file, dtNavMesh** const mesh);
DLLEXPORT bool FreeNavMesh(dtNavMesh* meshPtr);

DLLEXPORT bool CreateNavMeshQuery(dtNavMesh* mesh, dtNavMeshQuery** const query);
DLLEXPORT bool FreeNavMeshQuery(dtNavMeshQuery* query);

DLLEXPORT dtStatus PathStraight(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], dtStraightPathOptions pathOptions, int* pointCount, float* pointBuffer, dtPolyFlags* pointFlags);
DLLEXPORT dtStatus MoveAlongSurface(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector);
DLLEXPORT dtStatus FindRandomPointAroundCircle(dtNavMeshQuery* query, float center[], float radius, float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector);
DLLEXPORT dtStatus FindClosestPoint(dtNavMeshQuery* query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], float* outputVector);
DLLEXPORT dtStatus FindClosestPointInBox(dtNavMeshQuery* query, float boxCenter[], float boxExtents[], float referencePos[], dtPolyFlags queryFilter[], float* outputVector);
DLLEXPORT dtStatus HasLineOfSight(dtNavMeshQuery* query, float start[], float end[], float polyPickExt[], dtPolyFlags queryFilter[], bool* hasLos, float* outputVector);
DLLEXPORT dtStatus UpdateDoorFlags(dtNavMesh* navMesh, dtPolyRef polyRefs[], int polyCount, unsigned short flagsToRemove, unsigned short flagsToAdd);
DLLEXPORT dtStatus GetPolyAt(dtNavMeshQuery *query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], dtPolyRef polyRef[], float *point);
DLLEXPORT dtStatus GetPolysInBox(dtNavMeshQuery *query, float center[], float polyPickExt[], dtPolyFlags queryFilter[], dtPolyRef polyRefs[], int *polyCount, int maxPolys);
