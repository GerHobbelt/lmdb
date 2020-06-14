// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable InvalidXmlDocComment

using System;
using MDB_ID = System.UInt32;

namespace LMDB.CLR
{
  public static partial class Headers
  {
    public static Int32 CMP( MDB_ID x, MDB_ID y ) => x < y ? -1 : x > y ? 1 : 0;

    /* IDL sizes - likely should be even bigger
     *   limiting factors: sizeof(ID), thread stack size
     */
    public const Int32 MDB_IDL_LOGN = 16; /* DB_SIZE is 2^16, UM_SIZE is 2^17 */
    public const Int32 MDB_IDL_DB_SIZE = 1 << MDB_IDL_LOGN;
    public const Int32 MDB_IDL_UM_SIZE = 1 << MDB_IDL_LOGN + 1;
    public const Int32 MDB_IDL_DB_MAX = MDB_IDL_DB_SIZE - 1;
    public const Int32 MDB_IDL_UM_MAX = MDB_IDL_UM_SIZE - 1;

    public static MDB_ID MDB_IDL_SIZEOF( MDB_ID[] ids ) => ( ids[ 0 ] + 1 ) * sizeof( MDB_ID );
    public static Boolean MDB_IDL_IS_ZERO( MDB_ID[] ids ) => ids[ 0 ] == 0;
    public static void MDB_IDL_CPY( MDB_ID[] dst, MDB_ID[] src ) => Array.Copy( src, dst, src.Length );/* memcpy( dst, src, MDB_IDL_SIZEOF( src )*/
    public static MDB_ID MDB_IDL_FIRST( MDB_ID[] ids ) => ids[ 1 ];
    public static MDB_ID MDB_IDL_LAST( MDB_ID[] ids ) => ids[ ids[ 0 ] ];

    /** Current max length of an #mdb_midl_alloc()ed IDL */
    public static void MDB_IDL_ALLOCLEN( MDB_ID[] ids ) => throw new NotImplementedException(); // ( ids[ -1 ] );

    /** Append ID to IDL. The IDL must be big enough. */
    public static void mdb_midl_xappend( MDB_ID[] idl, MDB_ID id )
    {
      throw new NotImplementedException();
      //do
      //{
      //  MDB_ID xidl = idl;
      //  MDB_ID xlen = ++( xidl[ 0 ] );
      //  xidl[ xlen ] = id;
      //}
      //while ( 0 );
    }

    /** Search for an ID in an IDL.
     * @param[in] ids	The IDL to search.
     * @param[in] id	The ID to search for.
     * @return	The index of the first ID greater than or equal to \b id.
     */
    public static UInt32 mdb_midl_search( MDB_ID[] ids, MDB_ID id )
    {
      /*
       * binary search of id in ids
       * if found, returns position of id
       * if not found, returns first position greater than id
       */
      UInt32 @base = 0;
      UInt32 cursor = 1;
      var val = 0;
      var n = ids[ 0 ];

      while ( 0 < n )
      {
        var pivot = n >> 1;
        cursor = @base + pivot + 1;
        val = CMP( ids[ cursor ], id );

        if ( val < 0 )
        {
          n = pivot;
        }
        else if ( val > 0 )
        {
          @base = cursor;
          n -= pivot + 1;
        }
        else
        {
          return cursor;
        }
      }

      if ( val > 0 ) ++cursor;
      return cursor;
    }


    /** Allocate an IDL.
     * Allocates memory for an IDL of the given size.
     * @return	IDL on success, NULL on failure.
     */
    public static MDB_ID[] mdb_midl_alloc( UInt32 num )
    {
      var ids = new MDB_ID[ num + 2 ]; // malloc((num+2) * sizeof(MDB_ID));
      ids[ 0 ] = num;
      ids[ 1 ] = 0;
      return ids;
    }

    /** Free an IDL.
     * @param[in] ids	The IDL to free.
     */
    public static void mdb_midl_free( MDB_ID[] ids )
    {
      throw new NotImplementedException();
      //if ( ids != null ) free( ids - 1 );
    }

    /** Shrink an IDL.
     * Return the IDL to the default size if it has grown larger.
     * @param[in,out] idp	Address of the IDL to shrink.
     */
    public static void mdb_midl_shrink( MDB_ID[] idp )
    {
      throw new NotImplementedException();
      //var ids = idp;
      //if ( *( --ids ) > MDB_IDL_UM_MAX && ( ids = realloc( ids, ( MDB_IDL_UM_MAX + 2 ) * sizeof( MDB_ID ) ) ) )
      //{
      //  ids[0] = MDB_IDL_UM_MAX;
      //  ids[1] = ids;
      //}
    }

    public static Int32 mdb_midl_grow( MDB_ID[] idp, Int32 num )
    {
      throw new NotImplementedException();
      //MDB_ID[] idn = *idp - 1;
      /* grow it */
      //idn = realloc( idn, ( *idn + num + 2 ) * sizeof( MDB_ID ) );
      //if ( !idn ) return ENOMEM;
      //idn[0] += num;
      //idp[0] = idn;
      //return 0;
    }

