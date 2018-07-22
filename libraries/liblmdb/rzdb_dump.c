/* mdb_dump.c - memory-mapped database dump tool */
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
#include <stdio.h>
#include <errno.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <unistd.h>
#include <signal.h>
#include "rzdb.h"

#define Yu	RZDB_PRIy(u)

#define PRINT	1
static int mode;

typedef struct flagbit {
	int bit;
	char *name;
} flagbit;

flagbit dbflags[] = {
	{ RZDB_REVERSEKEY, "reversekey" },
	{ RZDB_DUPSORT, "dupsort" },
	{ RZDB_INTEGERKEY, "integerkey" },
	{ RZDB_DUPFIXED, "dupfixed" },
	{ RZDB_INTEGERDUP, "integerdup" },
	{ RZDB_REVERSEDUP, "reversedup" },
	{ 0, NULL }
};

static volatile sig_atomic_t gotsig;

static void dumpsig( int sig )
{
	gotsig=1;
}

static const char hexc[] = "0123456789abcdef";

static void hex(unsigned char c)
{
	putchar(hexc[c >> 4]);
	putchar(hexc[c & 0xf]);
}

static void text(RZDB_val *v)
{
	unsigned char *c, *end;

	putchar(' ');
	c = v->mv_data;
	end = c + v->mv_size;
	while (c < end) {
		if (isprint(*c)) {
			putchar(*c);
		} else {
			putchar('\\');
			hex(*c);
		}
		c++;
	}
	putchar('\n');
}

static void byte(RZDB_val *v)
{
	unsigned char *c, *end;

	putchar(' ');
	c = v->mv_data;
	end = c + v->mv_size;
	while (c < end) {
		hex(*c++);
	}
	putchar('\n');
}

/* Dump in BDB-compatible format */
static int dumpit(RZDB_txn *txn, RZDB_dbi dbi, char *name)
{
	RZDB_cursor *mc;
	RZDB_stat ms;
	RZDB_val key, data;
	RZDB_envinfo info;
	unsigned int flags;
	int rc, i;

	rc = mdb_dbi_flags(txn, dbi, &flags);
	if (rc) return rc;

	rc = mdb_stat(txn, dbi, &ms);
	if (rc) return rc;

	rc = mdb_env_info(mdb_txn_env(txn), &info);
	if (rc) return rc;

	printf("VERSION=3\n");
	printf("format=%s\n", mode & PRINT ? "print" : "bytevalue");
	if (name)
		printf("database=%s\n", name);
	printf("type=btree\n");
	printf("mapsize=%"Yu"\n", info.me_mapsize);
	if (info.me_mapaddr)
		printf("mapaddr=%p\n", info.me_mapaddr);
	printf("maxreaders=%u\n", info.me_maxreaders);

	if (flags & RZDB_DUPSORT)
		printf("duplicates=1\n");

	for (i=0; dbflags[i].bit; i++)
		if (flags & dbflags[i].bit)
			printf("%s=1\n", dbflags[i].name);

	printf("db_pagesize=%d\n", ms.ms_psize);
	printf("HEADER=END\n");

	rc = mdb_cursor_open(txn, dbi, &mc);
	if (rc) return rc;

	while ((rc = mdb_cursor_get(mc, &key, &data, RZDB_NEXT) == RZDB_SUCCESS)) {
		if (gotsig) {
			rc = EINTR;
			break;
		}
		if (mode & PRINT) {
			text(&key);
			text(&data);
		} else {
			byte(&key);
			byte(&data);
		}
	}
	printf("DATA=END\n");
	if (rc == RZDB_NOTFOUND)
		rc = RZDB_SUCCESS;

	return rc;
}

static void usage(char *prog)
{
	fprintf(stderr, "usage: %s [-V] [-f output] [-l] [-n] [-p] [-a|-s subdb] dbpath\n", prog);
	exit(EXIT_FAILURE);
}

