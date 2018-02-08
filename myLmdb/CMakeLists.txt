cmake_minimum_required(VERSION 3.5)
project(imgDB)

set(PROJECT_DIR ${PROJECT_SOURCE_DIR})
set(LIBRARY_OUTPUT_PATH ${PROJECT_DIR}/lib/)
set(EXECUTABLE_OUTPUT_PATH ${PROJECT_DIR}/bin/)
# This is for CLion
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${LIBRARY_OUTPUT_PATH})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${LIBRARY_OUTPUT_PATH})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${EXECUTABLE_OUTPUT_PATH})

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -g -O0 -fPIC")

set(INCLUDE_LIST ${PROJECT_DIR}/lmdb/)
include_directories(${INCLUDE_LIST})

set(SOURCE_FILES main.cpp ${PROJECT_DIR}/lmdb/mdb.c ${PROJECT_DIR}/lmdb/midl.c)
add_executable(imgDB ${SOURCE_FILES})

set(SYS_LIBS opencv_contrib opencv_core opencv_highgui opencv_imgproc opencv_ml opencv_objdetect pthread)
target_link_libraries(imgDB ${SYS_LIBS})