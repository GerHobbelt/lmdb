/* lmdb_method.c - memory-mapped database api */
/*
 * Copyright 2011-2020 Howard Chu, Symas Corp.
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
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include "lmdb.h"

int rc = 0;

#define E(expr) CHECK((rc = (expr)) == MDB_SUCCESS, #expr)
#define RES(err, expr) ((rc = expr) == (err) || (CHECK(!rc, #expr), 0))
#define CHECK(test, msg) ((test) ? (void)0 : ((void)fprintf(stderr, \
       "%s:%d: %s: %s\n", __FILE__, __LINE__, msg, mdb_strerror(rc))))

#define DMSDBDIR   "./DMSOS_DBDir"

int LMDB_Put(const char *key, const char *value);
int LMDB_Get(const char *key, char **value);
int LMDB_Del(const char *key);

MDB_env *env;
MDB_dbi dbi;
MDB_val db_key;
MDB_val db_value;
MDB_txn *txn;

static int LMDB_Init()
{
    E(mdb_env_create(&env));
    E(mdb_env_set_maxreaders(env, 1));
    E(mdb_env_set_mapsize(env, 10485760));
    E(mdb_env_open(env, DMSDBDIR, MDB_FIXEDMAP, 0664));

    E(mdb_txn_begin(env, NULL, 0, &txn));
    E(mdb_dbi_open(txn, NULL, 0, &dbi));
}

static int LMDB_Done()
{

    mdb_dbi_close(env, dbi);
    mdb_env_close(env);
}

int LMDB_Put(const char *key, const char *value)
{
    LMDB_Init();
    printf("LMDB_Put key: %s, value %s\n", key, value);

    db_key.mv_data = key;
    db_key.mv_size = strlen(key);
    db_value.mv_data = value;
    db_value.mv_size = strlen(value);

    if (RES(MDB_KEYEXIST, mdb_put(txn, dbi, &db_key, &db_value, MDB_NOOVERWRITE))) {
        printf("key %s already exist\n", key);
        mdb_txn_abort(txn);
        goto abort;
    }

    mdb_txn_commit(txn);

abort:
    LMDB_Done();
    return rc;
}

int LMDB_Get(const char *key, char **value)
{
    LMDB_Init();

    printf("LMDB_Get key: %s\n", key);

    db_key.mv_data = key;
    db_key.mv_size = strlen(key);

    E(mdb_get(txn, dbi, &db_key, &db_value));
    if (rc)
        goto abort;

    *value = malloc(db_value.mv_size);
    memcpy(*value, db_value.mv_data, db_value.mv_size);

abort:
    LMDB_Done();
    return rc;
}

int LMDB_Del(const char *key)
{
    LMDB_Init();

    printf("LMDB_Del key: %s\n", key);

    db_key.mv_data = key;
    db_key.mv_size = strlen(key);

    if (RES(MDB_NOTFOUND, mdb_del(txn, dbi, &db_key, NULL))) {
        printf("key %s not found\n", key);
        mdb_txn_abort(txn);
        goto abort;
    }

    mdb_txn_commit(txn);

abort:
    LMDB_Done();
    return rc;
}
