cmake_minimum_required(VERSION 2.8)

add_library(liblmdb STATIC mdb.c lmdb.h midl.c midl.h)
set_target_properties(liblmdb PROPERTIES DEBUG_POSTFIX d)

install(TARGET liblmdb DESTINATION lib)

install(FILES lmdb.h midl.h DESTINATION include)

