/**	@file midl.h
 *	@brief LMDB ID List header file.
 *
 *	This file was originally part of back-bdb but has been
 *	modified for use in libmdb. Most of the macros defined
 *	in this file are unused, just left over from the original.
 *
 *	This file is only used internally in libmdb and its definitions
 *	are not exposed publicly.
 */
/* $OpenLDAP$ */
/* This work is part of OpenLDAP Software <http://www.openldap.org/>.
 *
 * Copyright 2000-2018 The OpenLDAP Foundation.
 * Portions Copyright 2001-2018 Howard Chu, Symas Corp.
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

#ifndef _RZDB_MIDL_H_
#define _RZDB_MIDL_H_

#include "rzdb.h"

#ifdef __cplusplus
extern "C" {
#endif

/** @defgroup internal	LMDB Internals
 *	@{
 */

/** @defgroup idls	ID List Management
 *	@{
 */
	/** A generic unsigned ID number. These were entryIDs in back-bdb.
	 *	Preferably it should have the same size as a pointer.
	 */
typedef mdb_size_t RZDB_ID;

	/** An IDL is an ID List, a sorted array of IDs. The first
	 * element of the array is a counter for how many actual
	 * IDs are in the list. In the original back-bdb code, IDLs are
	 * sorted in ascending order. For libmdb IDLs are sorted in
	 * descending order.
	 */
typedef RZDB_ID *RZDB_IDL;

/* IDL sizes - likely should be even bigger
 *   limiting factors: sizeof(ID), thread stack size
 */
#ifdef RZDB_VL32
#define	RZDB_IDL_LOGN	14	/* DB_SIZE is 2^14, UM_SIZE is 2^15 */
#else
#define	RZDB_IDL_LOGN	16	/* DB_SIZE is 2^16, UM_SIZE is 2^17 */
#endif
#define RZDB_IDL_DB_SIZE		(1<<RZDB_IDL_LOGN)
#define RZDB_IDL_UM_SIZE		(1<<(RZDB_IDL_LOGN+1))

#define RZDB_IDL_DB_MAX		(RZDB_IDL_DB_SIZE-1)
#define RZDB_IDL_UM_MAX		(RZDB_IDL_UM_SIZE-1)

#define RZDB_IDL_SIZEOF(ids)		(((ids)[0]+1) * sizeof(RZDB_ID))
#define RZDB_IDL_IS_ZERO(ids) ( (ids)[0] == 0 )
#define RZDB_IDL_CPY( dst, src ) (memcpy( dst, src, RZDB_IDL_SIZEOF( src ) ))
#define RZDB_IDL_FIRST( ids )	( (ids)[1] )
#define RZDB_IDL_LAST( ids )		( (ids)[(ids)[0]] )

	/** Current max length of an #mdb_midl_alloc()ed IDL */
#define RZDB_IDL_ALLOCLEN( ids )	( (ids)[-1] )

	/** Append ID to IDL. The IDL must be big enough. */
#define mdb_midl_xappend(idl, id) do { \
		RZDB_ID *xidl = (idl), xlen = ++(xidl[0]); \
		xidl[xlen] = (id); \
	} while (0)

	/** Search for an ID in an IDL.
	 * @param[in] ids	The IDL to search.
	 * @param[in] id	The ID to search for.
	 * @return	The index of the first ID greater than or equal to \b id.
	 */
unsigned mdb_midl_search( RZDB_IDL ids, RZDB_ID id );

	/** Allocate an IDL.
	 * Allocates memory for an IDL of the given size.
	 * @return	IDL on success, NULL on failure.
	 */
RZDB_IDL mdb_midl_alloc(int num);

	/** Free an IDL.
	 * @param[in] ids	The IDL to free.
	 */
void mdb_midl_free(RZDB_IDL ids);

	/** Shrink an IDL.
	 * Return the IDL to the default size if it has grown larger.
	 * @param[in,out] idp	Address of the IDL to shrink.
	 */
