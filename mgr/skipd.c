//#define _XOPEN_SOURCE

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <fcntl.h>
#include <getopt.h>
#include <limits.h> // [LONG|INT][MIN|MAX]
#include <errno.h>  // errno
#include <string.h>
#include <signal.h>
#include <assert.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <sys/time.h>
#include <time.h>
#include <sys/un.h>
#include <sys/stat.h>
#include <unistd.h>
#include <syslog.h>

#include "coroutine.h"
#include "skipd.h"

enum ccr_break_state {
    ccr_break_continue = 0,
    ccr_break_curr,
    ccr_break_all,
    ccr_break_killed
};

enum ccr_error_code {
    ccr_error_err1 = -1,
    ccr_error_err2 = -2,
    ccr_error_err3 = -3,
    ccr_error_ok = 0, /* 此命令暂时中断，需等待再次执行 */
    ccr_error_ok1 = 1, /* 此命令成功结束执行 */
    ccr_error_ok2 = 2
};

typedef struct _skipd_client {
    ev_io io_read;
    ev_io io_write;

    struct ccrContextTag ccr_read;
    struct ccrContextTag ccr_write;
    struct ccrContextTag ccr_process;
    struct ccrContextTag ccr_runcmd;
    int break_level;

    int fd;

    char* command;
    char* key;
    int data_len;

    char* origin;
    int origin_len;
    int read_len;
    int read_pos;

    char* send;
    int send_max;
    int send_len;
    int send_pos;

    MDB_txn *rtx;
    skipd_server* server;
} skipd_client;

/* declare */
static void begin_write(EV_P_ skipd_client* client);
static void end_write(EV_P_ skipd_client* client);
static void begin_read(EV_P_ skipd_client* client);
static void end_read(EV_P_ skipd_client* client);

void skipd_daemonize(char * path);
static int setnonblock(int fd);
static int client_ccr_process(EV_P_ skipd_client* client);
static int client_ccr_write(EV_P_ skipd_client* client);
int commit_create(skipd_server *skipdb);
int commit_log(skipd_server* server, int status, char* key, int klen, char* value, int vlen);
int commit_flush(skipd_server* server);

char* global_magic = MAGIC;
skipd_server* global_server;
ev_signal signal_watcher;
ev_signal signal_watcher2;

#define STATIC_BUF_LEN 511
char static_buffer[STATIC_BUF_LEN+1];

#if 0
static void print_time(char* prefix)
{
    struct timeval tv;
    unsigned int ms;

    gettimeofday (&tv, NULL);
    ms = (tv.tv_sec * 1000) + (tv.tv_usec / 1000);
    printf("%s: %03u\n", prefix, ms);
}
#endif

static void sigint_cb (EV_P_ ev_signal *w, int revents) {
    ev_signal_stop (EV_A_ w);
    ev_break(EV_A_ EVBREAK_ALL);
}

void sys_script(char *cmd) {
    if(NULL == cmd) {
        return;
    }

    snprintf(static_buffer, STATIC_BUF_LEN, "%s > /tmp/.skipd/log 2>&1 &\n", cmd);
    system(static_buffer);
    strcpy(static_buffer, ""); // Ensure we don't re-execute it again
}

int log_level = LOG_ERR;
static void emit_log(int level, char* line) {
    //TODO for level
    //int syslog_level = LOG_ERR;
    //syslog(syslog_level, "%s", line);
    printf("%s", line);
}

static void _skipd_logv(int filter, const char *format, va_list vl)
{
	char buf[256];

        //TODO for log_evel
	/* if (!(log_level & filter)) {
	    return;
        } */

	vsnprintf(buf, sizeof(buf), format, vl);
	buf[sizeof(buf) - 1] = '\0';

	emit_log(filter, buf);
}

static void skipd_log(int filter, const char *format, ...)
{
	va_list ap;

	va_start(ap, format);
	_skipd_logv(filter, format, ap);
	va_end(ap);
}

#define E(expr) do { if((expr) != MDB_SUCCESS) {skipd_log(0, "%s#%d error: %s\n", __FUNCTION__, __LINE__, abort()); } }

//MDB_cmp_func
//mdb_set_compare
static int mdb_cmp_prefix(const MDB_val *a, const MDB_val *b)
{
	int diff;
	ssize_t len_diff;
	unsigned int len;

	len = a->mv_size;
	len_diff = (ssize_t) a->mv_size - (ssize_t) b->mv_size;
	if (len_diff > 0) {
		len = b->mv_size;
		len_diff = 1;
	}

	diff = memcmp(a->mv_data, b->mv_data, len);
	return diff ? diff : len_diff<0 ? -1 : len_diff;
}

