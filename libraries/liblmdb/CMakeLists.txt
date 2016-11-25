cmake_minimum_required(VERSION 2.8)

file(READ "${CMAKE_CURRENT_SOURCE_DIR}/lmdb.h" VERSION_HEADER)
string(REGEX MATCH ".*MDB_VERSION_MAJOR[ \t]+([0-9])" MAJOR_VERSION_MATCH "${VERSION_HEADER}")
set(LMDB_MAJOR_VERSION ${CMAKE_MATCH_1})
string(REGEX MATCH ".*MDB_VERSION_MINOR[ \t]+([0-9])" MINOR_VERSION_MATCH "${VERSION_HEADER}")
set(LMDB_MINOR_VERSION ${CMAKE_MATCH_1})
string(REGEX MATCH ".*MDB_VERSION_PATCH[ \t]+([0-9]+)" PATCH_VERSION_MATCH "${VERSION_HEADER}")
set(LMDB_PATCH_VERSION ${CMAKE_MATCH_1})

set(LMDB_VERSION "${LMDB_MAJOR_VERSION}.${LMDB_MINOR_VERSION}.${LMDB_PATCH_VERSION}")

project(lmdb)

add_library(lmdb STATIC mdb.c lmdb.h midl.c midl.h)
set_target_properties(lmdb PROPERTIES DEBUG_POSTFIX d)
target_link_libraries(lmdb PUBLIC ntdll.lib)

install(TARGETS lmdb DESTINATION lib
        EXPORT lmdb-targets
        DESTINATION lib)

install(FILES lmdb.h midl.h DESTINATION include)

include(CMakePackageConfigHelpers)

set(INSTALL_INCLUDE_DIR include)
configure_package_config_file(${CMAKE_CURRENT_SOURCE_DIR}/cmake/package-config.cmakein ${CMAKE_CURRENT_BINARY_DIR}/lmdb-config.cmake
                              INSTALL_DESTINATION CMake
                              PATH_VARS INSTALL_INCLUDE_DIR
                              )
       
write_basic_package_version_file( ${CMAKE_CURRENT_BINARY_DIR}/lmdb-config-version.cmake VERSION ${LMDB_VERSION} COMPATIBILITY SameMajorVersion )

install(FILES ${CMAKE_CURRENT_BINARY_DIR}/lmdb-config.cmake ${CMAKE_CURRENT_BINARY_DIR}/lmdb-config-version.cmake
        DESTINATION CMake )


install(EXPORT lmdb-targets DESTINATION CMake)


