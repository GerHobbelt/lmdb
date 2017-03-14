#include <time.h>
#include <stdlib.h>
#include <stdio.h>
#include <lmdb.h>

#define E(expr) CHECK((rc = (expr)) == MDB_SUCCESS, #expr)
#define RES(err, expr) ((rc = expr) == (err) || (CHECK(!rc, #expr), 0))
#define CHECK(test, msg) ((test) ? (void)0 : ((void)fprintf(stderr, \
	"%s:%d: %s: %s\n", __FILE__, __LINE__, msg, mdb_strerror(rc)), abort()))

int main(void)
{
    int rc;
    MDB_env *env = NULL;
    MDB_dbi dbi = {0};
    MDB_val key = {0}, data = {0};
    MDB_txn *txn = NULL;
    MDB_stat mst = {0};
    MDB_cursor *cursor, *cur2;
    MDB_cursor_op op;
    char skey[32] = "hela";
    char sval[32] = "value3";

    E(mdb_env_create(&env));
    E(mdb_env_set_maxreaders(env, 1));
    E(mdb_env_set_mapsize(env, 2097152));
    E(mdb_env_open(env, "./testdb", MDB_FIXEDMAP|MDB_NOMETASYNC, 0664));
    E(mdb_txn_begin(env, NULL, 0, &txn));
    E(mdb_dbi_open(txn, NULL, 0, &dbi));

    key.mv_size = strlen(skey);
    key.mv_data = skey;

    data.mv_size = sizeof(sval);
    data.mv_data = sval;
    if (RES(MDB_KEYEXIST, mdb_put(txn, dbi, &key, &data, MDB_NOOVERWRITE))) {
        data.mv_size = sizeof(sval);
        data.mv_data = sval;
    }
    E(mdb_txn_commit(txn));

    E(mdb_txn_begin(env, NULL, MDB_RDONLY, &txn));
    E(mdb_cursor_open(txn, dbi, &cursor));
    while ((rc = mdb_cursor_get(cursor, &key, &data, MDB_NEXT)) == 0) {
            printf("key: %p %.*s, data: %p %.*s\n",
                    key.mv_data,  (int) key.mv_size,  (char *) key.mv_data,
                    data.mv_data, (int) data.mv_size, (char *) data.mv_data);
    }
    CHECK(rc == MDB_NOTFOUND, "mdb_cursor_get");
    mdb_cursor_close(cursor);
    mdb_txn_abort(txn);
    printf("\n");

    key.mv_size = strlen(skey)-2;
    key.mv_data = skey;
    E(mdb_txn_begin(env, NULL, MDB_RDONLY, &txn));
    E(mdb_cursor_open(txn, dbi, &cursor));
    if (RES(0, mdb_cursor_get(cursor, &key, &data, MDB_SET_RANGE))) {
        while ((rc = mdb_cursor_get(cursor, &key, &data, MDB_NEXT)) == 0) {
                printf("key: %p %.*s, data: %p %.*s\n",
                        key.mv_data,  (int) key.mv_size,  (char *) key.mv_data,
                        data.mv_data, (int) data.mv_size, (char *) data.mv_data);
        }
    }

    mdb_dbi_close(env, dbi);
    mdb_env_close(env);

    printf("test ok\n");
    return 0;
}
