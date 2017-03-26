#ifndef __SKIPD_H_
#define  __SKIPD_H_

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <ev.h>
#include <lmdb.h>
#include <sqlite3.h>

#define offsetof2(TYPE, MEMBER) ((size_t) &((TYPE *)0)->MEMBER)
#define container_of(ptr, type, member) ({                      \
        const typeof( ((type *)0)->member ) *__mptr = (const typeof( ((type *)0)->member )*)(ptr);    \
        (type *)( (char *)__mptr - offsetof2(type,member) );})

#define MAGIC "magicv1 "
#define MAGIC_LEN 8
#define HEADER_LEN 8
#define HEADER_PREFIX (MAGIC_LEN + HEADER_LEN)
#define SK_PATH_MAX 128
#define BUF_MAX 2048
#define READ_MAX 65536

#define DELAY_PREFIX "__delay__"
#define DELAY_PREFIX_LEN 9
#define DELAY_KEY_LEN 128

#define SKIPD_DEBUG 3

#define _min(a,b) \
   ({ __typeof__ (a) _a = (a); \
       __typeof__ (b) _b = (b); \
     _a > _b ? _b : _a; })

#define _max(a,b) \
   ({ __typeof__ (a) _a = (a); \
       __typeof__ (b) _b = (b); \
     _a > _b ? _a : _b; })

typedef enum {
    S2ISUCCESS = 0,
    S2IOVERFLOW,
    S2IUNDERFLOW,
    S2IINCONVERTIBLE
} STR2INT_ERROR;

static STR2INT_ERROR str2int(int *i, char *s, int base) {
  char *end;
  long  l;
  errno = 0;
  l = strtol(s, &end, base);

  if ((errno == ERANGE && l == LONG_MAX) || l > INT_MAX) {
    return S2IOVERFLOW;
  }
  if ((errno == ERANGE && l == LONG_MIN) || l < INT_MIN) {
    return S2IUNDERFLOW;
  }
  if (*s == '\0' || *end != '\0') {
    return S2IINCONVERTIBLE;
  }
  *i = (int)l;
  return S2ISUCCESS;
}

typedef struct _skipd_server {
    ev_io io;
    int fd;
    struct sockaddr_un socket;
    int socket_len;

    ev_timer watcher;

    MDB_env *env;
    MDB_dbi dbi;
    MDB_txn *wtx;       /* the current writing transaction */
    MDB_txn *cache_rtx; /* cache the last one of read transaction for optimising */
    int writing;

    // jffs2 not support mmap, so must use sqlite to save the commit log
    // then replay the commit log to LMDB
    sqlite3 *sqlite_db; 
    int dirty;

    int daemon;
    char db_path[SK_PATH_MAX];
    char sock_path[SK_PATH_MAX];
    char pid_path[SK_PATH_MAX];
} skipd_server;
#endif

