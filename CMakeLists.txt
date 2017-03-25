PROJECT(skipdbv2)
CMAKE_MINIMUM_REQUIRED (VERSION 2.8)
ADD_DEFINITIONS(-Os -g -Wstrict-prototypes -Wall)
#ADD_DEFINITIONS(-O0 -g -Wstrict-prototypes -Wall  -falign-loops=16 -DBUILDING_SKIPDB_DLL)

SET(EXECUTABLE_OUTPUT_PATH ${PROJECT_BINARY_DIR}/bin)
SET(LIBRARY_OUTPUT_PATH ${PROJECT_BINARY_DIR}/lib)
set(LIBEV_LIBRARIES CACHE PATH "Path to the libev library")
set(LIBEV_INCLUDE_DIRS CACHE PATH "Path to the libev include directory")

#INCLUDE_DIRECTORIES(${PROJECT_SOURCE_DIR}/src)

if ("${LIBEV_INCLUDE_DIRS}" STREQUAL "")
set(LIBEV_LIBRARIES ev)
endif()

INCLUDE_DIRECTORIES(${LIBEV_INCLUDE_DIRS} ${PROJECT_SOURCE_DIR}/libraries/liblmdb)
FILE(GLOB_RECURSE LMDB_SRC libraries/liblmdb/mdb.c libraries/liblmdb/midl.c)

#set(CMAKE_LIBRARY_PATH ${CMAKE_LIBRARY_PATH} /opt/brcm-arm/usr/lib)

ADD_LIBRARY(lmdb STATIC ${LMDB_SRC})

#INCLUDE_DIRECTORIES(${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mdb_stat libraries/liblmdb/mdb_stat.c)
TARGET_LINK_LIBRARIES(mdb_stat lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mdb_copy libraries/liblmdb/mdb_copy.c)
TARGET_LINK_LIBRARIES(mdb_copy lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mdb_dump libraries/liblmdb/mdb_dump.c)
TARGET_LINK_LIBRARIES(mdb_dump lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mdb_load libraries/liblmdb/mdb_load.c)
TARGET_LINK_LIBRARIES(mdb_load lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mtest libraries/liblmdb/mtest.c)
TARGET_LINK_LIBRARIES(mtest lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mtest2 libraries/liblmdb/mtest2.c)
TARGET_LINK_LIBRARIES(mtest2 lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mtest3 libraries/liblmdb/mtest3.c)
TARGET_LINK_LIBRARIES(mtest3 lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mtest4 libraries/liblmdb/mtest4.c)
TARGET_LINK_LIBRARIES(mtest4 lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mtest5 libraries/liblmdb/mtest5.c)
TARGET_LINK_LIBRARIES(mtest5 lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(mtest6 ${LMDB_SRC} libraries/liblmdb/mtest6.c)
TARGET_LINK_LIBRARIES(mtest6 pthread m ${LIBEV_LIBRARIES})
SET_TARGET_PROPERTIES(mtest6  PROPERTIES COMPILE_FLAGS " -DMDB_DEBUG=1 ")

ADD_EXECUTABLE(test ./tests/test.c)
TARGET_LINK_LIBRARIES(test lmdb pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(skipd ${LMDB_SRC} mgr/skipd.c mgr/daemonize.c)
TARGET_LINK_LIBRARIES(skipd pthread m ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(dbus mgr/dbus.c)
TARGET_LINK_LIBRARIES(dbus ${LIBEV_LIBRARIES})

ADD_EXECUTABLE(dbtest mgr/dbapi.c)
TARGET_LINK_LIBRARIES(dbapi)

