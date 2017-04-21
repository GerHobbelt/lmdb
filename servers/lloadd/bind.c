/* $OpenLDAP$ */
/* This work is part of OpenLDAP Software <http://www.openldap.org/>.
 *
 * Copyright 1998-2020 The OpenLDAP Foundation.
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

#include "portable.h"

#include <ac/socket.h>
#include <ac/errno.h>
#include <ac/string.h>
#include <ac/time.h>
#include <ac/unistd.h>

#include "lutil.h"
#include "slap.h"

/*
 * On entering the function, we've put a reference on both connections and hold
 * upstream's c_io_mutex.
 */
static int
request_bind( Operation *op )
{
    Connection *client = op->o_client, *upstream = op->o_upstream;
    BerElement *ber, *copy = NULL;
    BerValue binddn;
    ber_tag_t tag;
    ber_int_t version;

    ber = upstream->c_pendingber;
    if ( ber == NULL && (ber = ber_alloc()) == NULL ) {
        Debug( LDAP_DEBUG_ANY, "request_bind: "
                "ber_alloc failed\n" );
        goto fail;
    }
    upstream->c_pendingber = ber;

    if ( (copy = ber_alloc()) == NULL ) {
        goto fail;
    }
    ber_init2( copy, &op->o_request, 0 );

    tag = ber_get_int( copy, &version );
    if ( tag == LBER_ERROR ) {
        goto fail;
    } else if ( version != LDAP_VERSION3 ) {
        ldap_pvt_thread_mutex_unlock( &upstream->c_io_mutex );
        operation_send_reject(
                op, LDAP_PROTOCOL_ERROR, "LDAP version unsupported", 1 );
        ber_free( copy, 0 );
        return 0;
    }

    tag = ber_get_stringbv( copy, &binddn, LBER_BV_NOTERM );
    if ( tag == LBER_ERROR ) {
        goto fail;
    }

    CONNECTION_LOCK(client);
    if ( !BER_BVISNULL( &client->c_auth ) ) {
        ch_free( client->c_auth.bv_val );
    }

    if ( !BER_BVISEMPTY( &binddn ) ) {
        char *ptr;
        client->c_auth.bv_len = STRLENOF("dn:") + binddn.bv_len;
        client->c_auth.bv_val = ch_malloc( client->c_auth.bv_len + 1 );

        ptr = lutil_strcopy( client->c_auth.bv_val, "dn:" );
        ptr = lutil_strncopy( ptr, binddn.bv_val, binddn.bv_len );
        *ptr = '\0';
    } else {
        BER_BVZERO( &client->c_auth );
    }
    CONNECTION_UNLOCK(client);

    CONNECTION_LOCK(upstream);
    op->o_upstream_msgid = upstream->c_next_msgid++;

    ber_printf( ber, "t{titOtO}", LDAP_TAG_MESSAGE,
            LDAP_TAG_MSGID, op->o_upstream_msgid,
            LDAP_REQ_BIND, &op->o_request,
            LDAP_TAG_CONTROLS, BER_BV_OPTIONAL( &op->o_ctrls ) );

    Debug( LDAP_DEBUG_TRACE, "request_bind: "
            "added bind from client connid=%lu to upstream connid=%lu as "
            "msgid=%d\n",
            op->o_client_connid, op->o_upstream_connid, op->o_upstream_msgid );
    if ( tavl_insert( &upstream->c_ops, op, operation_upstream_cmp,
                 avl_dup_error ) ) {
        assert(0);
    }
    CONNECTION_UNLOCK(upstream);

    ldap_pvt_thread_mutex_unlock( &upstream->c_io_mutex );

    ber_free( copy, 0 );
    upstream_write_cb( -1, 0, upstream );
    return 0;

fail:
    if ( copy ) {
        ber_free( copy, 0 );
    }
    ldap_pvt_thread_mutex_unlock( &upstream->c_io_mutex );
    return 1;
}

/*
 * On entering the function, we've put a reference on both connections and hold
 * upstream's c_io_mutex.
 */