static void client_release(EV_P_ skipd_client* client) {
    //skipd_log(SKIPD_DEBUG, "closing client\n");

    ev_io_stop(EV_A_ &client->io_read);
    ev_io_stop(EV_A_ &client->io_write);
    close(client->fd);

    if(NULL != client->rtx) {
        mdb_txn_abort(client->rtx);
        client->rtx = NULL;
    }

    if(NULL != client->send) {
        free(client->send);
    }

    if(NULL != client->origin) {
        free(client->origin);
    }

    free(client);
}

static void db_flush(skipd_server *server) {
    sqlite3_exec(server->sqlite_db, "COMMIT TRANSACTION", NULL, NULL, NULL);
    if(NULL != server->stmt) {
        sqlite3_finalize(server->stmt);
        server->stmt = NULL;
    }

    mdb_txn_commit(server->wtx);
    server->wtx = NULL;
    fprintf(stderr, "do transaction writing=%d\n", server->writing);
    server->writing = 0;
}

static void client_read_inner(EV_P_ skipd_client* client, int revents) {
    int rt;

    //process command
    assert(client->break_level == 0);

    rt = client_ccr_process(EV_A_ client);
    //skipd_log(SKIPD_DEBUG, "process:%d\n", rt);
    if(ccr_error_err1 == rt) {
        //Close it directly
        client_release(EV_A_ client);
    } else if(ccr_error_ok == rt) {
        //Just continue
        client->break_level = 0;
    } else if(rt < 0) {
        if(ccr_break_killed == client->break_level) {
            //wait for waiting completely
            if(0 == client->send_len) {
                //close it
                client_release(EV_A_ client);
            } else {
                memset(&client->ccr_process, 0, sizeof(struct ccrContextTag));
            }
        }
    } else if(rt > 0) {
        //OK and than reset
        memset(&client->ccr_process, 0, sizeof(struct ccrContextTag));
        client->break_level = 0;
    }
}

// This callback is called when client data is available
static void client_read(EV_P_ ev_io *w, int revents) {
    skipd_client* client = container_of(w, skipd_client, io_read);
    client_read_inner(EV_A_ client, revents);
}

static void client_write(EV_P_ ev_io *w, int revents) {
    skipd_client* client = container_of(w, skipd_client, io_write);
    int rt;

    //skipd_log(SKIPD_DEBUG, "begin for writing\n");

    rt = client_ccr_write(EV_A_ client);
    if(ccr_break_killed == client->break_level) {
        client_release(EV_A_ client);
    } else {
            // rt > 0, So reset it
            memset(&client->ccr_write, 0, sizeof(struct ccrContextTag));
            client->break_level = 0;

            //change to read state directly,
            //如果read过程还有资源未释放，跳回read进行资源释放
            //event_active(&client->io_read, EV_READ, 0);
            client_read_inner(EV_A_ client, 0);
    }
}

static int client_send_key(EV_P_ skipd_client* client, char* cmd, char* key, int klen, char* buf, int len) {
    char pref_buf[HEADER_PREFIX+1];
    int n, resp_len = (len + 2);

    memcpy(pref_buf, global_magic, MAGIC_LEN);

    resp_len += strlen(cmd);
    if(0 == klen) {
        klen = strlen(key);
    }
    resp_len += klen;

    sprintf(pref_buf + MAGIC_LEN, "%07d ", resp_len);
    resp_len += HEADER_PREFIX;

    if(NULL == client->send) {
        client->send = (char*)malloc(BUF_MAX+1);
        client->send_max = BUF_MAX;
        client->send_len = 0;
    }

    if(resp_len > client->send_max) {
        free(client->send);
        client->send = (char*)malloc(resp_len);
        client->send_max = resp_len;
        client->send_len = 0;
    }

    memcpy(client->send, pref_buf, HEADER_PREFIX);
    n = sprintf(client->send + HEADER_PREFIX, "%s %.*s ", cmd, klen, key);
    memcpy(client->send + HEADER_PREFIX + n, buf, len);
    client->send[resp_len] = '\0';
    client->send_len = resp_len;
    client->send_pos = 0;

    //switch to read state
    memset(&client->ccr_write, 0, sizeof(struct ccrContextTag));
    ev_io_stop(EV_A_ &client->io_read);
    ev_io_start(EV_A_ &client->io_write);

    return 0;
}