    /** Make room for num additional elements in an IDL.
     * @param[in,out] idp	Address of the IDL.
     * @param[in] num	Number of elements to make room for.
     * @return	0 on success, ENOMEM on failure.
     */
    public static Int32 mdb_midl_need( ref MDB_ID[] idp, UInt32 num )
    {
      throw new NotImplementedException();
      //var ids = idp;
      //num += ids[ 0 ];
      //if ( num > ids[ -1 ] )
      //{
      //  num = (UInt32)( ( num + num / 4 + ( 256 + 2 ) ) & -256 );
      //  //if (!(ids = realloc( ids-1, num* sizeof(MDB_ID)))) return ENOMEM;
      //  ids[ 0 ] = num - 2;
      //  idp = ids;
      //}
      //return 0;
    }

    /** Append an ID onto an IDL.
     * @param[in,out] idp	Address of the IDL to append to.
     * @param[in] id	The ID to append.
     * @return	0 on success, ENOMEM if the IDL is too large.
     */
    public static Int32 mdb_midl_append( MDB_ID[] idp, MDB_ID id )
    {
      throw new NotImplementedException();
      //MDB_ID[] ids = idp;
      ///* Too big? */
      //if ( ids[ 0 ] >= ids[ -1 ] )
      //{
      //  if ( mdb_midl_grow( idp, MDB_IDL_UM_MAX ) )
      //    return ENOMEM;
      //  ids = *idp;
      //}
      //ids[ 0 ]++;
      //ids[ ids[ 0 ] ] = id;
      //return 0;
    }

    /** Append an IDL onto an IDL.
     * @param[in,out] idp	Address of the IDL to append to.
     * @param[in] app	The IDL to append.
     * @return	0 on success, ENOMEM if the IDL is too large.
     */
    public static Int32 mdb_midl_append_list( MDB_ID[] idp, MDB_ID[] app )
    {
      throw new NotImplementedException();
      //var ids = idp;
      ///* Too big? */
      //if (ids[0] + app[0] >= ids[-1]) {
      //	if (mdb_midl_grow(idp, app[0]))
      //		return ENOMEM;
      //	ids = *idp;
      //}
      //memcpy(&ids[ids[0]+1], &app[1], app[0] * sizeof(MDB_ID));
      //ids[0] += app[0];
      //return 0;
    }

    /** Append an ID range onto an IDL.
     * @param[in,out] idp	Address of the IDL to append to.
     * @param[in] id	The lowest ID to append.
     * @param[in] n		Number of IDs to append.
     * @return	0 on success, ENOMEM if the IDL is too large.
     */
    public static Int32 mdb_midl_append_range( MDB_ID[] idp, MDB_ID id, UInt32 n )
    {
      throw new NotImplementedException();
      //var ids = idp;
      //var len = ids[ 0 ];
      ///* Too big? */
      //if ( len + n > ids[ -1 ] )
      //{
      //  if ( mdb_midl_grow( idp, n | MDB_IDL_UM_MAX ) != 0 ) return ENOMEM;
      //  ids = idp;
      //}
      //ids[ 0 ] = len + n;
      //ids += len;
      //while ( n != 0 )
      //{
      //  ids[ n-- ] = id++;
      //}
      //return 0;
    }

    /** Merge an IDL onto an IDL. The destination IDL must be big enough.
     * @param[in] idl	The IDL to merge into.
     * @param[in] merge	The IDL to merge.
     */
    public static void mdb_midl_xmerge( MDB_ID[] idl, MDB_ID[] merge )
    {
      // throw new NotImplementedException();
      var i = merge[ 0 ];
      var j = idl[ 0 ];
      var k = i + j;
      var total = k;
      idl[ 0 ] = MDB_ID.MaxValue;    /* delimiter for idl scan below */
      while ( i != 0 )
      {
        var merge_id = merge[ i-- ];
        for ( var old_id = idl[ j ]; old_id < merge_id; old_id = idl[ --j ] )
        {
          idl[ k-- ] = old_id;
        }
        idl[ k-- ] = merge_id;
      }
      idl[ 0 ] = total;
    }

    public const Int32 SMALL = 8;

    public static void MIDL_SWAP( ref MDB_ID a, ref MDB_ID b )
    {
      var itmp = a;
      a = b;
      b = itmp;
    }

    public const Int32 CHAR_BIT = 8;