static int
request_bind_as_vc( Operation *op )
{
    Connection *client = op->o_client, *upstream = op->o_upstream;
    BerElement *ber, *request, *copy = NULL;
    BerValue binddn, auth, mech;
    char *msg = "internal error";
    int result = LDAP_OTHER;
    ber_int_t version;
    ber_tag_t tag;
    ber_len_t len;

    if ( (request = ber_alloc()) == NULL ) {
        goto fail;
    }
    ber_init2( request, &op->o_request, 0 );

    tag = ber_scanf( request, "im", &version, &binddn );
    if ( tag == LBER_ERROR || version != LDAP_VERSION3 ) {
        result = LDAP_PROTOCOL_ERROR;
        msg = "version not recognised";
        goto fail;
    }

    copy = ber_dup( request );
    if ( !copy ) {
        goto fail;
    }

    tag = ber_skip_element( request, &auth );
    if ( tag == LBER_ERROR ) {
        result = LDAP_PROTOCOL_ERROR;
        msg = "malformed bind request";
        goto fail;
    }

    ber = upstream->c_pendingber;
    if ( ber == NULL && (ber = ber_alloc()) == NULL ) {
        Debug( LDAP_DEBUG_ANY, "request_bind_as_vc: "
                "ber_alloc failed\n" );
        goto fail;
    }
    upstream->c_pendingber = ber;

    op->o_upstream_msgid = upstream->c_next_msgid++;

    CONNECTION_LOCK(upstream);
    ber_printf( ber, "t{tit{tst{{tOOtOtO}}}}", LDAP_TAG_MESSAGE,
            LDAP_TAG_MSGID, op->o_upstream_msgid,
            LDAP_REQ_EXTENDED,
            LDAP_TAG_EXOP_REQ_OID, LDAP_EXOP_VERIFY_CREDENTIALS,
            LDAP_TAG_EXOP_REQ_VALUE,
            LDAP_TAG_EXOP_VERIFY_CREDENTIALS_COOKIE, BER_BV_OPTIONAL( &upstream->c_vc_cookie ),
            &binddn, tag, &auth,
            LDAP_TAG_EXOP_VERIFY_CREDENTIALS_CONTROLS, BER_BV_OPTIONAL( &op->o_ctrls ) );
    CONNECTION_UNLOCK(upstream);

    tag = ber_peek_tag( copy, &len );
    switch ( tag ) {
        case LDAP_AUTH_SASL:
            ber_get_stringbv( copy, &mech, LBER_BV_NOTERM );

            CONNECTION_LOCK(client);
            if ( ber_bvcmp( &mech, &client->c_sasl_bind_mech ) ) {
                ber_memfree( client->c_sasl_bind_mech.bv_val );
                ber_dupbv( &client->c_sasl_bind_mech, &mech );
            }
            CONNECTION_UNLOCK(client);
            /* TODO: extract authzdn from the message */
            break;
        case LDAP_AUTH_SIMPLE:
            CONNECTION_LOCK(client);
            if ( !BER_BVISNULL( &client->c_auth ) ) {
                ch_free( client->c_auth.bv_val );
            }
            if ( !BER_BVISEMPTY( &binddn ) ) {
                char *ptr;
                client->c_auth.bv_len = STRLENOF("dn:") + binddn.bv_len;
                client->c_auth.bv_val = ch_malloc( client->c_auth.bv_len + 1 );

                ptr = lutil_strcopy( client->c_auth.bv_val, "dn:" );
                ptr = lutil_strncopy( ptr, binddn.bv_val, binddn.bv_len );
                *ptr = '\0';
            } else {
                BER_BVZERO( &client->c_auth );
            }

            if ( !BER_BVISNULL( &client->c_sasl_bind_mech ) ) {
                ber_memfree( client->c_sasl_bind_mech.bv_val );
                BER_BVZERO( &client->c_sasl_bind_mech );
            }
            CONNECTION_UNLOCK(client);
            break;
        default:
            result = LDAP_PROTOCOL_ERROR;
            msg = "malformed bind request";
            goto fail;
    }

    CONNECTION_LOCK(upstream);
    Debug( LDAP_DEBUG_TRACE, "request_bind_as_vc: "
            "added bind from client connid=%lu to upstream connid=%lu as VC "
            "exop msgid=%d\n",
            op->o_client_connid, op->o_upstream_connid, op->o_upstream_msgid );
    if ( tavl_insert( &upstream->c_ops, op, operation_upstream_cmp,
                 avl_dup_error ) ) {
        assert(0);
    }
    CONNECTION_UNLOCK(upstream);

    ldap_pvt_thread_mutex_unlock( &upstream->c_io_mutex );

    ber_free( copy, 0 );
    upstream_write_cb( -1, 0, upstream );

    return 0;

fail:
    if ( copy ) {
        ber_free( copy, 0 );
    }
    ldap_pvt_thread_mutex_unlock( &upstream->c_io_mutex );
    operation_send_reject( op, result, msg, 1 );
    return 1;
}

