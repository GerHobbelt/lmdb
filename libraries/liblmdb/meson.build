project('lmdb', 'c',
  version: '0.9.70',
  meson_version: '>= 0.51.0',
  default_options: [
    'c_std=c99',
    'warning_level=2',
    'buildtype=debugoptimized',
    'default_library=both',
  ],
)

cc = meson.get_compiler('c')

add_project_arguments([
  '-D_XOPEN_SOURCE=500',
], language: 'c')

cflags = cc.get_supported_arguments([
  '-Wno-unused-parameter',
  '-Wbad-function-cast',
  '-Wuninitialized',
  '-Wno-implicit-fallthrough',
])

link_flags = cc.get_supported_link_arguments([
  '-Bsymbolic',
])

threads_dep = dependency('threads')

lmdb_sources = files([
  'mdb.c',
  'midl.c',
])

install_headers(['lmdb.h'])

lmdb_lib = library('lmdb',
  lmdb_sources,
  link_args: link_flags,
  c_args: cflags,
  dependencies: threads_dep,
  install: true,
)

lmdb_dep = declare_dependency(
  link_with: lmdb_lib,
  dependencies: threads_dep,
)

bin_tools = [
  'mdb_stat',
  'mdb_copy',
  'mdb_dump',
  'mdb_load',
  'mdb_drop',
]

foreach tool: bin_tools
  executable(tool, tool + '.c', dependencies: lmdb_dep, c_args: cflags, install: true)
  install_man(tool + '.1', install_dir: get_option('mandir') / 'man1')
endforeach

tests = [
  'mtest',
  'mtest2',
  'mtest3',
  'mtest4',
  'mtest5',
]

#foreach mtest: tests
#  test(mtest, executable(mtest, mtest + '.c', dependencies: lmdb_dep, c_args: cflags))
#endforeach
