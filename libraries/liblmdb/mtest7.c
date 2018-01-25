/*
 * Copyright 2011-2017 Howard Chu, Symas Corp.
 * Copyright 2017 Lorenz Bauer, Cloudflare, Ltd.
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
/*
 * Tests conditions under which SIGPIPE can be generated, and verifies we return EPIPE.
 */
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <pthread.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <fcntl.h>
#include "lmdb.h"

#define E(expr) CHECK((rc = (expr)) == MDB_SUCCESS, #expr)
#define E_ERRNO(expr) CHECK((expr) != -1 || (rc = errno) != 0, #expr)
#define CHECK(test, msg) ((test) ? (void)0 : ((void)fprintf(stderr, \
	"%s:%d: %s: %s\n", __FILE__, __LINE__, msg, mdb_strerror(rc)), abort()))

/* Has to be larger than socketpair send / receive buffer */
#define VALSIZE (10*1024*1024)

void *threadfn(void *arg)
{
	char buf[4096];
	int fd = (intptr_t)arg;
	read(fd, (void*)buf, sizeof(buf));
	close(fd);
	return NULL;
}

int main(int argc,char * argv[])
{
	int rc, nfail = 0;
	MDB_env *env;
	MDB_txn *txn;
	MDB_dbi dbi;
	MDB_val key, val;
	int pipefd[2];
	int socketfd[2];
	int filefd;
	pthread_t thr;

	key.mv_size = 7;
	key.mv_data = "testing";
	val.mv_size = VALSIZE;
	val.mv_data = (void*)malloc(VALSIZE);

	E(mdb_env_create(&env));
	E(mdb_env_set_mapsize(env, VALSIZE*3));
	E(mdb_env_open(env, "./testdb", 0, 0664));
	E(mdb_txn_begin(env, NULL, 0, &txn));
	E(mdb_dbi_open(txn, NULL, 0, &dbi));
	E(mdb_put(txn, dbi, &key, &val, 0));
	E(mdb_txn_commit(txn));
	txn = NULL;

	// pipe
	printf("testing pipe()\n");
	E_ERRNO(pipe(pipefd));
	E_ERRNO(close(pipefd[0]));
	rc = mdb_env_copyfd2(env, pipefd[1], MDB_CP_COMPACT);
	if (rc != EPIPE) {
		nfail++;
		fprintf(stderr, "invalid pipe return code: %s\n", mdb_strerror(rc));
	}

	// socketpair
	printf("testing socketpair()\n");
	E_ERRNO(socketpair(AF_UNIX, SOCK_STREAM, 0, socketfd));
	E(pthread_create(&thr, NULL, threadfn, (void *)(intptr_t)socketfd[0]));
	rc = mdb_env_copyfd2(env, socketfd[1], MDB_CP_COMPACT);
	if (rc != EPIPE && rc != ENOTCONN) {
		nfail++;
		fprintf(stderr, "invalid socketpair return code: %d %s\n", rc, mdb_strerror(rc));
	}

	// file, sanity check
	printf("testing file / open()\n");
	E_ERRNO(filefd = open("./testdb/temp.out", O_WRONLY|O_CREAT|O_TRUNC, 0644));
	rc = mdb_env_copyfd2(env, filefd, MDB_CP_COMPACT);
	if (rc) {
		nfail++;
		fprintf(stderr, "invalid file return code: %s\n", mdb_strerror(rc));
	}
	close(filefd);

	mdb_dbi_close(env, dbi);
	mdb_env_close(env);
	free(val.mv_data);

	printf("%d failures total\n", nfail);
	return nfail;
}