static int client_send(EV_P_ skipd_client* client, char* buf, int len) {
    return client_send_key(EV_A_ client
            , (NULL == client->command ? "errcmd": client->command)
            , (NULL == client->key ? "errkey": client->key)
            , 0
            , buf, len);
}

static int client_ccr_write(EV_P_ skipd_client* client) {
    struct ccrContextTag* ctx = &client->ccr_write;

    //stack
    ccrBeginContext
    int n;
    ccrEndContext(ctx);

    ccrBegin(ctx);
    while(client->send_pos < client->send_len) {
        //use send instead of write
        CS->n = write(client->fd, client->send + client->send_pos, client->send_len - client->send_pos);
        //CS->n = send(client->fd, client->send + client->send_pos, client->send_len - client->send_pos, 0);
        if(CS->n <= 0) {
            if(errno == EINTR || errno == EAGAIN) {
                ccrReturn(ctx, ccr_error_ok);
            } else {
                //TODO fixme
                client->send_len = 0;
                client->send_pos = 0;
                ev_io_stop(EV_A_ &client->io_write);
                ev_io_start(EV_A_ &client->io_read);

                ccrStop(ctx, ccr_error_err1);
            }
        }

        if((client->send_len -= CS->n) > 0) {
            //write again
            client->send_pos += CS->n;
            ccrReturn(ctx, ccr_error_ok);
        } else {
            //Finished write
            client->send_len = 0;
            client->send_pos = 0;
            ev_io_stop(EV_A_ &client->io_write);
            ev_io_start(EV_A_ &client->io_read);
            ccrReturn(ctx, ccr_error_ok1);
        }
    }
    ccrFinish(ctx, ccr_error_ok1);
}

static int client_ccr_read_util(skipd_client* client, int step_len) {
    int buf_len;
    struct ccrContextTag* ctx = &client->ccr_read;

    //stack
    ccrBeginContext
    int n;
    int left;
    ccrEndContext(ctx);

    ccrBegin(ctx);

    if(step_len > READ_MAX) {
        ccrReturn(ctx, ccr_error_err2);
    }

    buf_len = _max(step_len, BUF_MAX);
    if(buf_len > client->origin_len) {
        if(NULL != client->origin) {
            free(client->origin);
        }
        client->origin = (char*)malloc(buf_len + 1);
        client->origin_len = buf_len;
        client->origin[client->origin_len] = '\0';
    }
    client->read_pos = 0;
    client->read_len = 0;

    for(;;) {
        CS->left = step_len - client->read_len;
        assert(0 != CS->left);

        CS->n = recv(client->fd, client->origin + client->read_len, CS->left, 0);
        if (0 == CS->n) {
            //client closed
            ccrReturn(ctx, ccr_error_err1);
        } else if(CS->n < 0) {
            if (errno == EAGAIN || errno == EWOULDBLOCK) {
                client->break_level = ccr_break_all;
                ccrReturn(ctx, ccr_error_ok);
            } else {
                ccrReturn(ctx, ccr_error_err1);
            }
        } else {
            client->read_len += CS->n;
            if(client->read_len == step_len) {
                break;
            }
        }
    }
    ccrFinish(ctx, ccr_error_ok1);
}