    /** Sort an IDL.
     * @param[in,out] ids	The IDL to sort.
     */
    public static void mdb_midl_sort( MDB_ID[] ids )
    {
      /* Max possible depth of int-indexed tree * 2 items/level */
      var istack = new Int32[ sizeof( Int32 ) * CHAR_BIT * 2 ];

      var ir = (Int32)ids[ 0 ];
      var l = 1;
      var jstack = 0;
      for (; ; )
      {
        Int32 i;
        Int32 j;
        MDB_ID a;
        if ( ir - l < SMALL )
        { /* Insertion sort */
          for ( j = l + 1; j <= ir; j++ )
          {
            a = ids[ j ];
            for ( i = j - 1; i >= 1; i-- )
            {
              if ( ids[ i ] >= a ) break;
              ids[ i + 1 ] = ids[ i ];
            }
            ids[ i + 1 ] = a;
          }
          if ( jstack == 0 ) break;
          ir = istack[ jstack-- ];
          l = istack[ jstack-- ];
        }
        else
        {
          var k = ( l + ir ) >> 1;
          MIDL_SWAP( ref ids[ k ], ref ids[ l + 1 ] );
          if ( ids[ l ] < ids[ ir ] ) MIDL_SWAP( ref ids[ l ], ref ids[ ir ] );
          if ( ids[ l + 1 ] < ids[ ir ] ) MIDL_SWAP( ref ids[ l + 1 ], ref ids[ ir ] );
          if ( ids[ l ] < ids[ l + 1 ] ) MIDL_SWAP( ref ids[ l ], ref ids[ l + 1 ] );
          i = l + 1;
          j = ir;
          a = ids[ l + 1 ];
          for (; ; )
          {
            do i++; while ( ids[ i ] > a );
            do j--; while ( ids[ j ] < a );
            if ( j < i ) break;
            MIDL_SWAP( ref ids[ i ], ref ids[ j ] );
          }
          ids[ l + 1 ] = ids[ j ];
          ids[ j ] = a;
          jstack += 2;
          if ( ir - i + 1 >= j - l )
          {
            istack[ jstack ] = ir;
            istack[ jstack - 1 ] = i;
            ir = j - 1;
          }
          else
          {
            istack[ jstack ] = j - 1;
            istack[ jstack - 1 ] = l;
            l = i;
          }
        }
      }
    }

    /** An ID2 is an ID/pointer pair.
     */
    public class MDB_ID2
    {
      public MDB_ID mid;    /**< The ID */
      public MDB_page mptr;   /**< The pointer */
    }

    /** An ID2L is an ID2 List, a sorted array of ID2s.
     * The first element's \b mid member is a count of how many actual
     * elements are in the array. The \b mptr member of the first element is unused.
     * The array is sorted in ascending order by \b mid.
     */

    /** Search for an ID in an ID2L.
     * @param[in] ids	The ID2L to search.
     * @param[in] id	The ID to search for.
     * @return	The index of the first ID2 whose \b mid member is greater than or equal to \b id.
     */
    public static UInt32 mdb_mid2l_search( MDB_ID2[] ids, MDB_ID id )
    {
      /*
       * binary search of id in ids
       * if found, returns position of id
       * if not found, returns first position greater than id
       */
      UInt32 @base = 0;
      UInt32 cursor = 1;
      var val = 0;
      var n = (UInt32)ids[ 0 ].mid;

      while ( 0 < n )
      {
        var pivot = n >> 1;
        cursor = @base + pivot + 1;
        val = CMP( id, ids[ cursor ].mid );

        if ( val < 0 )
        {
          n = pivot;

        }
        else if ( val > 0 )
        {
          @base = cursor;
          n -= pivot + 1;

        }
        else
        {
          return cursor;
        }
      }

      if ( val > 0 )
      {
        ++cursor;
      }
      return cursor;
    }

    /** Insert an ID2 into a ID2L.
     * @param[in,out] ids	The ID2L to insert into.
     * @param[in] id	The ID2 to insert.
     * @return	0 on success, -1 if the ID was already present in the ID2L.
     */
    public static Int32 mdb_mid2l_insert( MDB_ID2[] ids, MDB_ID2 id )
    {
      var x = mdb_mid2l_search( ids, id.mid );

      if ( x < 1 ) return -2; /* internal error */
      if ( x <= ids[ 0 ].mid && ids[ x ].mid == id.mid ) return -1; /* duplicate */
      if ( ids[ 0 ].mid >= MDB_IDL_UM_MAX ) return -2; /* too big */

      /* insert id */
      ids[ 0 ].mid++;
      for ( var i = (UInt32)ids[ 0 ].mid; i > x; i-- )
      {
        ids[ i ] = ids[ i - 1 ];
      }
      ids[ x ] = id;

      return 0;
    }

    /** Append an ID2 into a ID2L.
     * @param[in,out] ids	The ID2L to append into.
     * @param[in] id	The ID2 to append.
     * @return	0 on success, -2 if the ID2L is too big.
     */
    public static Int32 mdb_mid2l_append( MDB_ID2[] ids, MDB_ID2 id )
    {
      /* Too big? */
      if ( ids[ 0 ].mid >= MDB_IDL_UM_MAX ) return -2;

      ids[ 0 ].mid++;
      ids[ ids[ 0 ].mid ] = id;
      return 0;
    }
  }
}