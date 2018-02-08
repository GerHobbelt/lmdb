/* sample-mdb.txt - MDB toy/sample
 *
 * Do a line-by-line comparison of this and sample-bdb.txt
 */
/*
 * Copyright 2012-2017 Howard Chu, Symas Corp.
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
#include <sys/stat.h>
#include <dirent.h>
#include <iostream>
#include "opencv2/opencv.hpp"
#include "lmdb.h"

typedef std::uint64_t hash_t;
constexpr hash_t prime = 0x100000001B3ull;
constexpr hash_t basis = 0xCBF29CE484222325ull;

hash_t hash_(char const* str)
{
	hash_t ret{basis};
	while(*str){
		ret ^= *str;
		ret *= prime;
		str++;
	}
	return ret;
}

constexpr hash_t hash_compile_time(char const* str, hash_t last_value = basis)
{
	return *str ? hash_compile_time(str + 1, (*str ^ last_value) * prime) : last_value;
}

void showAllFiles(const char *dir_name)
{
    // check the parameter !
    if(NULL == dir_name){
        std::cout << " dir_name is null ! " << std::endl;
        return;
    }
    // check if dir_name is a valid dir
    struct stat s;
    lstat(dir_name , &s);
    if(!S_ISDIR(s.st_mode)){
        std::cout << "dir_name is not a valid directory !" << std::endl;
        return;
    }

    struct dirent * filename;    // return value for readdir()
    DIR * dir;                   // return value for opendir()
    dir = opendir(dir_name);
    if (NULL == dir){
        std::cout << "Can not open dir " << dir_name << std::endl;
        return;
    }
    std::cout << "Successfully opened the dir !" << std::endl;

    /* read all the files in the dir ~ */
    while((filename = readdir(dir)) != NULL) {
        // get rid of "." and ".."
        if (0 == strcmp(filename->d_name, ".") || 0 == strcmp(filename->d_name, ".."))
            continue;
        if (strstr(filename->d_name, ".png")){
            std::cout << filename->d_name << std::endl;
            std::cout << strcat(filename->d_name, dir_name) << std::endl;
        }else
            continue;
    }
}

int main(int argc,char * argv[])
{
	int rc;
	MDB_env *env;
	MDB_dbi dbi;
	MDB_val key, data;
	MDB_txn *txn;
	MDB_cursor *cursor;

	char sval[32];
	std::string DB1Route;
    std::string dataRoute;

	if (argc < 2){
		std::cout << "Pls input parameters for manipulation." << std::endl;
		std::cout << "-p <DataBase abs route> <File List with abs route> " << "is for producing DB" << std::endl;
		std::cout << "-e <Begin NO.> <End No.> <DB route> " << "is for extracting from DB" << std::endl;
		std::cout << "-c <DataBase1 absolute route> <DataBase2 absolute route> " << "is for combining two DBs" << std::endl;
        std::cout << "-check <DataBase absolute route> " << "is for checking DB" << std::endl;
		return 0;
	}else{
		switch (hash_(argv[1])){
			case hash_compile_time("-p"):
				if (4 != argc){
					std::cout << "-p <DataBase abs route> <File List with abs route> " << "is for producing DB" << std::endl;
                    return 0;
				}else{
					DB1Route.assign(argv[2]);
                    dataRoute.assign(argv[3]);
                    mkdir(DB1Route.c_str(), 0777);
                    showAllFiles(dataRoute.c_str());
                    break;
				}
			default:
				return 0;
		}
	}

	/* Note: Most error checking omitted for simplicity */
	rc = mdb_env_create(&env);
	rc = mdb_env_open(env, DB1Route.c_str(), 0, 0664);

	rc = mdb_txn_begin(env, NULL, 0, &txn);
	rc = mdb_dbi_open(txn, NULL, 0, &dbi);
	key.mv_size = sizeof(int);
	key.mv_data = sval;
	data.mv_size = sizeof(sval);
	data.mv_data = sval;

	sprintf(sval, "%03x %d foo bar", 32, 3141592);
	rc = mdb_put(txn, dbi, &key, &data, 0);
	rc = mdb_txn_commit(txn);
	if (rc) {
		fprintf(stderr, "mdb_txn_commit: (%d) %s\n", rc, mdb_strerror(rc));
		goto leave;
	}

	rc = mdb_txn_begin(env, NULL, MDB_RDONLY, &txn);
	rc = mdb_cursor_open(txn, dbi, &cursor);
	while ((rc = mdb_cursor_get(cursor, &key, &data, MDB_NEXT)) == 0) {
		printf("key: %p %.*s, data: %p %.*s\n",
			key.mv_data,  (int) key.mv_size,  (char *) key.mv_data,
			data.mv_data, (int) data.mv_size, (char *) data.mv_data);
	}
	mdb_cursor_close(cursor);
	mdb_txn_abort(txn);

leave:
	mdb_dbi_close(env, dbi);
	mdb_env_close(env);
	return 0;
}