void mdb_midl_shrink(RZDB_IDL *idp);

	/** Make room for num additional elements in an IDL.
	 * @param[in,out] idp	Address of the IDL.
	 * @param[in] num	Number of elements to make room for.
	 * @return	0 on success, ENOMEM on failure.
	 */
int mdb_midl_need(RZDB_IDL *idp, unsigned num);

	/** Append an ID onto an IDL.
	 * @param[in,out] idp	Address of the IDL to append to.
	 * @param[in] id	The ID to append.
	 * @return	0 on success, ENOMEM if the IDL is too large.
	 */
int mdb_midl_append( RZDB_IDL *idp, RZDB_ID id );

	/** Append an IDL onto an IDL.
	 * @param[in,out] idp	Address of the IDL to append to.
	 * @param[in] app	The IDL to append.
	 * @return	0 on success, ENOMEM if the IDL is too large.
	 */
int mdb_midl_append_list( RZDB_IDL *idp, RZDB_IDL app );

	/** Append an ID range onto an IDL.
	 * @param[in,out] idp	Address of the IDL to append to.
	 * @param[in] id	The lowest ID to append.
	 * @param[in] n		Number of IDs to append.
	 * @return	0 on success, ENOMEM if the IDL is too large.
	 */
int mdb_midl_append_range( RZDB_IDL *idp, RZDB_ID id, unsigned n );

	/** Merge an IDL onto an IDL. The destination IDL must be big enough.
	 * @param[in] idl	The IDL to merge into.
	 * @param[in] merge	The IDL to merge.
	 */
void mdb_midl_xmerge( RZDB_IDL idl, RZDB_IDL merge );

	/** Sort an IDL.
	 * @param[in,out] ids	The IDL to sort.
	 */
void mdb_midl_sort( RZDB_IDL ids );

	/** An ID2 is an ID/pointer pair.
	 */
typedef struct RZDB_ID2 {
	RZDB_ID mid;		/**< The ID */
	void *mptr;		/**< The pointer */
} RZDB_ID2;

	/** An ID2L is an ID2 List, a sorted array of ID2s.
	 * The first element's \b mid member is a count of how many actual
	 * elements are in the array. The \b mptr member of the first element is unused.
	 * The array is sorted in ascending order by \b mid.
	 */
typedef RZDB_ID2 *RZDB_ID2L;

	/** Search for an ID in an ID2L.
	 * @param[in] ids	The ID2L to search.
	 * @param[in] id	The ID to search for.
	 * @return	The index of the first ID2 whose \b mid member is greater than or equal to \b id.
	 */
unsigned mdb_mid2l_search( RZDB_ID2L ids, RZDB_ID id );


	/** Insert an ID2 into a ID2L.
	 * @param[in,out] ids	The ID2L to insert into.
	 * @param[in] id	The ID2 to insert.
	 * @return	0 on success, -1 if the ID was already present in the ID2L.
	 */
int mdb_mid2l_insert( RZDB_ID2L ids, RZDB_ID2 *id );

	/** Append an ID2 into a ID2L.
	 * @param[in,out] ids	The ID2L to append into.
	 * @param[in] id	The ID2 to append.
	 * @return	0 on success, -2 if the ID2L is too big.
	 */
int mdb_mid2l_append( RZDB_ID2L ids, RZDB_ID2 *id );

#ifdef RZDB_VL32
typedef struct RZDB_ID3 {
	RZDB_ID mid;		/**< The ID */
	void *mptr;		/**< The pointer */
	unsigned int mcnt;		/**< Number of pages */
	unsigned int mref;		/**< Refcounter */
} RZDB_ID3;

typedef RZDB_ID3 *RZDB_ID3L;

unsigned mdb_mid3l_search( RZDB_ID3L ids, RZDB_ID id );
int mdb_mid3l_insert( RZDB_ID3L ids, RZDB_ID3 *id );

#endif /* RZDB_VL32 */
/** @} */
/** @} */
#ifdef __cplusplus
}
#endif
#endif	/* _RZDB_MIDL_H_ */