int main(int argc, char *argv[])
{
	int i, rc;
	RZDB_env *env;
	RZDB_txn *txn;
	RZDB_dbi dbi;
	char *prog = argv[0];
	char *envname;
	char *subname = NULL;
	int alldbs = 0, envflags = 0, list = 0;

	if (argc < 2) {
		usage(prog);
	}

	/* -a: dump main DB and all subDBs
	 * -s: dump only the named subDB
	 * -n: use NOSUBDIR flag on env_open
	 * -p: use printable characters
	 * -f: write to file instead of stdout
	 * -V: print version and exit
	 * (default) dump only the main DB
	 */
	while ((i = getopt(argc, argv, "af:lnps:V")) != EOF) {
		switch(i) {
		case 'V':
			printf("%s\n", RZDB_VERSION_STRING);
			exit(0);
			break;
		case 'l':
			list = 1;
			/*FALLTHROUGH*/;
		case 'a':
			if (subname)
				usage(prog);
			alldbs++;
			break;
		case 'f':
			if (freopen(optarg, "w", stdout) == NULL) {
				fprintf(stderr, "%s: %s: reopen: %s\n",
					prog, optarg, strerror(errno));
				exit(EXIT_FAILURE);
			}
			break;
		case 'n':
			envflags |= RZDB_NOSUBDIR;
			break;
		case 'p':
			mode |= PRINT;
			break;
		case 's':
			if (alldbs)
				usage(prog);
			subname = optarg;
			break;
		default:
			usage(prog);
		}
	}

	if (optind != argc - 1)
		usage(prog);

#ifdef SIGPIPE
	signal(SIGPIPE, dumpsig);
#endif
#ifdef SIGHUP
	signal(SIGHUP, dumpsig);
#endif
	signal(SIGINT, dumpsig);
	signal(SIGTERM, dumpsig);

	envname = argv[optind];
	rc = mdb_env_create(&env);
	if (rc) {
		fprintf(stderr, "mdb_env_create failed, error %d %s\n", rc, mdb_strerror(rc));
		return EXIT_FAILURE;
	}

	if (alldbs || subname) {
		mdb_env_set_maxdbs(env, 2);
	}

	rc = mdb_env_open(env, envname, envflags | RZDB_RDONLY, 0664);
	if (rc) {
		fprintf(stderr, "mdb_env_open failed, error %d %s\n", rc, mdb_strerror(rc));
		goto env_close;
	}

	rc = mdb_txn_begin(env, NULL, RZDB_RDONLY, &txn);
	if (rc) {
		fprintf(stderr, "mdb_txn_begin failed, error %d %s\n", rc, mdb_strerror(rc));
		goto env_close;
	}

	rc = mdb_open(txn, subname, 0, &dbi);
	if (rc) {
		fprintf(stderr, "mdb_open failed, error %d %s\n", rc, mdb_strerror(rc));
		goto txn_abort;
	}

	if (alldbs) {
		RZDB_cursor *cursor;
		RZDB_val key;
		int count = 0;

		rc = mdb_cursor_open(txn, dbi, &cursor);
		if (rc) {
			fprintf(stderr, "mdb_cursor_open failed, error %d %s\n", rc, mdb_strerror(rc));
			goto txn_abort;
		}
		while ((rc = mdb_cursor_get(cursor, &key, NULL, RZDB_NEXT_NODUP)) == 0) {
			char *str;
			RZDB_dbi db2;
			if (memchr(key.mv_data, '\0', key.mv_size))
				continue;
			count++;
			str = malloc(key.mv_size+1);
			memcpy(str, key.mv_data, key.mv_size);
			str[key.mv_size] = '\0';
			rc = mdb_open(txn, str, 0, &db2);
			if (rc == RZDB_SUCCESS) {
				if (list) {
					printf("%s\n", str);
					list++;
				} else {
					rc = dumpit(txn, db2, str);
					if (rc)
						break;
				}
				mdb_close(env, db2);
			}
			free(str);
			if (rc) continue;
		}
		mdb_cursor_close(cursor);
		if (!count) {
			fprintf(stderr, "%s: %s does not contain multiple databases\n", prog, envname);
			rc = RZDB_NOTFOUND;
		} else if (rc == RZDB_NOTFOUND) {
			rc = RZDB_SUCCESS;
		}
	} else {
		rc = dumpit(txn, dbi, subname);
	}
	if (rc && rc != RZDB_NOTFOUND)
		fprintf(stderr, "%s: %s: %s\n", prog, envname, mdb_strerror(rc));

	mdb_close(env, dbi);
txn_abort:
	mdb_txn_abort(txn);
env_close:
	mdb_env_close(env);

	return rc ? EXIT_FAILURE : EXIT_SUCCESS;
}