static int client_run_command(EV_P_ skipd_client* client)
{
    char *p1, *p2;
    int rc, tmpi, tmp2;
    MDB_val dkey, dvalue;
    struct ccrContextTag* ctx = &client->ccr_read;

    //stack
    ccrBeginContext
    int n;
    MDB_cursor* cursor;
    MDB_val skey;
    MDB_val svalue;
    ccrEndContext(ctx);

    ccrBegin(ctx);

    p1 = client->origin;
    p2 = strstr(p1, " ");
    if(NULL == p2) {
        ccrStop(ctx, ccr_error_err2);
    }
    *p2 = '\0';
    client->command = p1;

    if(!strcmp(client->command, "set")) {
        p1 = p2+1;
        p2 = strstr(p1, " ");
        if(NULL == p2) {
            ccrStop(ctx, ccr_error_err2);
        }
        *p2 = '\0';
        client->key = p1;
        p1 = p2+1;

        dkey.mv_data = client->key;
        dkey.mv_size = strlen(client->key);
        dvalue.mv_data = p1;
        dvalue.mv_size = (client->data_len - (p1 - client->origin));
        begin_write(EV_A_ client);
        commit_log(client->server, 1, dkey.mv_data, dkey.mv_size, dvalue.mv_data, dvalue.mv_size); 
        mdb_put(client->server->wtx, client->server->dbi, &dkey, &dvalue, 0);
        end_write(EV_A_ client);

        p1 = "ok\n";
        client_send(EV_A_ client, p1, strlen(p1));
        ccrReturn(ctx, ccr_error_ok1);
    } else if(!strcmp(client->command, "get")) {
        p1 = p2+1;
        client->key = p1;
        p1 = p2+1;

        dkey.mv_data = client->key;
        dkey.mv_size = strlen(client->key);
        memset(&dvalue, 0, sizeof(dvalue));

        begin_read(EV_A_ client);
        rc = mdb_get(client->rtx, client->server->dbi, &dkey, &dvalue);
        end_read(EV_A_ client);

        if(MDB_NOTFOUND == rc || NULL == dvalue.mv_data) {
            //fprintf(stderr, "get none\n");
            p1 = "none\n";
            client_send(EV_A_ client, p1, strlen(p1));
        } else {
            //fprintf(stderr, "get result dvalue=%.*s\n", dvalue.size, dvalue.mv_data);
            client_send(EV_A_ client, (char*)dvalue.mv_data, dvalue.mv_size);
        }
        ccrReturn(ctx, ccr_error_ok1);
    } else if(!strcmp(client->command, "list")) {
        p1 = p2+1;
        client->key = p1;
        p1 = p2+1;

        CS->n = strlen(client->key);
        CS->skey.mv_size = CS->n;
        CS->skey.mv_data = client->key;
        begin_read(EV_A_ client);
        mdb_cursor_open(client->rtx, client->server->dbi, &CS->cursor);
        if(strcmp(client->key, "__all__")) {
            if((rc = mdb_cursor_get(CS->cursor, &CS->skey, &CS->svalue, MDB_SET_RANGE)) == 0) {
                //fprintf(stderr, "list:%.*s:%.*s\n", CS->skey.mv_size, CS->skey.mv_data, CS->svalue.mv_size, (char*)CS->svalue.mv_data);
                while(rc == 0) {
                    if((int)CS->skey.mv_size < CS->n) {
                        break;
                    }
                    if(0 != memcmp(CS->skey.mv_data, client->key, CS->n)) {
                        break;
                    }
                    client_send_key(EV_A_ client
                            , client->command
                            , (char*)CS->skey.mv_data, (int)CS->skey.mv_size
                            , (char*)CS->svalue.mv_data, (int)CS->svalue.mv_size);
                    ccrReturn(ctx, ccr_error_ok);
                    rc = mdb_cursor_get(CS->cursor, &CS->skey, &CS->svalue, MDB_NEXT);
                }
            }
        } else {
            //list __all__ implement
            while ((rc = mdb_cursor_get(CS->cursor, &CS->skey, &CS->svalue, MDB_NEXT)) == 0) {
                client_send_key(EV_A_ client
                        , client->command
                        , (char*)CS->skey.mv_data, (int)CS->skey.mv_size
                        , (char*)CS->svalue.mv_data, (int)CS->svalue.mv_size);
                ccrReturn(ctx, ccr_error_ok);
            }
        }
        if(NULL != CS->cursor) {
            mdb_cursor_close(CS->cursor);
        }
        end_read(EV_A_ client);

        //send end
        p1 = "__end__\n";
        client_send(EV_A_ client, p1, strlen(p1));
        ccrReturn(ctx, ccr_error_ok);

        ccrReturn(ctx, ccr_error_ok1);
    } else if(!strcmp(client->command, "remove")) {
        p1 = p2+1;
        client->key = p1;
        p1 = p2+1;

        dkey.mv_data = client->key;
        dkey.mv_size = strlen(client->key);

        begin_write(EV_A_ client);
        commit_log(client->server, 0, dkey.mv_data, dkey.mv_size, NULL, 0); 
        mdb_del(client->server->wtx, client->server->dbi, &dkey, NULL);
        end_write(EV_A_ client);

        p1 = "ok\n";
        client_send(EV_A_ client, p1, strlen(p1));
        ccrReturn(ctx, ccr_error_ok1);
    } else if((!strcmp(client->command, "inc")) || (!strcmp(client->command, "desc"))) {
        p1 = p2+1;
        p2 = strstr(p1, " ");
        if(NULL == p2) {
            ccrStop(ctx, ccr_error_err2);
        }
        *p2 = '\0';
        client->key = p1;
        p1 = p2+1;

        dkey.mv_data = client->key;
        dkey.mv_size = strlen(client->key);

        if(S2ISUCCESS != str2int(&tmpi, p1, 10)) {
            p1 = "error\n";
            client_send(EV_A_ client, p1, strlen(p1));
            client->break_level = ccr_break_killed;
            ccrStop(ctx, ccr_error_err2);
        }

        begin_read(EV_A_ client);
        rc = mdb_get(client->rtx, client->server->dbi, &dkey, &dvalue);
        if(MDB_NOTFOUND == rc || NULL == dvalue.mv_data) {
            tmp2 = 0;
        } else {
            if(S2ISUCCESS != str2int(&tmp2, (char*)dvalue.mv_data, 10)) {
                end_read(EV_A_ client);

                p1 = "error\n";
                client_send(EV_A_ client, p1, strlen(p1));
                ccrReturn(ctx, ccr_error_ok1);
            }
        }
        end_read(EV_A_ client);

        //TODO remote \n from data?
        if(!strcmp(client->command, "inc")) {
            sprintf(static_buffer, "%d", tmp2+tmpi);
        } else {
            sprintf(static_buffer, "%d", tmp2-tmpi);
        }
        dvalue.mv_data = static_buffer;
        dvalue.mv_size = strlen(static_buffer);

        begin_write(EV_A_ client);
        commit_log(client->server, 1, dkey.mv_data, dkey.mv_size, dvalue.mv_data, dvalue.mv_size); 
        mdb_put(client->server->wtx, client->server->dbi, &dkey, &dvalue, 0);
        end_write(EV_A_ client);

        client_send(EV_A_ client, (char*)dvalue.mv_data, dvalue.mv_size);;
        ccrReturn(ctx, ccr_error_ok1);
    }

    ccrFinish(ctx, ccr_error_err2);
}

