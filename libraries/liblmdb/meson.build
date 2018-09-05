project(
  'lmdb', 'c',
  version: '0.9.22',
  default_options: [
    'warning_level=2'
  ],
  license: 'OpenLDAP',
  meson_version: '>= 0.46.0'
)

lmdb_name = meson.project_name()

lmdb_version = meson.project_version()
message('Building lmdb ' + lmdb_version)

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

lmdb_inc = include_directories('.')
lmdb = both_libraries(
  'lmdb',
  [ lmdb_public_headers, lmdb_headers, lmdb_sources ],
  include_directories: lmdb_inc,
  dependencies: thread_dep,
  install: true,
  c_args: common_cflags,
  vs_module_defs: 'lmdb.def',
)

lmdb_dep = declare_dependency(
  link_with: lmdb,
  include_directories: lmdb_inc,
)

programs = [
  'mdb_stat',
  'mdb_copy',
  'mdb_dump',
  'mdb_load',
  'mdb_drop',
]

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
    c_args: common_cflags,
  )
  test(test_program, exe, args: [ '--tap', '-k' ])
endforeach

man_files = files(
  'mdb_stat.1',
  'mdb_copy.1',
  'mdb_dump.1',
  'mdb_load.1',
  'mdb_drop.1',
)

install_man(man_files)

# Generate pc file
pkgg = import('pkgconfig')

pkgg.generate(libraries: [ lmdb ],
              name: lmdb_name,
              description: 'Lightning Memory-Mapped Database.')
