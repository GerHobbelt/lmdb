#include "skipd.h"

static int get_data_num(void *data, int argc, char **argv, char **azColName){
    skipd_server *server = (skipd_server*) data;
    str2int(&server->data_num, argv[0], 10);
    return 0;
}

static int replay(void *data, int argc, char **argv, char **azColName){
    skipd_server *server = (skipd_server*) data;
    MDB_val dkey, dvalue;
    int st = 0;
    if(argc != 3) {
        fprintf(stderr, "error argc=%d\n", argc);
        return 0;
    }
    str2int(&st, argv[0], 10);

    if(st == 0 || argv[1] == NULL || argv[2] == NULL) {
        //remove
        dkey.mv_data = (char*)argv[1];
        dkey.mv_size = strlen((char*)argv[1]);
        mdb_del(server->wtx, server->dbi, &dkey, NULL);
        server->dirty++;
    } else {
        //insert
        dkey.mv_data = (char*)argv[1];
        dkey.mv_size = strlen((char*)argv[1]);
        dvalue.mv_data = (char*)argv[2];
        dvalue.mv_size = strlen((char*)argv[2]);
        mdb_put(server->wtx, server->dbi, &dkey, &dvalue, 0);
    }
    return 0;
}

int commit_create(skipd_server *server) {
    int rc = 0;
    sqlite3 *db = NULL;
    char *exec_errmsg;
    char sql[1024];
    char path[256];

    do {
        snprintf(path, sizeof(path), "%s/log", server->db_path);
	snprintf(sql, sizeof(sql), "CREATE TABLE IF NOT EXISTS tkey (id INTEGER PRIMARY KEY AUTOINCREMENT, tid INTEGER)");
	rc = sqlite3_open(path, &db);
	if(SQLITE_OK != rc) {
            fprintf(stderr, "Can't open database %s (%i): %s\n", path, rc, sqlite3_errmsg(db));
            rc = -1;
            break;
	}

	rc = sqlite3_exec(db, sql, NULL, NULL, &exec_errmsg);
	if(SQLITE_OK != rc) {
	    fprintf(stderr, "Can't create table (%i): %s\n", rc, exec_errmsg);
            sqlite3_free(exec_errmsg);
            rc = -2;
            break;
        }

        snprintf(sql, sizeof(sql), "SELECT id,tid FROM tkey order by id desc LIMIT 1;");
        rc = sqlite3_exec(db, sql, get_data_num, (void*)server, &exec_errmsg);
        if( rc != SQLITE_OK ){
            fprintf(stderr, "SQL error: %s\n", exec_errmsg);
            sqlite3_free(exec_errmsg);
            rc = -3;
            break;
        }

        if(0 == server->data_num) {
            snprintf(sql, sizeof(sql), "INSERT INTO tkey(id, tid) VALUES (1, %d);", server->data_num);
            rc = sqlite3_exec(db, sql, NULL, NULL, &exec_errmsg);
            if(SQLITE_OK != rc) {
                fprintf(stderr, "SQL error: %s\n", exec_errmsg);
                sqlite3_free(exec_errmsg);
                rc = -4;
                break;
            }

            server->data_num = 1;
        }

        snprintf(sql, sizeof(sql), "CREATE TABLE IF NOT EXISTS data_%d (id INTEGER PRIMARY KEY AUTOINCREMENT, st INTEGER, k TEXT NOT NULL, v TEXT);", server->data_num);
	rc = sqlite3_exec(db, sql, NULL, NULL, &exec_errmsg);
	if(SQLITE_OK != rc) {
	    fprintf(stderr, "Can't create table (%i): %s\n", rc, exec_errmsg);
            sqlite3_free(exec_errmsg);
            rc = -5;
            break;
        }

        //replay commit log to lmdb
        snprintf(sql, sizeof(sql), "SELECT st,k,v FROM data_%d;", server->data_num);
        rc = sqlite3_exec(db, sql, replay, (void*)server, &exec_errmsg);
        if(rc != SQLITE_OK){
            fprintf(stderr, "SQL error: %s\n", exec_errmsg);
            sqlite3_free(exec_errmsg);
            rc = -6;
            break;
        }

        fprintf(stderr, "dirty=%d data_num=%d\n", server->dirty, server->data_num);
        rc = 0;

    } while(0);

    if(0 == rc) {
        server->sqlite_db = db;
    } else {
        sqlite3_close(db);
    }
    return rc;
}

