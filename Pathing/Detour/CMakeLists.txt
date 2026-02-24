cmake_minimum_required(VERSION 3.0...4.1)
project(Detour)

file(GLOB SOURCES Source/*.cpp)
file(GLOB HEADERS Include/*.h Include/*.hpp)

source_group("Source Files" FILES ${SOURCES})
source_group("Header Files" FILES ${HEADERS})

source_group("Detour\\Source" REGULAR_EXPRESSION "Source/Detour.*\\.cpp")
source_group("Detour\\Include" REGULAR_EXPRESSION "Include/Detour.*\\.(h|hpp)")

add_library(Detour SHARED ${SOURCES} ${HEADERS})

add_library(RecastNavigation::Detour ALIAS Detour)
set_target_properties(Detour PROPERTIES DEBUG_POSTFIX -d)

set(Detour_INCLUDE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/Include")

option(RECASTNAVIGATION_DT_POLYREF64 "Use 64-bit polyrefs" ON)
if(RECASTNAVIGATION_DT_POLYREF64)
    target_compile_definitions(Detour PUBLIC DT_POLYREF64)
endif()

if(RECASTNAVIGATION_DT_VIRTUAL_QUERYFILTER)
    target_compile_definitions(Detour PUBLIC DT_VIRTUAL_QUERYFILTER)
endif()

target_include_directories(Detour PUBLIC
    "$<BUILD_INTERFACE:${Detour_INCLUDE_DIR}>"
)

set_target_properties(Detour PROPERTIES
        SOVERSION ${SOVERSION}
        VERSION ${LIB_VERSION}
        COMPILE_PDB_OUTPUT_DIRECTORY .
        COMPILE_PDB_NAME "Detour-d"
        )

install(TARGETS Detour
        EXPORT recastnavigation-targets
        RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
        ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBDIR}
        LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR} COMPONENT library
        INCLUDES DESTINATION ${CMAKE_INSTALL_INCLUDEDIR} ${CMAKE_INSTALL_INCLUDEDIR}/recastnavigation
        )

install(FILES ${HEADERS} DESTINATION
    ${CMAKE_INSTALL_INCLUDEDIR}/recastnavigation)

if(MSVC)
    install(FILES "$<TARGET_FILE_DIR:Detour>/Detour-d.pdb" CONFIGURATIONS "Debug" DESTINATION "lib" OPTIONAL)
endif()