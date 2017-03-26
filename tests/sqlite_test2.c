#include <stdio.h>
#include <stdlib.h>
#include "sqlite3.h"

int main(int argc, char **argv) {
        int rc;
	sqlite3 *db = NULL;

	// Database commands
	char create_sql[1024];
	snprintf(create_sql, sizeof(create_sql), "CREATE TABLE IF NOT EXISTS tkey (id INTEGER PRIMARY KEY AUTOINCREMENT, tid INTEGER)");

	rc = sqlite3_open(argv[1], &db);
	if(SQLITE_OK != rc) {
		fprintf(stderr, "Can't open database %s (%i): %s\n", argv[1], rc, sqlite3_errmsg(db));
		exit(1);
	}

	char *exec_errmsg;
	rc = sqlite3_exec(db, create_sql, NULL, NULL, &exec_errmsg);
	if(SQLITE_OK != rc) {
		fprintf(stderr, "Can't create table (%i): %s\n", rc, sqlite3_errmsg(db));
		sqlite3_close(db);
		exit(1);
	}

        snprintf(create_sql, sizeof(create_sql), "INSERT INTO tkey(tid) VALUES (1);");
	rc = sqlite3_exec(db, create_sql, NULL, NULL, &exec_errmsg);
	if(SQLITE_OK != rc) {
		fprintf(stderr, "Can't insert table (%i): %s\n", rc, sqlite3_errmsg(db));
		sqlite3_close(db);
		exit(1);
	}

        snprintf(create_sql, sizeof(create_sql), "INSERT INTO tkey(tid) VALUES (1);");
	rc = sqlite3_exec(db, create_sql, NULL, NULL, &exec_errmsg);
	if(SQLITE_OK != rc) {
		fprintf(stderr, "Can't insert table (%i): %s\n", rc, sqlite3_errmsg(db));
		sqlite3_close(db);
		exit(1);
	}

        int callback(void *data, int argc, char **argv, char **azColName){
           int i;
           fprintf(stderr, "%s: ", (const char*)data);
           for(i=0; i<argc; i++){
              printf("%s = %s\n", azColName[i], argv[i] ? argv[i] : "NULL");
           }
           printf("\n");
           return 0;
        }

        snprintf(create_sql, sizeof(create_sql), "SELECT id,tid FROM tkey order by id desc LIMIT 1;");
        rc = sqlite3_exec(db, create_sql, callback, (void*)"test", &exec_errmsg);
        if( rc != SQLITE_OK ){
            fprintf(stderr, "SQL error: %s\n", exec_errmsg);
            sqlite3_free(exec_errmsg);
        } else {
            fprintf(stdout, "Operation done successfully\n");
        }

        //TODO
        //SQLite> DELETE FROM COMPANY;
        //SQLite> VACUUM;

        return 0;
}