void *
client_reset( void *ctx, void *arg )
{
    Operation *op = arg;
    Connection *c = op->o_client;
    TAvlnode *root;
    int freed, destroy = 1;

    CONNECTION_LOCK(c);
    root = c->c_ops;
    c->c_ops = NULL;
    c->c_state = SLAP_C_CLOSING;
    if ( op->o_tag == LDAP_REQ_BIND ) {
        c->c_state = SLAP_C_BINDING;
        destroy = 0;
    }
    if ( !BER_BVISNULL( &c->c_auth ) ) {
        ch_free( c->c_auth.bv_val );
        BER_BVZERO( &c->c_auth );
    }
    if ( !BER_BVISNULL( &c->c_sasl_bind_mech ) ) {
        ch_free( c->c_sasl_bind_mech.bv_val );
        BER_BVZERO( &c->c_sasl_bind_mech );
    }
    CONNECTION_UNLOCK_INCREF(c);

    tavl_delete( &root, op, operation_client_cmp );
    freed = tavl_free( root, (AVL_FREE)operation_abandon );

    Debug( LDAP_DEBUG_TRACE, "client_reset: "
            "dropped %d operations\n",
            freed );

    if ( destroy ) {
        operation_destroy( op );
        CLIENT_LOCK_DESTROY(c);
    } else {
        CONNECTION_LOCK_DECREF(c);
        CLIENT_UNLOCK_OR_DESTROY(c);
    }

    return NULL;
}

void *
client_bind( void *ctx, void *arg )
{
    Operation *op = arg;
    Connection *upstream, *client = op->o_client;
    int rc = 0;

    CONNECTION_LOCK(client);
    CONNECTION_UNLOCK_INCREF(client);

    client_reset( ctx, arg );

    upstream = backend_select( op );
    if ( !upstream ) {
        Debug( LDAP_DEBUG_STATS, "client_bind: "
                "no available connection found\n" );
        operation_send_reject(
                op, LDAP_UNAVAILABLE, "no connections available", 1 );
        CONNECTION_LOCK_DECREF(client);
        CLIENT_UNLOCK_OR_DESTROY(client);
        return NULL;
    }

    op->o_upstream = upstream;
    op->o_upstream_connid = upstream->c_connid;
    if ( lload_features & LLOAD_FEATURE_VC ) {
        rc = request_bind_as_vc( op );
    } else {
        rc = request_bind( op );
    }

    CONNECTION_LOCK_DECREF(upstream);
    UPSTREAM_UNLOCK_OR_DESTROY(upstream);

    if ( rc ) {
        CLIENT_LOCK_DESTROY(client);
        return NULL;
    }

    CONNECTION_LOCK_DECREF(client);
    rc = tavl_insert( &client->c_ops, op, operation_client_cmp, avl_dup_error );
    assert( rc == LDAP_SUCCESS );
    CLIENT_UNLOCK_OR_DESTROY(client);

    return NULL;
}