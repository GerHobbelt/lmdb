project(liblmdb)

cmake_minimum_required(VERSION 2.8)

# macOS MACOSX_RPATH - @rpath as install name
cmake_policy(SET CMP0042 NEW)
set(CMAKE_INSTALL_RPATH_USE_LINK_PATH TRUE)

option(BUILD_TESTS "build unittests" OFF)
option(BUILD_TOOLS "build mdb tools" OFF)
option(BUILD_STATIC "build a static library" OFF)
option(BUILD_SHARED "build a shared library" ON)

if(BUILD_TESTS)
    enable_testing()
endif()

set(mdb_SRCS
    mdb.c
    midl.c
)

set(binarylist
    mdb_stat
    mdb_copy
    mdb_dump
    mdb_load
)
set(testlist
    mtest
    mtest2
    mtest3
    mtest4
    mtest5
)

if(MSVC)
    add_definitions(-wd4996)
endif()

if(WIN32)
    set(mdb_SRCS ${mdb_SRCS} liblmdb.def)
endif()

set(mdb_TARGETS)
if(BUILD_STATIC)
    add_library(lmdb-static ${mdb_SRCS})
    set(mdb_TARGETS ${mdb_TARGETS} lmdb-static)
endif()
if(BUILD_SHARED)
    add_library(lmdb SHARED ${mdb_SRCS})
    set(mdb_TARGETS ${mdb_TARGETS} lmdb)
endif()

if(BUILD_TOOLS)
    include_directories(${CMAKE_CURRENT_SOURCE_DIR})
    foreach(_binary ${binarylist})
        add_executable(${_binary} ${_binary}.c getopt.c)
        target_link_libraries(${_binary} mdb-static)
        set(mdb_TARGETS ${mdb_TARGETS} ${_binary})
    endforeach()
endif()

install(TARGETS ${mdb_TARGETS} RUNTIME DESTINATION bin
                               LIBRARY DESTINATION lib
                               ARCHIVE DESTINATION lib)

install(FILES lmdb.h DESTINATION include)
if(BUILD_TESTS)
    foreach(_test ${testlist})
        add_executable(${_test} ${_test}.c)
        target_link_libraries(${_test} lmdb-static)
        add_test(NAME ${_test}
        COMMAND "${CMAKE_COMMAND}" -DCMD=${_test} -P ${CMAKE_CURRENT_SOURCE_DIR}/runtests.cmake)
    endforeach()
endif()
