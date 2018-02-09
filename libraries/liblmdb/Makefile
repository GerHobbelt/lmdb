# Makefile for liblrzdb (Lightning memory-mapped database library).

########################################################################
# Configuration. The compiler options must enable threaded compilation.
#
# Preprocessor macros (for CPPFLAGS) of interest...
# Note that the defaults should already be correct for most
# platforms; you should not need to change any of these.
# Read their descriptions in rzdb.c if you do:
#
# - MDB_USE_POSIX_MUTEX, MDB_USE_POSIX_SEM, MDB_USE_SYSV_SEM
# - MDB_DSYNC
# - MDB_FDATASYNC
# - MDB_FDATASYNC_WORKS
# - MDB_USE_PWRITEV
# - MDB_USE_ROBUST
#
# There may be other macros in rzdb.c of interest. You should
# read rzdb.c before changing any of them.
#
CC	= gcc
AR	= ar
W	= -W -Wall -Wno-unused-parameter -Wbad-function-cast -Wuninitialized
THREADS = -pthread
OPT = -O2 -g
CFLAGS	= $(THREADS) $(OPT) $(W) $(XCFLAGS)
LDLIBS	= 
SOLIBS	= 
SOEXT	= .so
prefix	= /usr/local
exec_prefix = $(prefix)
bindir = $(exec_prefix)/bin
libdir = $(exec_prefix)/lib
includedir = $(prefix)/include
datarootdir = $(prefix)/share
mandir = $(datarootdir)/man

########################################################################

LIBNAME = librzdb
IHDRS	= rzdb.h
ILIBS	= $(LIBNAME).a $(LIBNAME)$(SOEXT)
IPROGS	= rzdb_stat rzdb_copy rzdb_dump rzdb_load rzdb_drop
IDOCS	= rzdb_stat.1 rzdb_copy.1 rzdb_dump.1 rzdb_load.1
PROGS	= $(IPROGS) mtest mtest2 mtest3 mtest4 mtest5
all:	$(ILIBS) $(PROGS)

install: $(ILIBS) $(IPROGS) $(IHDRS)
	mkdir -p $(DESTDIR)$(bindir)
	mkdir -p $(DESTDIR)$(libdir)
	mkdir -p $(DESTDIR)$(includedir)
	mkdir -p $(DESTDIR)$(mandir)/man1
	for f in $(IPROGS); do cp $$f $(DESTDIR)$(bindir); done
	for f in $(ILIBS); do cp $$f $(DESTDIR)$(libdir); done
	for f in $(IHDRS); do cp $$f $(DESTDIR)$(includedir); done
	for f in $(IDOCS); do cp $$f $(DESTDIR)$(mandir)/man1; done

clean:
	rm -rf $(PROGS) *.[ao] *.[ls]o *~ testdb

test:	all
	rm -rf testdb && mkdir testdb
	./mtest && ./rzdb_stat testdb

$(LIBNAME).a:	rzdb.o midl.o
	$(AR) rs $@ rzdb.o midl.o

$(LIBNAME)$(SOEXT):	rzdb.lo midl.lo
#	$(CC) $(LDFLAGS) -pthread -shared -Wl,-Bsymbolic -o $@ rzdb.o midl.o $(SOLIBS)
	$(CC) $(LDFLAGS) -pthread -shared -o $@ rzdb.lo midl.lo $(SOLIBS)

rzdb_stat: rzdb_stat.o $(LIBNAME).a
rzdb_copy: rzdb_copy.o $(LIBNAME).a
rzdb_dump: rzdb_dump.o $(LIBNAME).a
rzdb_load: rzdb_load.o $(LIBNAME).a
mtest:    mtest.o    $(LIBNAME).a
mdb_drop: mdb_drop.o $(LIBNAME).a
mtest2:	mtest2.o $(LIBNAME).a
mtest3:	mtest3.o $(LIBNAME).a
mtest4:	mtest4.o $(LIBNAME).a
mtest5:	mtest5.o $(LIBNAME).a
mtest6:	mtest6.o $(LIBNAME).a

rzdb.o: rzdb.c rzdb.h midl.h
	$(CC) $(CFLAGS) $(CPPFLAGS) -c rzdb.c

midl.o: midl.c midl.h
	$(CC) $(CFLAGS) $(CPPFLAGS) -c midl.c

rzdb.lo: rzdb.c rzdb.h midl.h
	$(CC) $(CFLAGS) -fPIC $(CPPFLAGS) -c rzdb.c -o $@

midl.lo: midl.c midl.h
	$(CC) $(CFLAGS) -fPIC $(CPPFLAGS) -c midl.c -o $@

%:	%.o
	$(CC) $(CFLAGS) $(LDFLAGS) $^ $(LDLIBS) -o $@

%.o:	%.c rzdb.h
	$(CC) $(CFLAGS) $(CPPFLAGS) -c $<

COV_FLAGS=-fprofile-arcs -ftest-coverage
COV_OBJS=xrzdb.o xmidl.o

coverage: xmtest
	for i in mtest*.c [0-9]*.c; do j=`basename \$$i .c`; $(MAKE) $$j.o; \
		gcc -o x$$j $$j.o $(COV_OBJS) -pthread $(COV_FLAGS); \
		rm -rf testdb; mkdir testdb; ./x$$j; done
	gcov xrzdb.c
	gcov xmidl.c

xmtest:	mtest.o xrzdb.o xmidl.o
	gcc -o xmtest mtest.o xrzdb.o xmidl.o -pthread $(COV_FLAGS)

xrzdb.o: rzdb.c rzdb.h midl.h
	$(CC) $(CFLAGS) -fPIC $(CPPFLAGS) -O0 $(COV_FLAGS) -c rzdb.c -o $@

xmidl.o: midl.c midl.h
	$(CC) $(CFLAGS) -fPIC $(CPPFLAGS) -O0 $(COV_FLAGS) -c midl.c -o $@
