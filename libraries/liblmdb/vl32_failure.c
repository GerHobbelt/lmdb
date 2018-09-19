#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <time.h>
#include "lmdb.h"
#include <assert.h>
#include <unistd.h>

#define E(expr) CHECK((rc = (expr)) == MDB_SUCCESS, #expr)
#define RES(err, expr) ((rc = expr) == (err) || (CHECK(!rc, #expr), 0))
#define CHECK(test, msg) ((test) ? (void)0 : ((void)fprintf(stderr, \
	"%s:%d: %s: %s\n", __FILE__, __LINE__, msg, mdb_strerror(rc)), abort()))

void write_stuff(MDB_env *env)
{
  int rc;
  MDB_dbi dbi;
  MDB_val key, data;
  MDB_txn *txn;
  char sval[] = "test";

  E(mdb_txn_begin(env, NULL, 0, &txn));
  E(mdb_dbi_open(txn, NULL, 0, &dbi));

  key.mv_size = sizeof(sval) - 1;
  key.mv_data = sval;
  data.mv_size = sizeof(sval) - 1;
  data.mv_data = sval;
  E(mdb_put(txn, dbi, &key, &data, 0));

  E(mdb_txn_commit(txn));
  mdb_dbi_close(env, dbi);

  return;
}

void read_stuff(MDB_env *env)
{
  int rc;
  MDB_dbi dbi;
  MDB_val key, data;
  MDB_txn *txn;
  char sval[] = "test";

  E(mdb_txn_begin(env, NULL, MDB_RDONLY, &txn));
  E(mdb_dbi_open(txn, NULL, 0, &dbi));

  key.mv_size = sizeof(sval) - 1;
  key.mv_data = sval;
  E(mdb_get(txn, dbi, &key, &data));
  assert(!strncmp(data.mv_data, sval, sizeof(sval) - 1));
  E(mdb_txn_commit(txn));
  mdb_dbi_close(env, dbi);

  return;
}

int main(int argc,char * argv[])
{
        int rc;
	MDB_env *env, *env_read;

        unlink("/tmp/data.mdb");
        unlink("/tmp/lock.mdb");

        E(mdb_env_create(&env));
        E(mdb_env_open(env, "/tmp", 0, 0644));

        E(mdb_env_create(&env_read));
        E(mdb_env_open(env_read, "/tmp", MDB_RDONLY | MDB_NOTLS, 0644));

        write_stuff(env);
        read_stuff(env_read);

        write_stuff(env);
        read_stuff(env_read);

        mdb_env_close(env);
        mdb_env_close(env_read);

        printf("OK\n");
	return 0;
}