int commit_log(skipd_server* server, int status, char* key, int klen, char* value, int vlen) {
    int rc = 0;
    sqlite3 *db = server->sqlite_db;
    sqlite3_stmt *stmt = server->stmt;
    if(0 == status) {
        server->dirty++;
    }
    sqlite3_bind_int(stmt, 1, status);
    sqlite3_bind_text(stmt, 2, key, klen, SQLITE_STATIC);
    if(vlen == 0) {
        sqlite3_bind_null(stmt, 3);
    } else {
        sqlite3_bind_text(stmt, 3, value, vlen, SQLITE_STATIC);
    }
    rc = sqlite3_step(stmt); 
    if (rc != SQLITE_DONE) {
        fprintf(stderr, "ERROR inserting data: %s\n", sqlite3_errmsg(db));
        rc = -2;
    }
    
    sqlite3_reset(stmt);
    return rc;
}

int commit_flush(skipd_server* server) {
    int rc = 0;
    char sql[1024];
    sqlite3 *db = server->sqlite_db;
    char *exec_errmsg;

    ++server->data_num;
    snprintf(sql, sizeof(sql), "CREATE TABLE IF NOT EXISTS data_%d (id INTEGER PRIMARY KEY AUTOINCREMENT, st INTEGER, k TEXT NOT NULL, v TEXT);", server->data_num);
    rc = sqlite3_exec(db, sql, NULL, NULL, &exec_errmsg);
    if(SQLITE_OK != rc) {
        fprintf(stderr, "Can't create table (%i): %s\n", rc, exec_errmsg);
        sqlite3_free(exec_errmsg);
        --server->data_num;
        return -1;
    }
    server->dirty = 0;

    //TODO better here
    sqlite3_exec(server->sqlite_db, "COMMIT TRANSACTION", NULL, NULL, NULL);
    snprintf(sql, sizeof(sql), "INSERT INTO data_%d (st, k, v) VALUES (?1, ?2, ?3);", server->data_num);
    if(SQLITE_OK != sqlite3_prepare_v2(db, sql, -1, &server->stmt, NULL)) {
        fprintf(stderr, "ERROR prepare data: %s\n", sqlite3_errmsg(db));
        return -1;
    }

    MDB_txn *rtx;
    MDB_cursor* cursor;
    MDB_val dkey, dvalue;
    mdb_txn_begin(server->env, NULL, MDB_RDONLY, &rtx);
    mdb_cursor_open(rtx, server->dbi, &cursor);
    while ((rc = mdb_cursor_get(cursor, &dkey, &dvalue, MDB_NEXT)) == 0) {
        commit_log(server, 1, dkey.mv_data, dkey.mv_size, dvalue.mv_data, dvalue.mv_size); 
    }

    mdb_cursor_close(cursor);
    mdb_txn_abort(rtx);
    sqlite3_exec(server->sqlite_db, "COMMIT TRANSACTION", NULL, NULL, NULL);
    sqlite3_finalize(server->stmt);
    server->stmt = NULL;

    //commit change
    snprintf(sql, sizeof(sql), "INSERT INTO tkey(tid) VALUES (%d);", server->data_num);
    rc = sqlite3_exec(db, sql, NULL, NULL, &exec_errmsg);
    if(SQLITE_OK != rc) {
        fprintf(stderr, "SQL error: %s\n", exec_errmsg);
        sqlite3_free(exec_errmsg);
        --server->data_num;
        return -2;
    }

    int old = server->data_num;
    snprintf(sql, sizeof(sql), "SELECT id,tid FROM tkey order by id desc LIMIT 1;");
    rc = sqlite3_exec(db, sql, get_data_num, (void*)server, &exec_errmsg);
    if( rc != SQLITE_OK ){
        fprintf(stderr, "SQL error: %s\n", exec_errmsg);
        sqlite3_free(exec_errmsg);
        return -3;
    }

    if(old != server->data_num) {
        //drop this table
        snprintf(sql, sizeof(sql), "DROP TABLE IF EXISTS data_%d;DROP TABLE IF EXISTS data_%d;VACUUM;", old-1, old);
    } else {
        snprintf(sql, sizeof(sql), "DROP TABLE IF EXISTS data_%d;VACUUM;", old-1);
    }
    rc = sqlite3_exec(db, sql, NULL, NULL, &exec_errmsg);
    if(SQLITE_OK != rc) {
        fprintf(stderr, "Can't drop table (%i): %s\n", rc, exec_errmsg);
        sqlite3_free(exec_errmsg);
        return -4;
    }

    return 0;
}

