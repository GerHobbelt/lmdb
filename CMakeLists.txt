cmake_minimum_required(VERSION 3.13)

# Parse version information
file(STRINGS 
    "${CMAKE_CURRENT_SOURCE_DIR}/libraries/liblmdb/lmdb.h" 
    LMDB_VERSION_MAJOR REGEX 
    "^#define MDB_VERSION_MAJOR[ \t]+([0-9]+)$")
string(REGEX REPLACE 
    "^#define MDB_VERSION_MAJOR[ \t]+([0-9]+)$" "\\1" 
    LMDB_VERSION_MAJOR 
    "${LMDB_VERSION_MAJOR}")
file(STRINGS 
    "${CMAKE_CURRENT_SOURCE_DIR}/libraries/liblmdb/lmdb.h" 
    LMDB_VERSION_MINOR REGEX 
    "^#define MDB_VERSION_MINOR[ \t]+([0-9]+)$")
string(REGEX REPLACE 
    "^#define MDB_VERSION_MINOR[ \t]+([0-9]+)$" "\\1" 
    LMDB_VERSION_MINOR 
    "${LMDB_VERSION_MINOR}")
file(STRINGS 
    "${CMAKE_CURRENT_SOURCE_DIR}/libraries/liblmdb/lmdb.h" 
    LMDB_VERSION_PATCH REGEX 
    "^#define MDB_VERSION_PATCH[ \t]+([0-9]+)$")
string(REGEX REPLACE 
    "^#define MDB_VERSION_PATCH[ \t]+([0-9]+)$" "\\1" 
    LMDB_VERSION_PATCH 
    "${LMDB_VERSION_PATCH}")
set(LMDB_VERSION_STRING "${LMDB_VERSION_MAJOR}.${LMDB_VERSION_MINOR}.${LMDB_VERSION_PATCH}")
mark_as_advanced(LMDB_VERSION_STRING LMDB_VERSION_MAJOR LMDB_VERSION_MINOR LMDB_VERSION_PATCH)

project(lmdb
    VERSION ${LMDB_VERSION_STRING}
    LANGUAGES C)

set(sources 
    libraries/liblmdb/mdb.c
    libraries/liblmdb/midl.c
)

add_library(lmdb
    STATIC ${sources}
)
target_include_directories(lmdb
    PUBLIC include-fwd
)
