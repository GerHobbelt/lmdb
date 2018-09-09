/* mtest.c - memory-mapped database tester/toy */
/*
 * Copyright 2011-2018 Howard Chu, Symas Corp.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted only as authorized by the OpenLDAP
 * Public License.
 *
 * A copy of this license is available in the file LICENSE in the
 * top-level directory of the distribution or, alternatively, at
 * <http://www.OpenLDAP.org/license.html>.
 */
#include "lmdb.h"
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#define E(expr) CHECK((rc = (expr)) == MDB_SUCCESS, #expr)
#define RES(err, expr) ((rc = expr) == (err) || (CHECK(!rc, #expr), 0))
#define CHECK(test, msg)                                                       \
  ((test) ? (void)0                                                            \
          : ((void)fprintf(stderr, "%s:%d: %s: %s\n", __FILE__, __LINE__, msg, \
                           mdb_strerror(rc)),                                  \
             abort()))

int main(int argc, char *argv[]) {
  int i = 0, j = 0, rc;
  MDB_env *env;
  MDB_dbi dbi;
  MDB_val key, data;
  MDB_txn *txn;
  MDB_stat mst;
  MDB_cursor *cursor, *cur2;
  MDB_cursor_op op;
  int count = 20000;
  char sval[32] = "";
  char shortkey[8], longkey[32];

  E(mdb_env_create(&env));
  E(mdb_env_set_maxreaders(env, 1));
  E(mdb_env_set_mapsize(env, 10485760));
  E(mdb_env_open(env, "./testdb", 0 /*|MDB_NOSYNC*/, 0664));

  E(mdb_txn_begin(env, NULL, 0, &txn));
  E(mdb_dbi_open(txn, NULL, 0, &dbi));

  key.mv_size = sizeof(shortkey);
  key.mv_data = shortkey;

  printf("Adding %d short keys\n", count);
  sprintf(sval, "short");
  data.mv_size = sizeof(sval);
  data.mv_data = sval;
  for (i = 0; i < count; i++) {
    sprintf(shortkey, "%08d", i);
    /* Set <data> in each iteration, since MDB_NOOVERWRITE may modify it */
    if (RES(MDB_KEYEXIST, mdb_put(txn, dbi, &key, &data, MDB_NOOVERWRITE))) {
      j++;
      data.mv_size = sizeof(sval);
      data.mv_data = sval;
    }
  }
  if (j)
    printf("%d duplicates skipped\n", j);
  E(mdb_txn_commit(txn));
  E(mdb_env_stat(env, &mst));

  key.mv_size = sizeof(longkey);
  key.mv_data = longkey;

  printf("Adding %d long keys\n", count);
  E(mdb_txn_begin(env, NULL, 0, &txn));
  sprintf(sval, "long");
  data.mv_size = sizeof(sval);
  data.mv_data = sval;

  j = 0;
  for (i = 0; i < count; i++) {
    sprintf(longkey, "%08dlong key", i);
    /* Set <data> in each iteration, since MDB_NOOVERWRITE may modify it */
    if (RES(MDB_KEYEXIST, mdb_put(txn, dbi, &key, &data, MDB_NOOVERWRITE))) {
      j++;
      data.mv_size = sizeof(sval);
      data.mv_data = sval;
    }
  }
  if (j)
    printf("%d duplicates skipped\n", j);
  E(mdb_txn_commit(txn));
  E(mdb_env_stat(env, &mst));

  printf("Deleting %d short keys\n", count);
  key.mv_size = sizeof(shortkey);
  key.mv_data = shortkey;
  for (i = 0; i < count; i++) {
    txn = NULL;
    E(mdb_txn_begin(env, NULL, 0, &txn));
    sprintf(shortkey, "%08d ", i);
    if (RES(MDB_NOTFOUND, mdb_del(txn, dbi, &key, NULL))) {
      j--;
      mdb_txn_abort(txn);
    } else {
      E(mdb_txn_commit(txn));
    }
  }
  printf("Deleted %d values\n", i);

  E(mdb_env_stat(env, &mst));
  E(mdb_txn_begin(env, NULL, MDB_RDONLY, &txn));
  E(mdb_cursor_open(txn, dbi, &cursor));
  printf("Cursor next\n");
  while ((rc = mdb_cursor_get(cursor, &key, &data, MDB_NEXT)) == 0) {
    printf("key: %.*s, data: %.*s\n", (int)key.mv_size, (char *)key.mv_data,
           (int)data.mv_size, (char *)data.mv_data);
  }
  CHECK(rc == MDB_NOTFOUND, "mdb_cursor_get");
  printf("Cursor last\n");
  E(mdb_cursor_get(cursor, &key, &data, MDB_LAST));
  printf("key: %.*s, data: %.*s\n", (int)key.mv_size, (char *)key.mv_data,
         (int)data.mv_size, (char *)data.mv_data);
  printf("Cursor prev\n");
  while ((rc = mdb_cursor_get(cursor, &key, &data, MDB_PREV)) == 0) {
    printf("key: %.*s, data: %.*s\n", (int)key.mv_size, (char *)key.mv_data,
           (int)data.mv_size, (char *)data.mv_data);
  }
  CHECK(rc == MDB_NOTFOUND, "mdb_cursor_get");
  printf("Cursor last/prev\n");
  E(mdb_cursor_get(cursor, &key, &data, MDB_LAST));
  printf("key: %.*s, data: %.*s\n", (int)key.mv_size, (char *)key.mv_data,
         (int)data.mv_size, (char *)data.mv_data);
  E(mdb_cursor_get(cursor, &key, &data, MDB_PREV));
  printf("key: %.*s, data: %.*s\n", (int)key.mv_size, (char *)key.mv_data,
         (int)data.mv_size, (char *)data.mv_data);

  mdb_cursor_close(cursor);
  mdb_txn_abort(txn);

  mdb_dbi_close(env, dbi);
  mdb_env_close(env);

  return 0;
}