static int client_ccr_process(EV_P_ skipd_client* client)
{
    char* p;
    struct ccrContextTag* ctx = &client->ccr_process;

    //stack
    ccrBeginContext
    int rt;
    int n;
    int left;
    ccrEndContext(ctx);

    ccrBegin(ctx);

    for(;;) {

        /* Sub routine */
        memset(&client->ccr_read, 0, sizeof(struct ccrContextTag));
        for(;;) {
            CS->rt = client_ccr_read_util(client, HEADER_PREFIX);
            if((ccr_error_err1 != CS->rt) && (CS->rt < 0)) {
                /* socket not closed but has some other error*/
                p = "command no found\n";
                client_send(EV_A_ client, p, strlen(p));
                client->break_level = ccr_break_killed;
                ccrStop(ctx, CS->rt);
            }

            /* TODO if(client->break_level >= ccr_break_all) {
                ccrReturn(ctx, CS->rt);
            } else if(ccr_break_curr == client->break_level) {
                client->break_level = ccr_break_continue;
                ccrReturn(ctx, CS->rt);
            } */

            /* OK */
            if(CS->rt > 0) {
                break;
            }
            ccrReturn(ctx, CS->rt);
        }

        assert(CS->rt > 0);
        if(0 != memcmp(client->origin, global_magic, MAGIC_LEN)) {
            p = "magic not found\n";
            client_send(EV_A_ client, p, strlen(p));
            client->break_level = ccr_break_killed;
            ccrStop(ctx, ccr_error_err2);
        }

        if(client->origin[HEADER_PREFIX-1] != ' ') {
            p = "header prefix error\n";
            client_send(EV_A_ client, p, strlen(p));
            client->break_level = ccr_break_killed;
            ccrStop(ctx, ccr_error_err2);
        }
        client->origin[HEADER_PREFIX-1] = '\0';

        if(S2ISUCCESS != str2int(&client->data_len, client->origin+MAGIC_LEN, 10)) {
            p = "data len error\n";
            client_send(EV_A_ client, p, strlen(p));
            client->break_level = ccr_break_killed;
            ccrStop(ctx, ccr_error_err2);
        }

        /* Sub routine */
        memset(&client->ccr_read, 0, sizeof(struct ccrContextTag));
        for(;;) {
            CS->rt = client_ccr_read_util(client, client->data_len);
            if((ccr_error_err1 != CS->rt) && (CS->rt < 0)) {
                p = "read data error\n";
                client_send(EV_A_ client, p, strlen(p));
                client->break_level = ccr_break_killed;
                ccrStop(ctx, CS->rt);
            }

            if(CS->rt > 0) {
                break;
            }
            ccrReturn(ctx, CS->rt);
        }

        assert(CS->rt > 0);
        client->origin[client->data_len] = '\0';
        //TODO must end with \n ?
        if('\n' != client->origin[client->data_len-1]) {
            p = "value error\n";
            client_send(EV_A_ client, p, strlen(p));
            client->break_level = ccr_break_killed;
            ccrReturn(ctx, ccr_error_err2);
        }
        client->origin[client->data_len-1] = '\0';

        // Command subroutine
        memset(&client->ccr_runcmd, 0, sizeof(struct ccrContextTag));
        for(;;) {
            CS->rt = client_run_command(EV_A_ client);
            if(CS->rt < 0) {
                p = "run command error\n";
                client_send(EV_A_ client, p, strlen(p));
                client->break_level = ccr_break_killed;
                ccrReturn(ctx, CS->rt);
            }
            if(CS->rt > 0) {
                break;
            }
            ccrReturn(ctx, CS->rt);
        }

        /* Reset the read_len */
        client->command = NULL;
        client->key = NULL;
        if(client->origin_len > BUF_MAX) {
            free(client->origin);
            client->origin = NULL;
            client->origin_len = 0;
        }
    }
    ccrFinish(ctx, ccr_error_ok1);
}

