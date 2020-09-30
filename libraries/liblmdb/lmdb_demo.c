/* demo.c - memory-mapped database api */
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
#include "lmdb_method.h"

int main(int argc,char * argv[])
{
    char *key1 = "key1";
    char *value1 = "value1";

    char *key2 = "key2";
    char *value2 = "value2";

    char *key3 = "key3";
    char *value3 = "value3";

    char *key = NULL;
    char *value = NULL;

    char *v;

    key = malloc(32);
    value = malloc(32);

    memcpy(key, key1, strlen(key1));
    memcpy(value, value1, strlen(value1));
    LMDB_Put(key, value);

    memcpy(key, key2, strlen(key2));
    memcpy(value, value2, strlen(value2));
    LMDB_Put(key, value);

    memcpy(key, key3, strlen(key3));
    memcpy(value, value3, strlen(value3));
    LMDB_Put(key, value);

    memset(key, '\0', strlen(key));
    memcpy(key, key1, strlen(key1));
    LMDB_Get(key, &v);
    printf("value of key1 %s\n", v);
    free(v);

    memset(key, '\0', strlen(key));
    memcpy(key, key2, strlen(key2));
    LMDB_Get(key, &v);
    printf("value of key2 %s\n", v);
    free(v);

    memset(key, '\0', strlen(key));
    memcpy(key, key3, strlen(key3));
    LMDB_Get(key, &v);
    printf("value of key3 %s\n", v);
    free(v);
}
