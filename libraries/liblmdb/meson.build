project(
  'liblmdb', 'c',
  version: '0.9.28',
  default_options: [
    'warning_level=2'
  ],
  license: 'OpenLDAP',
  meson_version: '>= 0.46.0'
)

liblmdb_name = meson.project_name()
liblmdb_version = meson.project_version()
liblmdb_soversion = 0
liblmdb_libversion = '0.0.0'

message('Building ' + liblmdb_name + ' ' + liblmdb_version)

cc = meson.get_compiler('c')
host_system = host_machine.system()
arch = host_machine.cpu_family()

# Compiler flags
if cc.get_id() == 'msvc'
  test_cflags = []
elif cc.get_id() == 'gcc' or cc.get_id() == 'clang'
  test_cflags = [
    '-Wno-unused-parameter',
    '-Wbad-function-cast',
    '-Wuninitialized',
  ]
else
  test_cflags = []
endif

common_cflags = []
foreach cflag: test_cflags
  if cc.has_argument(cflag)
    common_cflags += [ cflag ]
  endif
endforeach

extra_args = []
if cc.get_id() == 'msvc'
  extra_args += '-D_CRT_SECURE_NO_WARNINGS'
endif

thread_dep = dependency('threads')

lmdb_public_headers = files(
  'lmdb.h',
)

lmdb_headers = files(
  'midl.h'
)

lmdb_sources = files(
  'mdb.c',
  'midl.c',
)

install_headers(lmdb_public_headers)

lmdb_name_suffix = []
if cc.get_id() == 'msvc'
  lmdb_name_suffix = 'lib'
endif

lmdb_inc = include_directories('.')
lmdb_static = static_library(
  'lmdb',
  [ lmdb_public_headers, lmdb_headers, lmdb_sources ],
  include_directories: lmdb_inc,
  dependencies: thread_dep,
  install: true,
  c_args: common_cflags,
  name_suffix: lmdb_name_suffix,
)

lmdb = shared_library(
  'lmdb',
  objects: lmdb_static.extract_all_objects(),
  include_directories: lmdb_inc,
  dependencies: thread_dep,
  version: liblmdb_libversion,
  soversion: liblmdb_soversion,
  install: true,
  c_args: common_cflags,
  vs_module_defs: 'lmdb.def',
)

lmdb_dep = declare_dependency(
  link_with: lmdb,
  include_directories: lmdb_inc,
)

programs = []
if host_system != 'windows'
  programs += [
    'mdb_stat',
    'mdb_copy',
    'mdb_dump',
    'mdb_load',
  ]
endif

foreach program: programs
  executable(
    program, program + '.c',
    dependencies: lmdb_dep,
    include_directories: lmdb_inc,
    install: true,
    c_args: common_cflags,
  )
endforeach

test_programs = [
  'mtest',
  'mtest2',
  'mtest3',
  'mtest4',
  'mtest5',
]

foreach test_program: test_programs
  exe = executable(
    test_program, test_program + '.c',
    dependencies: lmdb_dep,
    include_directories: lmdb_inc,
    c_args: common_cflags + extra_args,
  )
  test(test_program, exe, args: [ '--tap', '-k' ])
endforeach

if host_system != 'windows'
man_files = files(
  'mdb_stat.1',
  'mdb_copy.1',
  'mdb_dump.1',
  'mdb_load.1',
)

install_man(man_files)
endif

# Generate pc file
pkg = import('pkgconfig')

pkg.generate(libraries: [ lmdb ],
             name: liblmdb_name,
             filebase: 'lmdb',
             description: 'Lightning Memory-Mapped Database.')