static skipd_client* client_new(int fd) {
    //int opt = 0;
    skipd_client* client = (skipd_client*)calloc(1, sizeof(skipd_client));
    client->fd = fd;
    client->send = NULL;
    client->origin = NULL;

    //setsockopt(client->fd, IPPROTO_TCP, TCP_NODELAY, &opt, sizeof(opt));
    setnonblock(client->fd);
    ev_io_init(&client->io_read, client_read, client->fd, EV_READ);
    ev_io_init(&client->io_write, client_write, client->fd, EV_WRITE);

    return client;
}

// This callback is called when data is readable on the unix socket.
static void server_cb(EV_P_ ev_io *w, int revents) {
    int client_fd;
    skipd_client* client;

    skipd_server* server = container_of(w, skipd_server, io);

    while (1) {
        client_fd = accept(server->fd, NULL, NULL);
        if( client_fd == -1 ) {
            if( errno != EAGAIN && errno != EWOULDBLOCK ) {
                skipd_log(SKIPD_DEBUG, "accept() failed errno=%i (%s)",  errno, strerror(errno));
                exit(EXIT_FAILURE);
            }
            break;
        }

        //print_time("newclient");
        client = client_new(client_fd);
        client->server = server;
        ev_io_start(EV_A_ &client->io_read);
    }
}

// Simply adds O_NONBLOCK to the file descriptor of choice
int setnonblock(int fd) {
    int flags;

    flags = fcntl(fd, F_GETFL);
    flags |= O_NONBLOCK;
    return fcntl(fd, F_SETFL, flags);
}

int unix_socket_init(struct sockaddr_un* socket_un, char* sock_path, int max_queue) {
    int fd;
    //int opt = 0;

    unlink(sock_path);

    // Setup a unix socket listener.
    fd = socket(PF_UNIX, SOCK_STREAM, 0);
    //fd = socketpair(AF_UNIX, SOCK_STREAM, 0);
    if (-1 == fd) {
        perror("echo server socket");
        exit(EXIT_FAILURE);
    }

    //SOL_TCP
    //setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, &opt, sizeof(opt));
    //opt = 1;
    //setsockopt(fd, SOL_SOCKET, SO_NOSIGPIPE, (void *)&opt, sizeof(int));

    // Set it non-blocking
    if (-1 == setnonblock(fd)) {
        perror("echo server socket nonblock");
        exit(EXIT_FAILURE);
    }

    // Set it as unix socket
    socket_un->sun_family = AF_UNIX;
    strcpy(socket_un->sun_path, sock_path);

    return fd;
}

static int server_open(skipd_server* server) {
    int rc = 0;
    struct stat st = {0};
    char *lmdb_path = "/tmp/.skipd";
    if(stat(server->db_path, &st) == -1) {
        fprintf(stderr, "create db_path\n");
        mkdir(server->db_path, 0700);
    }
    if(stat(lmdb_path, &st) == -1) {
        fprintf(stderr, "create lmdb\n");
        mkdir(lmdb_path, 0700);
    }
    unlink("/tmp/.skipd/data.mdb");
    unlink("/tmp/.skipd/lock.mdb");
    if(MDB_SUCCESS != mdb_env_create(&server->env)) {
        return -1;
    }
    if(MDB_SUCCESS != mdb_env_set_maxreaders(server->env, 32)) {
        return -2;
    }
    if(MDB_SUCCESS != mdb_env_set_mapsize(server->env, 4*1024*1024)) {
        return -3;
    }
    if(MDB_SUCCESS != (rc = mdb_env_open(server->env, lmdb_path, MDB_CREATE|MDB_FIXEDMAP|MDB_WRITEMAP|MDB_NOSYNC, 0664))) {
        return -rc;
    }
    mdb_txn_begin(server->env, NULL, 0, &server->wtx);
    mdb_dbi_open(server->wtx, NULL, 0, &server->dbi);
    mdb_set_compare(server->wtx, server->dbi, mdb_cmp_prefix);

    if(0 != (rc = commit_create(server))) {
        return rc;
    }
    //do once flush
    db_flush(server);

    return 0;
}

