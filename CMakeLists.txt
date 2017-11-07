cmake_minimum_required(VERSION 3.0.0)

project(liblmdb VERSION 0.0.0)

set(LIBLMDB_SRCS
    "libraries/liblmdb/mdb.c"
    "libraries/liblmdb/midl.c"
)

set(LIBLMDB_INCLUDES
    "libraries/liblmdb/lmdb.h"
    "libraries/liblmdb/midl.h"
)

add_library(lmdb ${LIBLMDB_SRCS})

if (WIN32)
  # Suppress warnings to reduce build log size.
  target_compile_options(lmdb PRIVATE
    /wd4267 /wd4244 /wd4800 /wd4503 /wd4554 /wd4996 /wd4348 /wd4018
    /wd4099 /wd4146 /wd4267 /wd4305 /wd4307
    /wd4715 /wd4722 /wd4723 /wd4838 /wd4309 /wd4334
    /wd4003 /wd4244 /wd4267 /wd4503 /wd4506 /wd4800 /wd4996
    /wd8029)
endif()

target_include_directories(lmdb
  PRIVATE
  "$<BUILD_INTERFACE:${CMAKE_CURRENT_LIST_DIR}>"
)

### Install ###
set(generated_dir "${CMAKE_CURRENT_BINARY_DIR}/generated")

set(config_install_dir "lib/cmake/${PROJECT_NAME}")
set(include_install_dir "include")

set(version_config "${generated_dir}/${PROJECT_NAME}ConfigVersion.cmake")
set(project_config "${generated_dir}/${PROJECT_NAME}Config.cmake")
set(targets_export_name "${PROJECT_NAME}Targets")
set(namespace "${PROJECT_NAME}::")

include(CMakePackageConfigHelpers)
write_basic_package_version_file(
  "${version_config}" COMPATIBILITY SameMajorVersion
  )

# Note: use 'targets_export_name'
configure_package_config_file(
  "${PROJECT_SOURCE_DIR}/cmake/Config.cmake.in"
  "${project_config}"
  INSTALL_DESTINATION "${config_install_dir}"
  )

install(
  TARGETS lmdb
  EXPORT "${targets_export_name}"
  LIBRARY DESTINATION "lib"
  ARCHIVE DESTINATION "lib"
  RUNTIME DESTINATION "bin"
  INCLUDES DESTINATION "${include_install_dir}"
  )

install(
  FILES ${LIBLMDB_INCLUDES}
  DESTINATION "${include_install_dir}/lmdb/"
  )

install(
  FILES "${project_config}" "${version_config}"
  DESTINATION "${config_install_dir}"
  )

install(
  EXPORT "${targets_export_name}"
  NAMESPACE "${namespace}"
  DESTINATION "${config_install_dir}"
  )
