#include <stdio.h>
#include <stdlib.h>
#include "sqlite3.h"

int main(int argc, char **argv) {
    int rc;
    sqlite3 *db = NULL; 
    
    // Database commands
    char create_sql[1024];
    snprintf(create_sql, sizeof(create_sql), "CREATE TABLE IF NOT EXISTS data_1 (id INTEGER PRIMARY KEY AUTOINCREMENT, st INTEGER, k TEXT NOT NULL, v TEXT);");
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

    sqlite3_stmt *stmt = NULL;
    sqlite3_prepare_v2(db, "insert into data_1 (st, k, v) values (?1, ?2, ?3);", -1, &stmt, NULL);
    sqlite3_bind_int(stmt, 1, 1);
    sqlite3_bind_text(stmt, 2, "insert into ()", -1, SQLITE_STATIC);
    sqlite3_bind_text(stmt, 3, "delete into ()", -1, SQLITE_STATIC);
    rc = sqlite3_step(stmt); 
    if (rc != SQLITE_DONE) {
        printf("ERROR inserting data: %s\n", sqlite3_errmsg(db));
        sqlite3_close(db);
        exit(1);
    }
    
    sqlite3_finalize(stmt);

    int callback(void *data, int argc, char **argv, char **azColName){
       int i;
       fprintf(stderr, "%s: ", (const char*)data);
       for(i=0; i<argc; i++){
          printf("%s = %s\n", azColName[i], argv[i] ? argv[i] : "NULL");
       }
       printf("\n");
       return 0;
    }

    snprintf(create_sql, sizeof(create_sql), "SELECT st,k,v FROM data_1 order by id LIMIT 1;");
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