int server_init(skipd_server* server, int max_queue) {
    int rc;
    if(0 != (rc = server_open(server))) {
        fprintf(stderr, "Load database=%s error=%d\n", server->db_path, rc);
        exit(EXIT_FAILURE);
    }

    server->fd = unix_socket_init(&server->socket, server->sock_path, max_queue);
    server->socket_len = sizeof(server->socket.sun_family) + strlen(server->socket.sun_path);

    if (-1 == bind(server->fd, (struct sockaddr*) &server->socket, server->socket_len)) {
        perror("echo server bind");
        exit(EXIT_FAILURE);
    }

    if (-1 == listen(server->fd, max_queue)) {
        perror("listen");
        exit(EXIT_FAILURE);
    }
    return 0;
}

static struct option options[] = {
    { "help",	no_argument,		NULL, 'h' },
    { "db_path", required_argument,	NULL, 'd' },
    { "sock_path", required_argument,	NULL, 's' },
    { "daemon", 	required_argument,	NULL, 'D' },
    { NULL, 0, 0, 0 }
};

static void begin_write(EV_P_ skipd_client* client) {
    skipd_server *server = client->server;

    //make a flush
    if(server->writing >= 200) {
        db_flush(server);
    }

    if(NULL == server->wtx) {
        //release the cache rtx first
        if(NULL != server->cache_rtx) {
            mdb_txn_abort(server->cache_rtx);
            server->cache_rtx = NULL;
        }
        mdb_txn_begin(server->env, NULL, 0, &server->wtx);
        sqlite3_exec(server->sqlite_db, "BEGIN TRANSACTION", NULL, NULL, NULL);

        char sql[1024];
        snprintf(sql, sizeof(sql), "INSERT INTO data_%d (st, k, v) VALUES (?1, ?2, ?3);", server->data_num);
        if(SQLITE_OK != sqlite3_prepare_v2(server->sqlite_db, sql, -1, &server->stmt, NULL)) {
            fprintf(stderr, "ERROR prepare data: %s\n", sqlite3_errmsg(server->sqlite_db));
        }
    }
    server->writing++;
}

static void end_write(EV_P_ skipd_client* client) {
    //wait for 1s to commit
    ev_timer_again(EV_A_ &client->server->watcher);
}

static void begin_read(EV_P_ skipd_client* client) {
    skipd_server *server = client->server;
    //Do transaction before real read
    if(NULL != server->wtx) {
        db_flush(server);
    }
    if(NULL != server->cache_rtx) {
        // use cached rtx
        client->rtx = server->cache_rtx;
        server->cache_rtx = NULL;
        mdb_txn_renew(client->rtx);
    } else {
        // create a new rtx
        mdb_txn_begin(server->env, NULL, MDB_RDONLY, &client->rtx);
    }
}

static void end_read(EV_P_ skipd_client* client) {
    skipd_server *server = client->server;
    if(NULL == server->cache_rtx) {
        //cache the rtx
        server->cache_rtx = client->rtx;
        client->rtx = NULL;
        mdb_txn_reset(server->cache_rtx);
    } else {
        //free the client rtx
        mdb_txn_abort(client->rtx);
    }
}

static void server_sync_tick(EV_P_ ev_timer *w, int revents) {
    skipd_server *server = global_server;
    ev_timer_stop(EV_A_ w);
    if(NULL != server->wtx) {
        db_flush(server);
    }
    if(server->dirty > 1500) {
        fprintf(stderr, "dirty=%d flush\n", server->dirty);
        commit_flush(server);
    }
}

static int check_dbpath(skipd_server* server)
{
    struct stat s;
    int err, n = strlen(server->db_path);

    if('/' == server->db_path[n]) {
        server->db_path[n] = '\0';
    }
    err = stat(server->db_path, &s);
    if(-1 == err) {
        if(ENOENT == errno) {
            mkdir(server->db_path, 0700);
        } else {
            return -1;
        }
    } else {
        if(!S_ISDIR(s.st_mode)) {
            return -1;
        }
    }

    return 0;
}

