#include <stdio.h>
#include <stdlib.h>
#include "lmdb.h"

#define E(expr) CHECK((rc = (expr)) == MDB_SUCCESS, #expr)
#define CHECK(test, msg) ((test) ? (void)0 : ((void)fprintf(stderr, \
    "%s:%d: %s: %s\n", __FILE__, __LINE__, msg, mdb_strerror(rc)), abort()))

int main(int argc,char * argv[])
{
    int rc;
    MDB_env *env;
    MDB_dbi dbi;
    MDB_val key, data;
    MDB_txn *txn;
    char sval[] = "foo";
    char dval[] = "bar";

    E(mdb_env_create(&env));
    E(mdb_env_set_maxdbs(env, 2));
    E(mdb_env_open(env, "./testdb", 0, 0664));

    E(mdb_txn_begin(env, NULL, 0, &txn));
    E(mdb_dbi_open(txn, "subdb", MDB_CREATE, &dbi));
    E(mdb_txn_commit(txn));

    key.mv_size = 3;
    key.mv_data = sval;
    data.mv_size = 3;
    data.mv_data = dval;

    E(mdb_txn_begin(env, NULL, 0, &txn));
    E(mdb_put(txn, dbi, &key, &data, 0));
    E(mdb_txn_commit(txn));

    mdb_dbi_close(env, dbi);
    mdb_env_close(env);

    printf("ready");

    return 0;
}