int main(int argc, char **argv)
{
    int n = 0, daemon = 0, max_queue = 64;
    int syslog_options = LOG_PID | LOG_PERROR | LOG_DEBUG;
    skipd_server *server;
    //struct ev_periodic every_few_seconds;
    //ev_timer init_watcher = {0};
    EV_P  = ev_default_loop(0);

    global_server = (skipd_server*)calloc(1, sizeof(skipd_server));
    server = global_server;

    strcpy(server->sock_path, "/tmp/.skipd_server_sock");

#if 1
    strcpy(server->pid_path, "/tmp/.skipd_pid");
    strcpy(server->db_path, "/jffs/db");
    daemon = 1;
#endif

    while (n >= 0) {
            n = getopt_long(argc, argv, "hd:D:s:", options, NULL);
            if (n < 0)
                    continue;
            switch (n) {
            case 'D':
                //TODO fix me if optarg is bigger than PATH_MAX
                //strcpy(server->pid_path, optarg);
                daemon = 0;
                break;
            case 'd':
                strcpy(server->db_path, optarg);
                break;
            case 's':
                strcpy(server->sock_path, optarg);
                break;
            case 'h':
                fprintf(stderr, "Usage: skipd xxx todo\n");
                exit(1);
                break;
            }
    }

    if(0 == strlen(server->db_path) || (0 != check_dbpath(server))) {
        //syslog not initialize hear
        fprintf(stderr, "Database path error\n");
        return 1;
    }

    if(daemon) {
        skipd_daemonize(server->pid_path);
    }

    setlogmask(LOG_UPTO (LOG_DEBUG));
    openlog("skipd", syslog_options, LOG_DAEMON);

    int pid_file = open(server->pid_path, O_CREAT|O_RDWR, 0666);
    struct flock f1 = {0}, f2 = {0};
    f1.l_type = F_WRLCK;
    f1.l_whence = SEEK_SET;
    f1.l_start = 0;
    f1.l_len = 0;
    f2 = f1;

    if (fcntl(pid_file, F_GETLK, &f1) == -1) {  
        perror("fcntl");
        exit(1);
    }
    if (f1.l_type == F_WRLCK) {
        printf("Process %ld has a write lock already!\n", f1.l_pid);
        exit(1);
    } else {
        fcntl(pid_file, F_SETLK, &f2);
    }

    //kill -SIGUSR2 22459
    signal(SIGPIPE, SIG_IGN);
    signal(SIGABRT, SIG_IGN);
    ev_signal_init (&signal_watcher, sigint_cb, SIGINT);
    ev_signal_start (EV_A_ &signal_watcher);
    ev_signal_init (&signal_watcher2, sigint_cb, SIGUSR2);
    ev_signal_start (EV_A_ &signal_watcher2);

    // Create unix socket in non-blocking fashion
    server_init(server, max_queue);

    // Get notified whenever the socket is ready to read
    ev_io_init(&server->io, server_cb, server->fd, EV_READ);
    ev_io_start(EV_A_ &server->io);

    // To be sure that we aren't actually blocking
    //ev_periodic_init(&every_few_seconds, not_blocked, 0, 5, 0);
    //ev_periodic_start(EV_A_ &every_few_seconds);
    //ev_timer_init(EV_A_ &init_watcher, server_cmd_init, 5.0, 0.0);
    //ev_timer_start(EV_A_ &init_watcher);
    //update at 2s after write
    ev_timer_init(EV_A_ &server->watcher, server_sync_tick, 0.0, 1.0);
    //ev_timer_start(EV_A_ &server->watcher);

    // Run our loop, ostensibly forever
    ev_loop(EV_A_ 0);

    if(NULL != server->cache_rtx) {
        mdb_txn_abort(server->cache_rtx);
        server->cache_rtx = NULL;
    }
    if(NULL != server->wtx) {
        db_flush(server);
    }
    mdb_dbi_close(server->env, server->dbi);
    mdb_env_close(server->env);
    sqlite3_close(server->sqlite_db);
    //skipd_log(SKIPD_DEBUG, "skipd exited\n");

    // TODO free the ev
    //ev_signal_stop(EV_A_ &signal_watcher);
    //ev_signal_stop(EV_A_ &signal_watcher2);
    //ev_timer_stop(EV_A_ &server->watcher);
    //ev_io_stop(EV_A_ &server->io);
    //free(EV_DEFAULT);

    close(server->fd);
    if(NULL != server) {
        free(server);
    }
    return EXIT_SUCCESS;
}

