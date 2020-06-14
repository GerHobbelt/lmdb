// ReSharper disable ParameterHidesMember
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable InvalidXmlDocComment
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ArrangeRedundantParentheses
// ReSharper disable MergeConditionalExpression
// ReSharper disable StringLiteralTypo
// ReSharper disable UseNullPropagation
#pragma warning disable 169

using System;
using mdb_mode_t = System.Int32;
using mdb_size_t = System.Int64;
using size_t = System.Int64;
using mdb_filehandle_t = System.IO.FileStream;
using MDB_dbi = System.UInt32;
using NTSTATUS = System.Int32;
using PHANDLE = System.IntPtr;
using ACCESS_MASK = System.Int32;
using PLARGE_INTEGER = System.Int32;
using ULONG = System.UInt32;
using HANDLE = System.IntPtr;
using ULONG_PTR = System.UInt32;
using SIZE_T = System.Int64;
using PSIZE_T = System.Int64;
using SSIZE_T = System.Int64;
using MDB_PID_T = System.Int32;
using MDB_THR_T = System.Int16;
using MDB_OFF_T = System.IntPtr;
using THREAD_RET = System.Int16;
using pthread_t = System.Int64;
using pthread_mutex_t = System.IntPtr;
using pthread_cond_t = System.IntPtr;
using mdb_mutex_t = System.IntPtr;
using mdb_mutexref_t = System.IntPtr;
using pthread_key_t = System.Int16;
using MDB_ID = System.UInt32;
using pgno_t = System.UInt32;
using txnid_t = System.UInt32;
using indx_t = System.UInt16;
using mdb_hash_t = System.UInt64;
using uint32_t = System.UInt32;
using uint16_t = System.UInt16;

#define _MSC_VER
#define _WIN32

namespace LMDB.CLR
{
  public static partial class Headers
  {
    /** @file lmdb.h
 *  @brief Lightning memory-mapped database library
 *
 *  @mainpage  Lightning Memory-Mapped Database Manager (LMDB)
 *
 *  @section intro_sec Introduction
 *  LMDB is a Btree-based database management library modeled loosely on the
 *  BerkeleyDB API, but much simplified. The entire database is exposed
 *  in a memory map, and all data fetches return data directly
 *  from the mapped memory, so no malloc's or memcpy's occur during
 *  data fetches. As such, the library is extremely simple because it
 *  requires no page caching layer of its own, and it is extremely high
 *  performance and memory-efficient. It is also fully transactional with
 *  full ACID semantics, and when the memory map is read-only, the
 *  database integrity cannot be corrupted by stray pointer writes from
 *  application code.
 *
 *  The library is fully thread-aware and supports concurrent read/write
 *  access from multiple processes and threads. Data pages use a copy-on-
 *  write strategy so no active data pages are ever overwritten, which
 *  also provides resistance to corruption and eliminates the need of any
 *  special recovery procedures after a system crash. Writes are fully
 *  serialized; only one write transaction may be active at a time, which
 *  guarantees that writers can never deadlock. The database structure is
 *  multi-versioned so readers run with no locks; writers cannot block
 *  readers, and readers don't block writers.
 *
 *  Unlike other well-known database mechanisms which use either write-ahead
 *  transaction logs or append-only data writes, LMDB requires no maintenance
 *  during operation. Both write-ahead loggers and append-only databases
 *  require periodic checkpointing and/or compaction of their log or database
 *  files otherwise they grow without bound. LMDB tracks free pages within
 *  the database and re-uses them for new write operations, so the database
 *  size does not grow without bound in normal use.
 *
 *  The memory map can be used as a read-only or read-write map. It is
 *  read-only by default as this provides total immunity to corruption.
 *  Using read-write mode offers much higher write performance, but adds
 *  the possibility for stray application writes thru pointers to silently
 *  corrupt the database. Of course if your application code is known to
 *  be bug-free (...) then this is not an issue.
 *
 *  If this is your first time using a transactional embedded key/value
 *  store, you may find the \ref starting page to be helpful.
 *
 *  @section caveats_sec Caveats
 *  Troubleshooting the lock file, plus semaphores on BSD systems:
 *
 *  - A broken lockfile can cause sync issues.
 *    Stale reader transactions left behind by an aborted program
 *    cause further writes to grow the database quickly, and
 *    stale locks can block further operation.
 *
 *    Fix: Check for stale readers periodically, using the
 *    #mdb_reader_check function or the \ref mdb_stat_1 "mdb_stat" tool.
 *    Stale writers will be cleared automatically on most systems:
 *    - Windows - automatic
 *    - BSD, systems using SysV semaphores - automatic
 *    - Linux, systems using POSIX mutexes with Robust option - automatic
 *    Otherwise just make all programs using the database close it;
 *    the lockfile is always reset on first open of the environment.
 *
 *  - On BSD systems or others configured with MDB_USE_SYSV_SEM or
 *    MDB_USE_POSIX_SEM,
 *    startup can fail due to semaphores owned by another userid.
 *
 *    Fix: Open and close the database as the user which owns the
 *    semaphores (likely last user) or as root, while no other
 *    process is using the database.
 *
 *  Restrictions/caveats (in addition to those listed for some functions):
 *
 *  - Only the database owner should normally use the database on
 *    BSD systems or when otherwise configured with MDB_USE_POSIX_SEM.
 *    Multiple users can cause startup to fail later, as noted above.
 *
 *  - There is normally no pure read-only mode, since readers need write
 *    access to locks and lock file. Exceptions: On read-only filesystems
 *    or with the #MDB_NOLOCK flag described under #mdb_env_open().
 *
 *  - An LMDB configuration will often reserve considerable \b unused
 *    memory address space and maybe file size for future growth.
 *    This does not use actual memory or disk space, but users may need
 *    to understand the difference so they won't be scared off.
 *
 *  - By default, in versions before 0.9.10, unused portions of the data
 *    file might receive garbage data from memory freed by other code.
 *    (This does not happen when using the #MDB_WRITEMAP flag.) As of
 *    0.9.10 the default behavior is to initialize such memory before
 *    writing to the data file. Since there may be a slight performance
 *    cost due to this initialization, applications may disable it using
 *    the #MDB_NOMEMINIT flag. Applications handling sensitive data
 *    which must not be written should not use this flag. This flag is
 *    irrelevant when using #MDB_WRITEMAP.
 *
 *  - A thread can only use one transaction at a time, plus any child
 *    transactions.  Each transaction belongs to one thread.  See below.
 *    The #MDB_NOTLS flag changes this for read-only transactions.
 *
 *  - Use an MDB_env* in the process which opened it, not after fork().
 *
 *  - Do not have open an LMDB database twice in the same process at
 *    the same time.  Not even from a plain open() call - close()ing it
 *    breaks fcntl() advisory locking.  (It is OK to reopen it after
 *    fork() - exec*(), since the lockfile has FD_CLOEXEC set.)
 *
 *  - Avoid long-lived transactions.  Read transactions prevent
 *    reuse of pages freed by newer write transactions, thus the
 *    database can grow quickly.  Write transactions prevent
 *    other write transactions, since writes are serialized.
 *
 *  - Avoid suspending a process with active transactions.  These
 *    would then be "long-lived" as above.  Also read transactions
 *    suspended when writers commit could sometimes see wrong data.
 *
 *  ...when several processes can use a database concurrently:
 *
 *  - Avoid aborting a process with an active transaction.
 *    The transaction becomes "long-lived" as above until a check
 *    for stale readers is performed or the lockfile is reset,
 *    since the process may not remove it from the lockfile.
 *
 *    This does not apply to write transactions if the system clears
 *    stale writers, see above.
 *
 *  - If you do that anyway, do a periodic check for stale readers. Or
 *    close the environment once in a while, so the lockfile can get reset.
 *
 *  - Do not use LMDB databases on remote filesystems, even between
 *    processes on the same host.  This breaks flock() on some OSes,
 *    possibly memory map sync, and certainly sync between programs
 *    on different hosts.
 *
 *  - Opening a database can fail if another process is opening or
 *    closing it at exactly the same time.
 *
 *  @author  Howard Chu, Symas Corporation.
 *
 *  @copyright Copyright 2011-2020 Howard Chu, Symas Corp. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted only as authorized by the OpenLDAP
 * Public License.
 *
 * A copy of this license is available in the file LICENSE in the
 * top-level directory of the distribution or, alternatively, at
 * <http://www.OpenLDAP.org/license.html>.
 *
 *  @par Derived From:
 * This code is derived from btree.c written by Martin Hedenfalk.
 *
 * Copyright (c) 2009, 2010 Martin Hedenfalk <martin@bzero.se>
 *
 * Permission to use, copy, modify, and distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

    public const Char MDB_FMT_Z = 'I';

    /** Unsigned type used for mapsize, entry counts and page  ransaction IDs.
     *
     *  It is normally size_t, hence the name. Defining MDB_VL32 makes it
     *  uint64_t, but do not try this unless you know what you are doing.
     */
    public const mdb_size_t MDB_SIZE_MAX = Int64.MaxValue;   /**< max #mdb_size_t */
    /** #mdb_size_t printf formats, \b t = one of [diouxX] without quotes */

    [Obsolete( "This is a macro that needs to be expanded when used - #define MDB_PRIy(t)  MDB_FMT_Z #t" )]
    public static Object MDB_PRIy = null;

    /** #mdb_size_t scanf formats, \b t = one of [dioux] without quotes */
    [Obsolete( "This is a macro that needs to be expanded when used - #define MDB_SCNy(t)  MDB_FMT_Z #t" )]
    public static Object MDB_SCNy = null;

    /** @defgroup mdb LMDB API
     *  @{
     *  @brief OpenLDAP Lightning Memory-Mapped Database Manager
     */
    /** @defgroup Version Version Macros
     *  @{
     */
    /** Library major version */
    public const Int32 MDB_VERSION_MAJOR = 0;
    /** Library minor version */
    public const Int32 MDB_VERSION_MINOR = 9;
    /** Library patch version */
    public const Int32 MDB_VERSION_PATCH = 70;

    /** Combine args a,b,c into a single integer for easy version comparisons */
    public static Int32 MDB_VERINT( Int32 a, Int32 b, Int32 c ) => a << 24 | b << 16 | c;

    /** The full library version as a single integer */
    public static Int32 MDB_VERSION_FULL = MDB_VERINT( MDB_VERSION_MAJOR, MDB_VERSION_MINOR, MDB_VERSION_PATCH );

    /** The release date of this library version */
    public const String MDB_VERSION_DATE = "December 19, 2015";

    /** A stringifier for the version info */
    public static String MDB_VERSTR( Int32 a, Int32 b, Int32 c, String d ) => $"LMDB {a}.{b}.{c}: ({d})";

    /** A helper for the stringifier macro */
    public static String MDB_VERFOO( Int32 a, Int32 b, Int32 c, String d ) => MDB_VERSTR( a, b, c, d );

    /** The full library version as a C string */
    public static String MDB_VERSION_STRING = MDB_VERFOO( MDB_VERSION_MAJOR, MDB_VERSION_MINOR, MDB_VERSION_PATCH, MDB_VERSION_DATE );
    /**  @} */

    /** @brief Generic structure used for passing keys and data in and out
     * of the database.
     *
     * Values returned from the database are valid only until a subsequent
     * update operation, or the end of the transaction. Do not modify or
     * free them, they commonly point into the database itself.
     *
     * Key sizes must be between 1 and #mdb_env_get_maxkeysize() inclusive.
     * The same applies to data sizes in databases with the #MDB_DUPSORT flag.
     * Other data items can in theory be from 0 to 0xffffffff bytes long.
     */
    public class MDB_val
    {
      public size_t mv_size; /**< size of the data item */
      public Object mv_data;  /**< address of the data item */
    }

    /** @brief A callback function used to compare two keys in a database */
    public delegate Int32 MDB_cmp_func( MDB_val a, MDB_val b );

    /** @brief A callback function used to relocate a position-dependent data item
     * in a fixed-address database.
     *
     * The \b newptr gives the item's desired address in
     * the memory map, and \b oldptr gives its previous address. The item's actual
     * data resides at the address in \b item.  This callback is expected to walk
     * through the fields of the record in \b item and modify any
     * values based at the \b oldptr address to be relative to the \b newptr address.
     * @param[in,out] item The item that is to be relocated.
     * @param[in] oldptr The previous address.
     * @param[in] newptr The new address to relocate to.
     * @param[in] relctx An application-provided context, set by #mdb_set_relctx().
     * @todo This feature is currently unimplemented.
     */
    public delegate void MDB_rel_func( MDB_val item, Object oldptr, Object newptr, Object relctx );

    /** @defgroup  mdb_env  Environment Flags
     *  @{
     */
    /** mmap at a fixed address (experimental) */
    public const Int32 MDB_FIXEDMAP = 0x01;
    /** no environment directory */
    public const Int32 MDB_NOSUBDIR = 0x4000;
    /** don't fsync after commit */
    public const Int32 MDB_NOSYNC = 0x10000;
    /** read only */
    public const Int32 MDB_RDONLY = 0x20000;
    /** don't fsync metapage after commit */
    public const Int32 MDB_NOMETASYNC = 0x40000;
    /** use writable mmap */
    public const Int32 MDB_WRITEMAP = 0x80000;
    /** use asynchronous msync when #MDB_WRITEMAP is used */
    public const Int32 MDB_MAPASYNC = 0x100000;
    /** tie reader locktable slots to #MDB_txn objects instead of to threads */
    public const Int32 MDB_NOTLS = 0x200000;
    /** don't do any locking, caller must manage their own locks */
    public const Int32 MDB_NOLOCK = 0x400000;
    /** don't do readahead (no effect on Windows) */
    public const Int32 MDB_NORDAHEAD = 0x800000;
    /** don't initialize malloc'd memory before writing to datafile */
    public const Int32 MDB_NOMEMINIT = 0x1000000;
    /** use the previous snapshot rather than the latest one */
    public const Int32 MDB_PREVSNAPSHOT = 0x2000000;
    /** @} */

    /**  @defgroup  mdb_dbi_open  Database Flags
     *  @{
     */
    /** use reverse string keys */
    public const Int32 MDB_REVERSEKEY = 0x02;
    /** use sorted duplicates */
    public const Int32 MDB_DUPSORT = 0x04;
    /** numeric keys in native byte order, either System.UInt32 or #mdb_size_t.
     *  (lmdb expects 32-bit int <= size_t <= 32/64-bit mdb_size_t.)
     *  The keys must all be of the same size. */
    public const Int32 MDB_INTEGERKEY = 0x08;
    /** with #MDB_DUPSORT, sorted dup items have fixed size */
    public const Int32 MDB_DUPFIXED = 0x10;
    /** with #MDB_DUPSORT, dups are #MDB_INTEGERKEY-style integers */
    public const Int32 MDB_INTEGERDUP = 0x20;
    /** with #MDB_DUPSORT, use reverse string dups */
    public const Int32 MDB_REVERSEDUP = 0x40;
    /** create DB if not already existing */
    public const Int32 MDB_CREATE = 0x40000;
    /** @} */

    /**  @defgroup mdb_put  Write Flags
     *  @{
     */
    /** For put: Don't write if the key already exists. */
    public const Int32 MDB_NOOVERWRITE = 0x10;
    /** Only for #MDB_DUPSORT<br>
     * For put: don't write if the key and data pair already exist.<br>
     * For mdb_cursor_del: remove all duplicate data items.
     */
    public const Int32 MDB_NODUPDATA = 0x20;
    /** For mdb_cursor_put: overwrite the current key/data pair */
    public const Int32 MDB_CURRENT = 0x40;
    /** For put: Just reserve space for data, don't copy it. Return a
     * pointer to the reserved space.
     */
    public const Int32 MDB_RESERVE = 0x10000;
    /** Data is being appended, don't split full pages. */
    public const Int32 MDB_APPEND = 0x20000;
    /** Duplicate data is being appended, don't split full pages. */
    public const Int32 MDB_APPENDDUP = 0x40000;
    /** Store multiple data items in one call. Only for #MDB_DUPFIXED. */
    public const Int32 MDB_MULTIPLE = 0x80000;
    /*  @} */

    /**  @defgroup mdb_copy  Copy Flags
     *  @{
     */
    /** Compacting copy: Omit free space from copy, and renumber all
     * pages sequentially.
     */
    public const Int32 MDB_CP_COMPACT = 0x01;
    /*  @} */

    /** @brief Cursor Get operations.
     *
     *  This is the set of all operations for retrieving data
     *  using a cursor.
     */
    public enum MDB_cursor_op
    {
      MDB_FIRST,        /**< Position at first key/data item */
      MDB_FIRST_DUP,      /**< Position at first data item of current key.
                Only for #MDB_DUPSORT */
      MDB_GET_BOTH,     /**< Position at key/data pair. Only for #MDB_DUPSORT */
      MDB_GET_BOTH_RANGE,   /**< position at key, nearest data. Only for #MDB_DUPSORT */
      MDB_GET_CURRENT,    /**< Return key/data at current cursor position */
      MDB_GET_MULTIPLE,   /**< Return up to a page of duplicate data items
                from current cursor position. Move cursor to prepare
                for #MDB_NEXT_MULTIPLE. Only for #MDB_DUPFIXED */
      MDB_LAST,       /**< Position at last key/data item */
      MDB_LAST_DUP,     /**< Position at last data item of current key.
                Only for #MDB_DUPSORT */
      MDB_NEXT,       /**< Position at next data item */
      MDB_NEXT_DUP,     /**< Position at next data item of current key.
                Only for #MDB_DUPSORT */
      MDB_NEXT_MULTIPLE,    /**< Return up to a page of duplicate data items
                from next cursor position. Move cursor to prepare
                for #MDB_NEXT_MULTIPLE. Only for #MDB_DUPFIXED */
      MDB_NEXT_NODUP,     /**< Position at first data item of next key */
      MDB_PREV,       /**< Position at previous data item */
      MDB_PREV_DUP,     /**< Position at previous data item of current key.
                Only for #MDB_DUPSORT */
      MDB_PREV_NODUP,     /**< Position at last data item of previous key */
      MDB_SET,        /**< Position at specified key */
      MDB_SET_KEY,      /**< Position at specified key, return key + data */
      MDB_SET_RANGE,      /**< Position at first key greater than or equal to specified key. */
      MDB_PREV_MULTIPLE   /**< Position at previous page and return up to
                a page of duplicate data items. Only for #MDB_DUPFIXED */
    }

    /** @defgroup  errors  Return Codes
     *
     *  BerkeleyDB uses -30800 to -30999, we'll go under them
     *  @{
     */
    /**  Successful result */
    public const Int32 MDB_SUCCESS = 0;
    /** key/data pair already exists */
    public const Int32 MDB_KEYEXIST = -30799;
    /** key/data pair not found (EOF) */
    public const Int32 MDB_NOTFOUND = -30798;
    /** Requested page not found - this usually indicates corruption */
    public const Int32 MDB_PAGE_NOTFOUND = -30797;
    /** Located page was wrong type */
    public const Int32 MDB_CORRUPTED = -30796;
    /** Update of meta page failed or environment had fatal error */
    public const Int32 MDB_PANIC = -30795;
    /** Environment version mismatch */
    public const Int32 MDB_VERSION_MISMATCH = -30794;
    /** File is not a valid LMDB file */
    public const Int32 MDB_INVALID = -30793;
    /** Environment mapsize reached */
    public const Int32 MDB_MAP_FULL = -30792;
    /** Environment maxdbs reached */
    public const Int32 MDB_DBS_FULL = -30791;
    /** Environment maxreaders reached */
    public const Int32 MDB_READERS_FULL = -30790;
    /** Too many TLS keys in use - Windows only */
    public const Int32 MDB_TLS_FULL = -30789;
    /** Txn has too many dirty pages */
    public const Int32 MDB_TXN_FULL = -30788;
    /** Cursor stack too deep - internal error */
    public const Int32 MDB_CURSOR_FULL = -30787;
    /** Page has not enough space - internal error */
    public const Int32 MDB_PAGE_FULL = -30786;
    /** Database contents grew beyond environment mapsize */
    public const Int32 MDB_MAP_RESIZED = -30785;
    /** Operation and DB incompatible, or DB type changed. This can mean:
     *  <ul>
     *  <li>The operation expects an #MDB_DUPSORT / #MDB_DUPFIXED database.
     *  <li>Opening a named DB when the unnamed DB has #MDB_DUPSORT / #MDB_INTEGERKEY.
     *  <li>Accessing a data record as a database, or vice versa.
     *  <li>The database was dropped and recreated with different flags.
     *  </ul>
     */
    public const Int32 MDB_INCOMPATIBLE = -30784;
    /** Invalid reuse of reader locktable slot */
    public const Int32 MDB_BAD_RSLOT = -30783;
    /** Transaction must abort, has a child, or is invalid */
    public const Int32 MDB_BAD_TXN = -30782;
    /** Unsupported size of key/DB name/data, or wrong DUPFIXED size */
    public const Int32 MDB_BAD_VALSIZE = -30781;
    /** The specified DBI was changed unexpectedly */
    public const Int32 MDB_BAD_DBI = -30780;
    /** Unexpected problem - txn should abort */
    public const Int32 MDB_PROBLEM = -30779;
    /** The last defined error code */
    public const Int32 MDB_LAST_ERRCODE = MDB_PROBLEM;
    /** @} */

    /** @brief Statistics for a database in the environment */
    public struct MDB_stat
    {
      UInt32 ms_psize;      /**< Size of a database page.
                      This is currently the same for all databases. */
      UInt32 ms_depth;      /**< Depth (height) of the B-tree */
      mdb_size_t ms_branch_pages; /**< Number of internal (non-leaf) pages */
      mdb_size_t ms_leaf_pages;   /**< Number of leaf pages */
      mdb_size_t ms_overflow_pages; /**< Number of overflow pages */
      mdb_size_t ms_entries;      /**< Number of data items */
    }

    /** @brief Information about the environment */
    public struct MDB_envinfo
    {
      Object me_mapaddr;     /**< Address of map, if fixed */
      mdb_size_t me_mapsize;        /**< Size of the data memory map */
      mdb_size_t me_last_pgno;      /**< ID of the last used page */
      mdb_size_t me_last_txnid;     /**< ID of the last committed transaction */
      UInt32 me_maxreaders;   /**< max reader slots in the environment */
      UInt32 me_numreaders;   /**< max reader slots used in the environment */
    }

    /** @brief Return the LMDB library version information.
     *
     * @param[out] major if non-NULL, the library major version number is copied here
     * @param[out] minor if non-NULL, the library minor version number is copied here
     * @param[out] patch if non-NULL, the library patch version number is copied here
     * @retval "version string" The library version as a string
     */
    public static String mdb_version( Int32 major, Int32 minor, Int32 patch ) => throw new NotImplementedException();

    /** @brief Create an LMDB environment handle.
     *
     * This function allocates memory for a #MDB_env structure. To release
     * the allocated memory and discard the handle, call #mdb_env_close().
     * Before the handle may be used, it must be opened using #mdb_env_open().
     * Various other options may also need to be set before opening the handle,
     * e.g. #mdb_env_set_mapsize(), #mdb_env_set_maxreaders(), #mdb_env_set_maxdbs(),
     * depending on usage requirements.
     * @param[out] env The address where the new handle will be stored
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_create( ref MDB_env env ) => throw new NotImplementedException();

    /** @brief Open an environment handle.
     *
     * If this function fails, #mdb_env_close() must be called to discard the #MDB_env handle.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] path The directory in which the database files reside. This
     * directory must already exist and be writable.
     * @param[in] flags Special options for this environment. This parameter
     * must be set to 0 or by bitwise OR'ing together one or more of the
     * values described here.
     * Flags set by mdb_env_set_flags() are also used.
     * <ul>
     *  <li>#MDB_FIXEDMAP
     *      use a fixed address for the mmap region. This flag must be specified
     *      when creating the environment, and is stored persistently in the environment.
     *    If successful, the memory map will always reside at the same virtual address
     *    and pointers used to reference data items in the database will be constant
     *    across multiple invocations. This option may not always work, depending on
     *    how the operating system has allocated memory to shared libraries and other uses.
     *    The feature is highly experimental.
     *  <li>#MDB_NOSUBDIR
     *    By default, LMDB creates its environment in a directory whose
     *    pathname is given in \b path, and creates its data and lock files
     *    under that directory. With this option, \b path is used as-is for
     *    the database main data file. The database lock file is the \b path
     *    with "-lock" appended.
     *  <li>#MDB_RDONLY
     *    Open the environment in read-only mode. No write operations will be
     *    allowed. LMDB will still modify the lock file - except on read-only
     *    filesystems, where LMDB does not use locks.
     *  <li>#MDB_WRITEMAP
     *    Use a writeable memory map unless MDB_RDONLY is set. This uses
     *    fewer mallocs but loses protection from application bugs
     *    like wild pointer writes and other bad updates into the database.
     *    This may be slightly faster for DBs that fit entirely in RAM, but
     *    is slower for DBs larger than RAM.
     *    Incompatible with nested transactions.
     *    Do not mix processes with and without MDB_WRITEMAP on the same
     *    environment.  This can defeat durability (#mdb_env_sync etc).
     *  <li>#MDB_NOMETASYNC
     *    Flush system buffers to disk only once per transaction, omit the
     *    metadata flush. Defer that until the system flushes files to disk,
     *    or next non-MDB_RDONLY commit or #mdb_env_sync(). This optimization
     *    maintains database integrity, but a system crash may undo the last
     *    committed transaction. I.e. it preserves the ACI (atomicity,
     *    consistency, isolation) but not D (durability) database property.
     *    This flag may be changed at any time using #mdb_env_set_flags().
     *  <li>#MDB_NOSYNC
     *    Don't flush system buffers to disk when committing a transaction.
     *    This optimization means a system crash can corrupt the database or
     *    lose the last transactions if buffers are not yet flushed to disk.
     *    The risk is governed by how often the system flushes dirty buffers
     *    to disk and how often #mdb_env_sync() is called.  However, if the
     *    filesystem preserves write order and the #MDB_WRITEMAP flag is not
     *    used, transactions exhibit ACI (atomicity, consistency, isolation)
     *    properties and only lose D (durability).  I.e. database integrity
     *    is maintained, but a system crash may undo the final transactions.
     *    Note that (#MDB_NOSYNC | #MDB_WRITEMAP) leaves the system with no
     *    hint for when to write transactions to disk, unless #mdb_env_sync()
     *    is called. (#MDB_MAPASYNC | #MDB_WRITEMAP) may be preferable.
     *    This flag may be changed at any time using #mdb_env_set_flags().
     *  <li>#MDB_MAPASYNC
     *    When using #MDB_WRITEMAP, use asynchronous flushes to disk.
     *    As with #MDB_NOSYNC, a system crash can then corrupt the
     *    database or lose the last transactions. Calling #mdb_env_sync()
     *    ensures on-disk database integrity until next commit.
     *    This flag may be changed at any time using #mdb_env_set_flags().
     *  <li>#MDB_NOTLS
     *    Don't use Thread-Local Storage. Tie reader locktable slots to
     *    #MDB_txn objects instead of to threads. I.e. #mdb_txn_reset() keeps
     *    the slot reseved for the #MDB_txn object. A thread may use parallel
     *    read-only transactions. A read-only transaction may span threads if
     *    the user synchronizes its use. Applications that multiplex many
     *    user threads over individual OS threads need this option. Such an
     *    application must also serialize the write transactions in an OS
     *    thread, since LMDB's write locking is unaware of the user threads.
     *  <li>#MDB_NOLOCK
     *    Don't do any locking. If concurrent access is anticipated, the
     *    caller must manage all concurrency itself. For proper operation
     *    the caller must enforce single-writer semantics, and must ensure
     *    that no readers are using old transactions while a writer is
     *    active. The simplest approach is to use an exclusive lock so that
     *    no readers may be active at all when a writer begins.
     *  <li>#MDB_NORDAHEAD
     *    Turn off readahead. Most operating systems perform readahead on
     *    read requests by default. This option turns it off if the OS
     *    supports it. Turning it off may help random read performance
     *    when the DB is larger than RAM and system RAM is full.
     *    The option is not implemented on Windows.
     *  <li>#MDB_NOMEMINIT
     *    Don't initialize malloc'd memory before writing to unused spaces
     *    in the data file. By default, memory for pages written to the data
     *    file is obtained using malloc. While these pages may be reused in
     *    subsequent transactions, freshly malloc'd pages will be initialized
     *    to zeroes before use. This avoids persisting leftover data from other
     *    code (that used the heap and subsequently freed the memory) into the
     *    data file. Note that many other system libraries may allocate
     *    and free memory from the heap for arbitrary uses. E.g., stdio may
     *    use the heap for file I/O buffers. This initialization step has a
     *    modest performance cost so some applications may want to disable
     *    it using this flag. This option can be a problem for applications
     *    which handle sensitive data like passwords, and it makes memory
     *    checkers like Valgrind noisy. This flag is not needed with #MDB_WRITEMAP,
     *    which writes directly to the mmap instead of using malloc for pages. The
     *    initialization is also skipped if #MDB_RESERVE is used; the
     *    caller is expected to overwrite all of the memory that was
     *    reserved in that case.
     *    This flag may be changed at any time using #mdb_env_set_flags().
     *  <li>#MDB_PREVSNAPSHOT
     *    Open the environment with the previous snapshot rather than the latest
     *    one. This loses the latest transaction, but may help work around some
     *    types of corruption. If opened with write access, this must be the
     *    only process using the environment. This flag is automatically reset
     *    after a write transaction is successfully committed.
     * </ul>
     * @param[in] mode The UNIX permissions to set on created files and semaphores.
     * This parameter is ignored on Windows.
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_VERSION_MISMATCH - the version of the LMDB library doesn't match the
     *  version that created the database environment.
     *  <li>#MDB_INVALID - the environment file headers are corrupted.
     *  <li>ENOENT - the directory specified by the path parameter doesn't exist.
     *  <li>EACCES - the user didn't have permission to access the environment files.
     *  <li>EAGAIN - the environment was locked by another process.
     * </ul>
     */
    public static Int32 mdb_env_open( MDB_env env, String path, UInt32 flags, mdb_mode_t mode ) => throw new NotImplementedException();

    /** @brief Copy an LMDB environment to the specified path.
     *
     * This function may be used to make a backup of an existing environment.
     * No lockfile is created, since it gets recreated at need.
     * @note This call can trigger significant file size growth if run in
     * parallel with write transactions, because it employs a read-only
     * transaction. See long-lived transactions under @ref caveats_sec.
     * @param[in] env An environment handle returned by #mdb_env_create(). It
     * must have already been opened successfully.
     * @param[in] path The directory in which the copy will reside. This
     * directory must already exist and be writable but must otherwise be
     * empty.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_copy( MDB_env env, String path ) => throw new NotImplementedException();

    /** @brief Copy an LMDB environment to the specified file descriptor.
     *
     * This function may be used to make a backup of an existing environment.
     * No lockfile is created, since it gets recreated at need.
     * @note This call can trigger significant file size growth if run in
     * parallel with write transactions, because it employs a read-only
     * transaction. See long-lived transactions under @ref caveats_sec.
     * @param[in] env An environment handle returned by #mdb_env_create(). It
     * must have already been opened successfully.
     * @param[in] fd The filedescriptor to write the copy to. It must
     * have already been opened for Write access.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_copyfd( MDB_env env, mdb_filehandle_t fd ) => throw new NotImplementedException();

    /** @brief Copy an LMDB environment to the specified path, with options.
     *
     * This function may be used to make a backup of an existing environment.
     * No lockfile is created, since it gets recreated at need.
     * @note This call can trigger significant file size growth if run in
     * parallel with write transactions, because it employs a read-only
     * transaction. See long-lived transactions under @ref caveats_sec.
     * @param[in] env An environment handle returned by #mdb_env_create(). It
     * must have already been opened successfully.
     * @param[in] path The directory in which the copy will reside. This
     * directory must already exist and be writable but must otherwise be
     * empty.
     * @param[in] flags Special options for this operation. This parameter
     * must be set to 0 or by bitwise OR'ing together one or more of the
     * values described here.
     * <ul>
     *  <li>#MDB_CP_COMPACT - Perform compaction while copying: omit free
     *    pages and sequentially renumber all pages in output. This option
     *    consumes more CPU and runs more slowly than the default.
     *    Currently it fails if the environment has suffered a page leak.
     * </ul>
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_copy2( MDB_env env, String path, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Copy an LMDB environment to the specified file descriptor,
     *  with options.
     *
     * This function may be used to make a backup of an existing environment.
     * No lockfile is created, since it gets recreated at need. See
     * #mdb_env_copy2() for further details.
     * @note This call can trigger significant file size growth if run in
     * parallel with write transactions, because it employs a read-only
     * transaction. See long-lived transactions under @ref caveats_sec.
     * @param[in] env An environment handle returned by #mdb_env_create(). It
     * must have already been opened successfully.
     * @param[in] fd The filedescriptor to write the copy to. It must
     * have already been opened for Write access.
     * @param[in] flags Special options for this operation.
     * See #mdb_env_copy2() for options.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_copyfd2( MDB_env env, mdb_filehandle_t fd, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Return statistics about the LMDB environment.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] stat The address of an #MDB_stat structure
     *   where the statistics will be copied
     */
    public static Int32 mdb_env_stat( MDB_env env, MDB_stat stat ) => throw new NotImplementedException();

    /** @brief Return information about the LMDB environment.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] stat The address of an #MDB_envinfo structure
     *   where the information will be copied
     */
    public static Int32 mdb_env_info( MDB_env env, MDB_envinfo stat ) => throw new NotImplementedException();

    /** @brief Flush the data buffers to disk.
     *
     * Data is always written to disk when #mdb_txn_commit() is called,
     * but the operating system may keep it buffered. LMDB always flushes
     * the OS buffers upon commit as well, unless the environment was
     * opened with #MDB_NOSYNC or in part #MDB_NOMETASYNC. This call is
     * not valid if the environment was opened with #MDB_RDONLY.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] force If non-zero, force a synchronous flush.  Otherwise
     *  if the environment has the #MDB_NOSYNC flag set the flushes
     *  will be omitted, and with #MDB_MAPASYNC they will be asynchronous.
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EACCES - the environment is read-only.
     *  <li>EINVAL - an invalid parameter was specified.
     *  <li>EIO - an error occurred during synchronization.
     * </ul>
     */
    public static Int32 mdb_env_sync( MDB_env env, Int32 force ) => throw new NotImplementedException();

    /** @brief Close the environment and release the memory map.
     *
     * Only a single thread may call this function. All transactions, databases,
     * and cursors must already be closed before calling this function. Attempts to
     * use any such handles after calling this function will cause a SIGSEGV.
     * The environment handle will be freed and must not be used again after this call.
     * @param[in] env An environment handle returned by #mdb_env_create()
     */
    public static void mdb_env_close( MDB_env env ) => throw new NotImplementedException();

    /** @brief Set environment flags.
     *
     * This may be used to set some flags in addition to those from
     * #mdb_env_open(), or to unset these flags.  If several threads
     * change the flags at the same time, the result is undefined.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] flags The flags to change, bitwise OR'ed together
     * @param[in] onoff A non-zero value sets the flags, zero clears them.
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_env_set_flags( MDB_env env, UInt32 flags, Int32 onoff ) => throw new NotImplementedException();

    /** @brief Get environment flags.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] flags The address of an integer to store the flags
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_env_get_flags( MDB_env env, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Return the path that was used in #mdb_env_open().
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] path Address of a string pointer to contain the path. This
     * is the actual string in the environment, not a copy. It should not be
     * altered in any way.
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_env_get_path( MDB_env env, out String path ) => throw new NotImplementedException();

    /** @brief Return the filedescriptor for the given environment.
     *
     * This function may be called after fork(), so the descriptor can be
     * closed before exec*().  Other LMDB file descriptors have FD_CLOEXEC.
     * (Until LMDB 0.9.18, only the lockfile had that.)
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] fd Address of a mdb_filehandle_t to contain the descriptor.
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_env_get_fd( MDB_env env, mdb_filehandle_t fd ) => throw new NotImplementedException();

    /** @brief Set the size of the memory map to use for this environment.
     *
     * The size should be a multiple of the OS page size. The default is
     * 10485760 bytes. The size of the memory map is also the maximum size
     * of the database. The value should be chosen as large as possible,
     * to accommodate future growth of the database.
     * This function should be called after #mdb_env_create() and before #mdb_env_open().
     * It may be called at later times if no transactions are active in
     * this process. Note that the library does not check for this condition,
     * the caller must ensure it explicitly.
     *
     * The new size takes effect immediately for the current process but
     * will not be persisted to any others until a write transaction has been
     * committed by the current process. Also, only mapsize increases are
     * persisted into the environment.
     *
     * If the mapsize is increased by another process, and data has grown
     * beyond the range of the current mapsize, #mdb_txn_begin() will
     * return #MDB_MAP_RESIZED. This function may be called with a size
     * of zero to adopt the new size.
     *
     * Any attempt to set a size smaller than the space already consumed
     * by the environment will be silently changed to the current size of the used space.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] size The size in bytes
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified, or the environment has
     *     an active write transaction.
     * </ul>
     */
    public static Int32 mdb_env_set_mapsize( MDB_env env, mdb_size_t size ) => throw new NotImplementedException();

    /** @brief Set the maximum number of threads/reader slots for the environment.
     *
     * This defines the number of slots in the lock table that is used to track readers in the
     * the environment. The default is 126.
     * Starting a read-only transaction normally ties a lock table slot to the
     * current thread until the environment closes or the thread exits. If
     * MDB_NOTLS is in use, #mdb_txn_begin() instead ties the slot to the
     * MDB_txn object until it or the #MDB_env object is destroyed.
     * This function may only be called after #mdb_env_create() and before #mdb_env_open().
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] readers The maximum number of reader lock table slots
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified, or the environment is already open.
     * </ul>
     */
    public static Int32 mdb_env_set_maxreaders( MDB_env env, UInt32 readers ) => throw new NotImplementedException();

    /** @brief Get the maximum number of threads/reader slots for the environment.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] readers Address of an integer to store the number of readers
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_env_get_maxreaders( MDB_env env, UInt32 readers ) => throw new NotImplementedException();

    /** @brief Set the maximum number of named databases for the environment.
     *
     * This function is only needed if multiple databases will be used in the
     * environment. Simpler applications that use the environment as a single
     * unnamed database can ignore this option.
     * This function may only be called after #mdb_env_create() and before #mdb_env_open().
     *
     * Currently a moderate number of slots are cheap but a huge number gets
     * expensive: 7-120 words per transaction, and every #mdb_dbi_open()
     * does a linear search of the opened slots.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] dbs The maximum number of databases
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified, or the environment is already open.
     * </ul>
     */
    public static Int32 mdb_env_set_maxdbs( MDB_env env, MDB_dbi dbs ) => throw new NotImplementedException();

    /** @brief Get the maximum size of keys and #MDB_DUPSORT data we can write.
     *
     * Depends on the compile-time constant #MDB_MAXKEYSIZE. Default 511.
     * See @ref MDB_val.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @return The maximum size of a key we can write
     */
    public static Int32 mdb_env_get_maxkeysize( MDB_env env ) => throw new NotImplementedException();

    /** @brief Set application information associated with the #MDB_env.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] ctx An arbitrary pointer for whatever the application needs.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_set_userctx( MDB_env env, Object ctx ) => throw new NotImplementedException();

    /** @brief Get the application information associated with the #MDB_env.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @return The pointer set by #mdb_env_set_userctx().
     */
    public static Object mdb_env_get_userctx( MDB_env env ) => throw new NotImplementedException();

    /** @brief A callback function for most LMDB assert() failures,
     * called before printing the message and aborting.
     *
     * @param[in] env An environment handle returned by #mdb_env_create().
     * @param[in] msg The assertion message, not including newline.
     */
    public delegate void MDB_assert_func( MDB_env env, String msg );

    /** Set or reset the assert() callback of the environment.
     * Disabled if liblmdb is buillt with NDEBUG.
     * @note This hack should become obsolete as lmdb's error handling matures.
     * @param[in] env An environment handle returned by #mdb_env_create().
     * @param[in] func An #MDB_assert_func function, or 0.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_env_set_assert( MDB_env env, MDB_assert_func func ) => throw new NotImplementedException();

    /** @brief Create a transaction for use with the environment.
     *
     * The transaction handle may be discarded using #mdb_txn_abort() or #mdb_txn_commit().
     * @note A transaction and its cursors must only be used by a single
     * thread, and a thread may only have a single transaction at a time.
     * If #MDB_NOTLS is in use, this does not apply to read-only transactions.
     * @note Cursors may not span transactions.
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] parent If this parameter is non-NULL, the new transaction
     * will be a nested transaction, with the transaction indicated by \b parent
     * as its parent. Transactions may be nested to any level. A parent
     * transaction and its cursors may not issue any other operations than
     * mdb_txn_commit and mdb_txn_abort while it has active child transactions.
     * @param[in] flags Special options for this transaction. This parameter
     * must be set to 0 or by bitwise OR'ing together one or more of the
     * values described here.
     * <ul>
     *  <li>#MDB_RDONLY
     *    This transaction will not perform any write operations.
     *  <li>#MDB_NOSYNC
     *    Don't flush system buffers to disk when committing this transaction.
     *  <li>#MDB_NOMETASYNC
     *    Flush system buffers but omit metadata flush when committing this transaction.
     * </ul>
     * @param[out] txn Address where the new #MDB_txn handle will be stored
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_PANIC - a fatal error occurred earlier and the environment
     *    must be shut down.
     *  <li>#MDB_MAP_RESIZED - another process wrote data beyond this MDB_env's
     *    mapsize and this environment's map must be resized as well.
     *    See #mdb_env_set_mapsize().
     *  <li>#MDB_READERS_FULL - a read-only transaction was requested and
     *    the reader lock table is full. See #mdb_env_set_maxreaders().
     *  <li>ENOMEM - out of memory.
     * </ul>
     */
    public static Int32 mdb_txn_begin( MDB_env env, MDB_txn parent, UInt32 flags, out MDB_txn txn ) => throw new NotImplementedException();

    /** @brief Returns the transaction's #MDB_env
     *
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     */
    public static MDB_env mdb_txn_env( MDB_txn txn ) => throw new NotImplementedException();

    /** @brief Return the transaction's ID.
     *
     * This returns the identifier associated with this transaction. For a
     * read-only transaction, this corresponds to the snapshot being read;
     * concurrent readers will frequently have the same transaction ID.
     *
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @return A transaction ID, valid if input is an active transaction.
     */
    public static mdb_size_t mdb_txn_id( MDB_txn txn ) => throw new NotImplementedException();

    /** @brief Commit all the operations of a transaction into the database.
     *
     * The transaction handle is freed. It and its cursors must not be used
     * again after this call, except with #mdb_cursor_renew().
     * @note Earlier documentation incorrectly said all cursors would be freed.
     * Only write-transactions free cursors.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     *  <li>ENOSPC - no more disk space.
     *  <li>EIO - a low-level I/O error occurred while writing.
     *  <li>ENOMEM - out of memory.
     * </ul>
     */
    public static Int32 mdb_txn_commit( MDB_txn txn ) => throw new NotImplementedException();

    /** @brief Abandon all the operations of the transaction instead of saving them.
     *
     * The transaction handle is freed. It and its cursors must not be used
     * again after this call, except with #mdb_cursor_renew().
     * @note Earlier documentation incorrectly said all cursors would be freed.
     * Only write-transactions free cursors.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     */
    public static void mdb_txn_abort( MDB_txn txn ) => throw new NotImplementedException();

    /** @brief Reset a read-only transaction.
     *
     * Abort the transaction like #mdb_txn_abort(), but keep the transaction
     * handle. #mdb_txn_renew() may reuse the handle. This saves allocation
     * overhead if the process will start a new read-only transaction soon,
     * and also locking overhead if #MDB_NOTLS is in use. The reader table
     * lock is released, but the table slot stays tied to its thread or
     * #MDB_txn. Use mdb_txn_abort() to discard a reset handle, and to free
     * its lock table slot if MDB_NOTLS is in use.
     * Cursors opened within the transaction must not be used
     * again after this call, except with #mdb_cursor_renew().
     * Reader locks generally don't interfere with writers, but they keep old
     * versions of database pages allocated. Thus they prevent the old pages
     * from being reused when writers commit new data, and so under heavy load
     * the database size may grow much more rapidly than otherwise.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     */
    public static void mdb_txn_reset( MDB_txn txn ) => throw new NotImplementedException();

    /** @brief Renew a read-only transaction.
     *
     * This acquires a new reader lock for a transaction handle that had been
     * released by #mdb_txn_reset(). It must be called before a reset transaction
     * may be used again.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_PANIC - a fatal error occurred earlier and the environment
     *    must be shut down.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_txn_renew( MDB_txn txn ) => throw new NotImplementedException();

    /** Compat with version <= 0.9.4, avoid clash with libmdb from MDB Tools project */
    [Obsolete( "Use mdb_dbi_open instead" )]
    public static Int32 mdb_open( MDB_txn txn, String name, UInt32 flags, MDB_dbi dbi ) => mdb_dbi_open( txn, name, flags, dbi );
    /** Compat with version <= 0.9.4, avoid clash with libmdb from MDB Tools project */
    [Obsolete( "Use mdb_close instead" )]
    public static void mdb_close( MDB_env env, MDB_dbi dbi ) => mdb_dbi_close( env, dbi );

    /** @brief Open a database in the environment.
     *
     * A database handle denotes the name and parameters of a database,
     * independently of whether such a database exists.
     * The database handle may be discarded by calling #mdb_dbi_close().
     * The old database handle is returned if the database was already open.
     * The handle may only be closed once.
     *
     * The database handle will be private to the current transaction until
     * the transaction is successfully committed. If the transaction is
     * aborted the handle will be closed automatically.
     * After a successful commit the handle will reside in the shared
     * environment, and may be used by other transactions.
     *
     * This function must not be called from multiple concurrent
     * transactions in the same process. A transaction that uses
     * this function must finish (either commit or abort) before
     * any other transaction in the process may use this function.
     *
     * To use named databases (with name != NULL), #mdb_env_set_maxdbs()
     * must be called before opening the environment.  Database names are
     * keys in the unnamed database, and may be read but not written.
     *
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] name The name of the database to open. If only a single
     *   database is needed in the environment, this value may be NULL.
     * @param[in] flags Special options for this database. This parameter
     * must be set to 0 or by bitwise OR'ing together one or more of the
     * values described here.
     * <ul>
     *  <li>#MDB_REVERSEKEY
     *    Keys are strings to be compared in reverse order, from the end
     *    of the strings to the beginning. By default, Keys are treated as strings and
     *    compared from beginning to end.
     *  <li>#MDB_DUPSORT
     *    Duplicate keys may be used in the database. (Or, from another perspective,
     *    keys may have multiple data items, stored in sorted order.) By default
     *    keys must be unique and may have only a single data item.
     *  <li>#MDB_INTEGERKEY
     *    Keys are binary integers in native byte order, either unsigned int
     *    or #mdb_size_t, and will be sorted as such.
     *    (lmdb expects 32-bit int <= size_t <= 32/64-bit mdb_size_t.)
     *    The keys must all be of the same size.
     *  <li>#MDB_DUPFIXED
     *    This flag may only be used in combination with #MDB_DUPSORT. This option
     *    tells the library that the data items for this database are all the same
     *    size, which allows further optimizations in storage and retrieval. When
     *    all data items are the same size, the #MDB_GET_MULTIPLE, #MDB_NEXT_MULTIPLE
     *    and #MDB_PREV_MULTIPLE cursor operations may be used to retrieve multiple
     *    items at once.
     *  <li>#MDB_INTEGERDUP
     *    This option specifies that duplicate data items are binary integers,
     *    similar to #MDB_INTEGERKEY keys.
     *  <li>#MDB_REVERSEDUP
     *    This option specifies that duplicate data items should be compared as
     *    strings in reverse order.
     *  <li>#MDB_CREATE
     *    Create the named database if it doesn't exist. This option is not
     *    allowed in a read-only transaction or a read-only environment.
     * </ul>
     * @param[out] dbi Address where the new #MDB_dbi handle will be stored
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_NOTFOUND - the specified database doesn't exist in the environment
     *    and #MDB_CREATE was not specified.
     *  <li>#MDB_DBS_FULL - too many databases have been opened. See #mdb_env_set_maxdbs().
     * </ul>
     */
    public static Int32 mdb_dbi_open( MDB_txn txn, String name, UInt32 flags, MDB_dbi dbi ) => throw new NotImplementedException();

    /** @brief Retrieve statistics for a database.
     *
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[out] stat The address of an #MDB_stat structure
     *   where the statistics will be copied
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_stat( MDB_txn txn, MDB_dbi dbi, MDB_stat stat ) => throw new NotImplementedException();

    /** @brief Retrieve the DB flags for a database handle.
     *
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[out] flags Address where the flags will be returned.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_dbi_flags( MDB_txn txn, MDB_dbi dbi, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Close a database handle. Normally unnecessary. Use with care:
     *
     * This call is not mutex protected. Handles should only be closed by
     * a single thread, and only if no other threads are going to reference
     * the database handle or one of its cursors any further. Do not close
     * a handle if an existing transaction has modified its database.
     * Doing so can cause misbehavior from database corruption to errors
     * like MDB_BAD_VALSIZE (since the DB name is gone).
     *
     * Closing a database handle is not necessary, but lets #mdb_dbi_open()
     * reuse the handle value.  Usually it's better to set a bigger
     * #mdb_env_set_maxdbs(), unless that value would be large.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     */
    public static void mdb_dbi_close( MDB_env env, MDB_dbi dbi ) => throw new NotImplementedException();

    /** @brief Empty or delete+close a database.
     *
     * See #mdb_dbi_close() for restrictions about closing the DB handle.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] del 0 to empty the DB, 1 to delete it from the
     * environment and close the DB handle.
     * @return A non-zero error value on failure and 0 on success.
     */
    public static Int32 mdb_drop( MDB_txn txn, MDB_dbi dbi, Int32 del ) => throw new NotImplementedException();

    /** @brief Set a custom key comparison function for a database.
     *
     * The comparison function is called whenever it is necessary to compare a
     * key specified by the application with a key currently stored in the database.
     * If no comparison function is specified, and no special key flags were specified
     * with #mdb_dbi_open(), the keys are compared lexically, with shorter keys collating
     * before longer keys.
     * @warning This function must be called before any data access functions are used,
     * otherwise data corruption may occur. The same comparison function must be used by every
     * program accessing the database, every time the database is used.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] cmp A #MDB_cmp_func function
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_set_compare( MDB_txn txn, MDB_dbi dbi, MDB_cmp_func cmp ) => throw new NotImplementedException();

    /** @brief Set a custom data comparison function for a #MDB_DUPSORT database.
     *
     * This comparison function is called whenever it is necessary to compare a data
     * item specified by the application with a data item currently stored in the database.
     * This function only takes effect if the database was opened with the #MDB_DUPSORT
     * flag.
     * If no comparison function is specified, and no special key flags were specified
     * with #mdb_dbi_open(), the data items are compared lexically, with shorter items collating
     * before longer items.
     * @warning This function must be called before any data access functions are used,
     * otherwise data corruption may occur. The same comparison function must be used by every
     * program accessing the database, every time the database is used.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] cmp A #MDB_cmp_func function
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_set_dupsort( MDB_txn txn, MDB_dbi dbi, MDB_cmp_func cmp ) => throw new NotImplementedException();

    /** @brief Set a relocation function for a #MDB_FIXEDMAP database.
     *
     * @todo The relocation function is called whenever it is necessary to move the data
     * of an item to a different position in the database (e.g. through tree
     * balancing operations, shifts as a result of adds or deletes, etc.). It is
     * intended to allow address/position-dependent data items to be stored in
     * a database in an environment opened with the #MDB_FIXEDMAP option.
     * Currently the relocation feature is unimplemented and setting
     * this function has no effect.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] rel A #MDB_rel_func function
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_set_relfunc( MDB_txn txn, MDB_dbi dbi, MDB_rel_func rel ) => throw new NotImplementedException();

    /** @brief Set a context pointer for a #MDB_FIXEDMAP database's relocation function.
     *
     * See #mdb_set_relfunc and #MDB_rel_func for more details.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] ctx An arbitrary pointer for whatever the application needs.
     * It will be passed to the callback function set by #mdb_set_relfunc
     * as its \b relctx parameter whenever the callback is invoked.
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_set_relctx( MDB_txn txn, MDB_dbi dbi, Object ctx ) => throw new NotImplementedException();

    /** @brief Get items from a database.
     *
     * This function retrieves key/data pairs from the database. The address
     * and length of the data associated with the specified \b key are returned
     * in the structure to which \b data refers.
     * If the database supports duplicate keys (#MDB_DUPSORT) then the
     * first data item for the key will be returned. Retrieval of other
     * items requires the use of #mdb_cursor_get().
     *
     * @note The memory pointed to by the returned values is owned by the
     * database. The caller need not dispose of the memory, and may not
     * modify it in any way. For values returned in a read-only transaction
     * any modification attempts will cause a SIGSEGV.
     * @note Values returned from the database are valid only until a
     * subsequent update operation, or the end of the transaction.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] key The key to search for in the database
     * @param[out] data The data corresponding to the key
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_NOTFOUND - the key was not in the database.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_get( MDB_txn txn, MDB_dbi dbi, MDB_val key, MDB_val data ) => throw new NotImplementedException();

    /** @brief Store items into a database.
     *
     * This function stores key/data pairs in the database. The default behavior
     * is to enter the new key/data pair, replacing any previously existing key
     * if duplicates are disallowed, or adding a duplicate data item if
     * duplicates are allowed (#MDB_DUPSORT).
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] key The key to store in the database
     * @param[in,out] data The data to store
     * @param[in] flags Special options for this operation. This parameter
     * must be set to 0 or by bitwise OR'ing together one or more of the
     * values described here.
     * <ul>
     *  <li>#MDB_NODUPDATA - enter the new key/data pair only if it does not
     *    already appear in the database. This flag may only be specified
     *    if the database was opened with #MDB_DUPSORT. The function will
     *    return #MDB_KEYEXIST if the key/data pair already appears in the
     *    database.
     *  <li>#MDB_NOOVERWRITE - enter the new key/data pair only if the key
     *    does not already appear in the database. The function will return
     *    #MDB_KEYEXIST if the key already appears in the database, even if
     *    the database supports duplicates (#MDB_DUPSORT). The \b data
     *    parameter will be set to point to the existing item.
     *  <li>#MDB_RESERVE - reserve space for data of the given size, but
     *    don't copy the given data. Instead, return a pointer to the
     *    reserved space, which the caller can fill in later - before
     *    the next update operation or the transaction ends. This saves
     *    an extra memcpy if the data is being generated later.
     *    LMDB does nothing else with this memory, the caller is expected
     *    to modify all of the space requested. This flag must not be
     *    specified if the database was opened with #MDB_DUPSORT.
     *  <li>#MDB_APPEND - append the given key/data pair to the end of the
     *    database. This option allows fast bulk loading when keys are
     *    already known to be in the correct order. Loading unsorted keys
     *    with this flag will cause a #MDB_KEYEXIST error.
     *  <li>#MDB_APPENDDUP - as above, but for sorted dup data.
     * </ul>
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_MAP_FULL - the database is full, see #mdb_env_set_mapsize().
     *  <li>#MDB_TXN_FULL - the transaction has too many dirty pages.
     *  <li>EACCES - an attempt was made to write in a read-only transaction.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_put( MDB_txn txn, MDB_dbi dbi, MDB_val key, MDB_val data, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Delete items from a database.
     *
     * This function removes key/data pairs from the database.
     * If the database does not support sorted duplicate data items
     * (#MDB_DUPSORT) the data parameter is ignored.
     * If the database supports sorted duplicates and the data parameter
     * is NULL, all of the duplicate data items for the key will be
     * deleted. Otherwise, if the data parameter is non-NULL
     * only the matching data item will be deleted.
     * This function will return #MDB_NOTFOUND if the specified key/data
     * pair is not in the database.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] key The key to delete from the database
     * @param[in] data The data to delete
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EACCES - an attempt was made to write in a read-only transaction.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_del( MDB_txn txn, MDB_dbi dbi, MDB_val key, MDB_val data ) => throw new NotImplementedException();

    /** @brief Create a cursor handle.
     *
     * A cursor is associated with a specific transaction and database.
     * A cursor cannot be used when its database handle is closed.  Nor
     * when its transaction has ended, except with #mdb_cursor_renew().
     * It can be discarded with #mdb_cursor_close().
     * A cursor in a write-transaction can be closed before its transaction
     * ends, and will otherwise be closed when its transaction ends.
     * A cursor in a read-only transaction must be closed explicitly, before
     * or after its transaction ends. It can be reused with
     * #mdb_cursor_renew() before finally closing it.
     * @note Earlier documentation said that cursors in every transaction
     * were closed when the transaction committed or aborted.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[out] cursor Address where the new #MDB_cursor handle will be stored
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_cursor_open( MDB_txn txn, MDB_dbi dbi, out MDB_cursor cursor ) => throw new NotImplementedException();

    /** @brief Close a cursor handle.
     *
     * The cursor handle will be freed and must not be used again after this call.
     * Its transaction must still be live if it is a write-transaction.
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     */
    public static void mdb_cursor_close( MDB_cursor cursor ) => throw new NotImplementedException();

    /** @brief Renew a cursor handle.
     *
     * A cursor is associated with a specific transaction and database.
     * Cursors that are only used in read-only
     * transactions may be re-used, to avoid unnecessary malloc/free overhead.
     * The cursor may be associated with a new read-only transaction, and
     * referencing the same database handle as it was created with.
     * This may be done whether the previous transaction is live or dead.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_cursor_renew( MDB_txn txn, MDB_cursor cursor ) => throw new NotImplementedException();

    /** @brief Return the cursor's transaction handle.
     *
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     */
    public static MDB_txn mdb_cursor_txn( MDB_cursor cursor ) => throw new NotImplementedException();

    /** @brief Return the cursor's database handle.
     *
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     */
    public static MDB_dbi mdb_cursor_dbi( MDB_cursor cursor ) => throw new NotImplementedException();

    /** @brief Retrieve by cursor.
     *
     * This function retrieves key/data pairs from the database. The address and length
     * of the key are returned in the object to which \b key refers (except for the
     * case of the #MDB_SET option, in which the \b key object is unchanged), and
     * the address and length of the data are returned in the object to which \b data
     * refers.
     * See #mdb_get() for restrictions on using the output values.
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     * @param[in,out] key The key for a retrieved item
     * @param[in,out] data The data of a retrieved item
     * @param[in] op A cursor operation #MDB_cursor_op
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_NOTFOUND - no matching key found.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_cursor_get( MDB_cursor cursor, MDB_val key, MDB_val data, MDB_cursor_op op ) => throw new NotImplementedException();

    /** @brief Store by cursor.
     *
     * This function stores key/data pairs into the database.
     * The cursor is positioned at the new item, or on failure usually near it.
     * @note Earlier documentation incorrectly said errors would leave the
     * state of the cursor unchanged.
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     * @param[in] key The key operated on.
     * @param[in] data The data operated on.
     * @param[in] flags Options for this operation. This parameter
     * must be set to 0 or one of the values described here.
     * <ul>
     *  <li>#MDB_CURRENT - replace the item at the current cursor position.
     *    The \b key parameter must still be provided, and must match it.
     *    If using sorted duplicates (#MDB_DUPSORT) the data item must still
     *    sort into the same place. This is intended to be used when the
     *    new data is the same size as the old. Otherwise it will simply
     *    perform a delete of the old record followed by an insert.
     *  <li>#MDB_NODUPDATA - enter the new key/data pair only if it does not
     *    already appear in the database. This flag may only be specified
     *    if the database was opened with #MDB_DUPSORT. The function will
     *    return #MDB_KEYEXIST if the key/data pair already appears in the
     *    database.
     *  <li>#MDB_NOOVERWRITE - enter the new key/data pair only if the key
     *    does not already appear in the database. The function will return
     *    #MDB_KEYEXIST if the key already appears in the database, even if
     *    the database supports duplicates (#MDB_DUPSORT).
     *  <li>#MDB_RESERVE - reserve space for data of the given size, but
     *    don't copy the given data. Instead, return a pointer to the
     *    reserved space, which the caller can fill in later - before
     *    the next update operation or the transaction ends. This saves
     *    an extra memcpy if the data is being generated later. This flag
     *    must not be specified if the database was opened with #MDB_DUPSORT.
     *  <li>#MDB_APPEND - append the given key/data pair to the end of the
     *    database. No key comparisons are performed. This option allows
     *    fast bulk loading when keys are already known to be in the
     *    correct order. Loading unsorted keys with this flag will cause
     *    a #MDB_KEYEXIST error.
     *  <li>#MDB_APPENDDUP - as above, but for sorted dup data.
     *  <li>#MDB_MULTIPLE - store multiple contiguous data elements in a
     *    single request. This flag may only be specified if the database
     *    was opened with #MDB_DUPFIXED. The \b data argument must be an
     *    array of two MDB_vals. The mv_size of the first MDB_val must be
     *    the size of a single data element. The mv_data of the first MDB_val
     *    must point to the beginning of the array of contiguous data elements.
     *    The mv_size of the second MDB_val must be the count of the number
     *    of data elements to store. On return this field will be set to
     *    the count of the number of elements actually written. The mv_data
     *    of the second MDB_val is unused.
     * </ul>
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>#MDB_MAP_FULL - the database is full, see #mdb_env_set_mapsize().
     *  <li>#MDB_TXN_FULL - the transaction has too many dirty pages.
     *  <li>EACCES - an attempt was made to write in a read-only transaction.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_cursor_put( MDB_cursor cursor, MDB_val key, MDB_val data, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Delete current key/data pair
     *
     * This function deletes the key/data pair to which the cursor refers.
     * This does not invalidate the cursor, so operations such as MDB_NEXT
     * can still be used on it.
     * Both MDB_NEXT and MDB_GET_CURRENT will return the same record after
     * this operation.
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     * @param[in] flags Options for this operation. This parameter
     * must be set to 0 or one of the values described here.
     * <ul>
     *  <li>#MDB_NODUPDATA - delete all of the data items for the current key.
     *    This flag may only be specified if the database was opened with #MDB_DUPSORT.
     * </ul>
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EACCES - an attempt was made to write in a read-only transaction.
     *  <li>EINVAL - an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_cursor_del( MDB_cursor cursor, UInt32 flags ) => throw new NotImplementedException();

    /** @brief Return count of duplicates for current key.
     *
     * This call is only valid on databases that support sorted duplicate
     * data items #MDB_DUPSORT.
     * @param[in] cursor A cursor handle returned by #mdb_cursor_open()
     * @param[out] countp Address where the count will be stored
     * @return A non-zero error value on failure and 0 on success. Some possible
     * errors are:
     * <ul>
     *  <li>EINVAL - cursor is not initialized, or an invalid parameter was specified.
     * </ul>
     */
    public static Int32 mdb_cursor_count( MDB_cursor cursor, mdb_size_t countp ) => throw new NotImplementedException();

    /** @brief A callback function used to print a message from the library.
     *
     * @param[in] msg The string to be printed.
     * @param[in] ctx An arbitrary context pointer for the callback.
     * @return < 0 on failure, >= 0 on success.
     */
    public delegate Int32 MDB_msg_func( String msg, Object ctx );

    /** @brief Dump the entries in the reader lock table.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[in] func A #MDB_msg_func function
     * @param[in] ctx Anything the message function needs
     * @return < 0 on failure, >= 0 on success.
     */
    public static Int32 mdb_reader_list( MDB_env env, MDB_msg_func func, Object ctx ) => throw new NotImplementedException();

    /** @brief Check for stale entries in the reader lock table.
     *
     * @param[in] env An environment handle returned by #mdb_env_create()
     * @param[out] dead Number of stale slots that were cleared
     * @return 0 on success, non-zero on failure.
     */
    public static Int32 mdb_reader_check( MDB_env env, Int32 dead ) => throw new NotImplementedException();
    /**  @} */

    /** @page tools LMDB Command Line Tools
      The following describes the command line tools that are available for LMDB.
      \li \ref mdb_copy_1
      \li \ref mdb_dump_1
      \li \ref mdb_load_1
      \li \ref mdb_stat_1
*/

    // from mdb.c

    /* We use native NT APIs to setup the memory map, so that we can
     * let the DB file grow incrementally instead of always preallocating
     * the full size. These APIs are defined in <wdm.h> and <ntifs.h>
     * but those headers are meant for driver-level development and
     * conflict with the regular user-level headers, so we explicitly
     * declare them here. We get pointers to these functions from
     * NTDLL.DLL at runtime, to avoid buildtime dependencies on any
     * NTDLL import libraries.
     */
    public delegate NTSTATUS NtCreateSectionFunc( out PHANDLE sh, ACCESS_MASK acc, Object oa, PLARGE_INTEGER ms, ULONG pp, ULONG aa, HANDLE fh );

    public static NtCreateSectionFunc NtCreateSection;

    public enum SECTION_INHERIT
    {
      ViewShare = 1,
      ViewUnmap = 2
    }

    public delegate NTSTATUS NtMapViewOfSectionFunc( PHANDLE sh, HANDLE ph, ref Object addr, ULONG_PTR zbits, SIZE_T cs, ref PLARGE_INTEGER off, ref PSIZE_T vs, SECTION_INHERIT ih, ULONG at, ULONG pp );

    public static NtMapViewOfSectionFunc NtMapViewOfSection;

    public delegate NTSTATUS NtCloseFunc( HANDLE h );

    public static NtCloseFunc NtClose;

    /** getpid() returns int; MinGW defines pid_t but MinGW64 typedefs it
     *  as int64 which is wrong. MSVC doesn't define it at all, so just
     *  don't use it.
     */
    public const Int32 LITTLE_ENDIAN = 1234;
    public const Int32 BIG_ENDIAN = 4321;
    public const Int32 BYTE_ORDER = LITTLE_ENDIAN;
    public const Int32 SSIZE_MAX = Int32.MaxValue;

    public static void CACHEFLUSH( Object addr, Object bytes, Object cache ) => throw new NotImplementedException();

    public static void VGMEMP_CREATE( Object h, Object r, Object z ) => throw new NotImplementedException();
    public static void VGMEMP_ALLOC( Object h, Object a, Object s ) => throw new NotImplementedException();
    public static void VGMEMP_FREE( Object h, Object a ) => throw new NotImplementedException();
    public static void VGMEMP_DESTROY( Object h ) => throw new NotImplementedException();
    public static void VGMEMP_DEFINED( Object a, Object s ) => throw new NotImplementedException();

    public const Boolean MDB_DEVEL = false;

    //# define mdb_func_  __FUNCTION__

    public const Int32 MDB_NO_ROOT = MDB_LAST_ERRCODE + 10;
    public const Int32 MDB_OWNERDEAD = 0;
    public const Int32 MDB_USE_ROBUST = 1;
    public const Int32 MDB_ROBUST_SUPPORTED = 1;

    public const Int32 MDB_USE_HASH = 1;
    public const Int32 MDB_PIDLOCK = 0;
    public static Int32 pthread_self() => throw new NotImplementedException(); // something like GetCurrentThreadId()
    public static Int32 pthread_key_create( Object x, Object y ) => throw new NotImplementedException(); // ((*(x) = TlsAlloc()) == TLS_OUT_OF_INDEXES? ErrCode() : 0) // something like x = System.Threading.Thread.SetData()
    public static void pthread_key_delete( Object x ) => throw new NotImplementedException(); // TlsFree( x)
    public static void pthread_getspecific( Object x ) => throw new NotImplementedException(); //  TlsGetValue( x)
    public static void pthread_setspecific( Object x, Object y ) => throw new NotImplementedException(); //  (TlsSetValue(x, y)? 0 : ErrCode())
    public static void pthread_mutex_unlock( Object x ) => throw new NotImplementedException(); //   ReleaseMutex(*x)
    public static void pthread_mutex_lock( Object x ) => throw new NotImplementedException(); //   WaitForSingleObject(*x, INFINITE)
    public static void pthread_cond_signal( Object x ) => throw new NotImplementedException(); //  SetEvent(*x)
    public static void pthread_cond_wait( Object cond, Object mutex ) => throw new NotImplementedException(); //   do{SignalObjectAndWait(*mutex, * cond, INFINITE, FALSE); WaitForSingleObject(*mutex, INFINITE);}while(0)
    public static void THREAD_CREATE( Object thr, Object start, Object arg ) => throw new NotImplementedException(); // (((thr) = CreateThread( NULL, 0, start, arg, 0, NULL)) ? 0 : ErrCode())
    public static void THREAD_FINISH( Object thr ) => throw new NotImplementedException(); // (WaitForSingleObject(thr, INFINITE)? ErrCode() : 0)
    public static void LOCK_MUTEX0( Object mutex ) => throw new NotImplementedException(); //  WaitForSingleObject( mutex, INFINITE)
    public static void UNLOCK_MUTEX( Object mutex ) => throw new NotImplementedException(); //    ReleaseMutex( mutex)
    public static void mdb_mutex_consistent( Object mutex ) => throw new NotImplementedException(); //  0
    public static void getpid() => throw new NotImplementedException(); //  GetCurrentProcessId()
    public static void MDB_FDATASYNC( Object fd ) => throw new NotImplementedException(); //  (!FlushFileBuffers( fd))
    public static void MDB_MSYNC( Object addr, Object len, Object flags ) => throw new NotImplementedException(); //  (!FlushViewOfFile( addr, len))
    public static void ErrCode() => throw new NotImplementedException(); // GetLastError()
    public static void GET_PAGESIZE( Object x ) => throw new NotImplementedException(); // { SYSTEM_INFO si; GetSystemInfo( &si ); ( x ) = si.dwPageSize; }
    public static void close( Object fd ) => throw new NotImplementedException(); // (CloseHandle(fd)? 0 : -1)
    public static void munmap( Object ptr, Object len ) => throw new NotImplementedException(); // UnmapViewOfFile( ptr)
    public const Int32 PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const Int32 MDB_PROCESS_QUERY_LIMITED_INFORMATION = PROCESS_QUERY_LIMITED_INFORMATION;

    public static Char Z => MDB_FMT_Z;
    [Obsolete( "This is a macro that needs to be expanded when used - #define MDB_PRIy(t)  MDB_FMT_Z #t" )]
    public static Object Yu = null; // MDB_PRIy(u)
    [Obsolete( "This is a macro that needs to be expanded when used - #define MDB_PRIy(t)  MDB_FMT_Z #t" )]
    public static Object Yd = null; // MDB_PRIy( d );

    public static Int32 MNAME_LEN = 8; // sizeof( pthread_mutex_t );

    public static String MUTEXNAME_PREFIX = "Global\\MDB";

    public static Int32 LOCK_MUTEX( Object rc, Object env, Object mutex ) => throw new NotImplementedException(); // (((rc) = LOCK_MUTEX0(mutex)) && ((rc) = mdb_mutex_failed(env, mutex, rc)))

    public static Int32 mdb_mutex_failed( MDB_env env, mdb_mutexref_t mutex, Int32 rc ) => throw new NotImplementedException();

    public const Int32 MS_SYNC = 1;
    public const Int32 MS_ASYNC = 0;
    public const Boolean MDB_DEBUG = false;

    public static void DPRINTF( params Object[] args ) { }

    public static void DPUTS( Object arg ) => DPRINTF( "%s", arg );

    /** Debuging output value of a cursor DBI: Negative in a sub-cursor. */
    public static Object DDBI( Object mc ) => throw new NotImplementedException(); // (((mc)->mc_flags & C_SUB) ? -(int)(mc)->mc_dbi : (int)(mc)->mc_dbi) // mc might be a cursor

    public static Int32 MAX_PAGESIZE = /*PAGEBASE ? 0x10000 :*/ 0x8000;

    public const Int32 MDB_MINKEYS = 2;

    public const UInt32 MDB_MAGIC = 0xBEEFC0DE;

    public const Int32 MDB_DATA_VERSION = /*MDB_DEVEL ? 999 :*/ 1;
    public const Int32 MDB_LOCK_VERSION = /*MDB_DEVEL ? 999 :*/ 2;

    public const Int32 MDB_LOCK_VERSION_BITS = 12;
    public const Int32 MDB_MAXKEYSIZE = /* MDB_DEVEL ? 0 :*/ 511;

    public static Int32 ENV_MAXKEY( Object env ) => MDB_MAXKEYSIZE;

    public const UInt32 MAXDATASIZE = 0xffffffff;

    public static void DKBUF() { }
    public static Int32 DKEY( Object x ) => 0;

    /** An invalid page number.
    *  Mainly used to denote an empty tree.
    */
    public static Int32 P_INVALID = ~0;

    /** Test if the flags \b f are set in a flag word \b w. */
    public static Boolean F_ISSET( Int32 w, Int32 f ) => ( w & f ) == f; //   (((w) & (f)) == (f))

    /** Round \b n up to an even number. */
    public static Int64 EVEN( UInt32 n ) => n + 1 & -2; /* sign-extending -2 to match n+1U */

    /** Least significant 1-bit of \b n.  n must be of an unsigned type. */
    public static Int32 LOW_BIT( Int32 n ) => n & -n;

    /** (log2(\b p2) % \b n), for p2 = power of 2 and 0 < n < 8. */
    public static Int64 LOG2_MOD( Int32 p2, Int32 n ) => 7 - 86 / ( p2 % ( ( 1U << n ) - 1 ) + 11 );

    /* Explanation: Let p2 = 2**(n*y + x), x<n and M = (1U<<n)-1. Now p2 =
     * (M+1)**y * 2**x = 2**x (mod M). Finally "/" "happens" to return 7-x.
     */

    /** Should be alignment of \b type. Ensure it is a power of 2. */
    public static Int32 ALIGNOF2( Int32 type ) => throw new NotImplementedException(); // LOW_BIT( offsetof(struct { char ch_; type align_;}, align_))

    /**  Used for offsets within a single page.
     *  Since memory pages are typically 4 or 8KB in size, 12-13 bits,
     *  this is plenty.
     */

    /**  Default size of memory map.
     *  This is certainly too small for any actual applications. Apps should always set
     *  the size explicitly using #mdb_env_set_mapsize().
     */
    public const Int32 DEFAULT_MAPSIZE = 1048576;

    /**  @defgroup readers  Reader Lock Table
     *  Readers don't acquire any locks for their data access. Instead, they
     *  simply record their transaction ID in the reader table. The reader
     *  mutex is needed just to find an empty slot in the reader table. The
     *  slot's address is saved in thread-specific data so that subsequent read
     *  transactions started by the same thread need no further locking to proceed.
     *
     *  If #MDB_NOTLS is set, the slot address is not saved in thread-specific data.
     *
     *  No reader table is used if the database is on a read-only filesystem, or
     *  if #MDB_NOLOCK is set.
     *
     *  Since the database uses multi-version concurrency control, readers don't
     *  actually need any locking. This table is used to keep track of which
     *  readers are using data from which old transactions, so that we'll know
     *  when a particular old transaction is no longer in use. Old transactions
     *  that have discarded any data pages can then have those pages reclaimed
     *  for use by a later write transaction.
     *
     *  The lock table is constructed such that reader slots are aligned with the
     *  processor's cache line size. Any slot is only ever used by one thread.
     *  This alignment guarantees that there will be no contention or cache
     *  thrashing as threads update their own slot info, and also eliminates
     *  any need for locking when accessing a slot.
     *
     *  A writer thread will scan every slot in the table to determine the oldest
     *  outstanding reader transaction. Any freed pages older than this will be
     *  reclaimed by the writer. The writer doesn't use any locks when scanning
     *  this table. This means that there's no guarantee that the writer will
     *  see the most up-to-date reader info, but that's not required for correct
     *  operation - all we need is to know the upper bound on the oldest reader,
     *  we don't care at all about the newest reader. So the only consequence of
     *  reading stale information here is that old pages might hang around a
     *  while longer before being reclaimed. That's actually good anyway, because
     *  the longer we delay reclaiming old pages, the more likely it is that a
     *  string of contiguous pages can be found after coalescing old pages from
     *  many old transactions together.
     *  @{
     */
    /**  Number of slots in the reader table.
     *  This value was chosen somewhat arbitrarily. 126 readers plus a
     *  couple mutexes fit exactly into 8KB on my development machine.
     *  Applications should set the table size using #mdb_env_set_maxreaders().
     */
    public const Int32 DEFAULT_READERS = 126;

    /**  The size of a CPU cache line in bytes. We want our lock structures
     *  aligned to this size to avoid false cache line sharing in the
     *  lock table.
     *  This value works for most CPUs. For Itanium this should be 128.
     */
    public const Int32 CACHELINE = 64;

    /**	The information we store in a single slot of the reader table.
     *	In addition to a transaction ID, we also record the process and
     *	thread ID that owns a slot, so that we can detect stale information,
     *	e.g. threads or processes that went away without cleaning up.
     *	@note We currently don't check for stale records. We simply re-init
     *	the table when we know that we're the only process opening the
     *	lock file.
     */
    public struct MDB_rxbody
    {
      internal const Int32 @sizeof = sizeof( txnid_t ) + sizeof( MDB_PID_T ) + sizeof( MDB_THR_T );
      /**	Current Transaction ID when this transaction began, or (txnid_t)-1.
       *	Multiple readers that start at the same time will probably have the
       *	same ID here. Again, it's not important to exclude them from
       *	anything; all we need to know is which version of the DB they
       *	started from so we can avoid overwriting any data used in that
       *	particular version.
       */
      public volatile txnid_t mrb_txnid;
      /** The process ID of the process owning this reader txn. */
      public volatile MDB_PID_T mrb_pid;
      /** The thread ID of the thread owning this txn. */
      public volatile MDB_THR_T mrb_tid;
    }

    /** The actual reader record, with cacheline padding. */
    public class MDB_reader
    {
      internal const Int32 @sizeof = CACHELINE;

      // TODO: this is a union
      //union {
      public MDB_rxbody mrx;
      /** shorthand for mrb_txnid */
      //#define mr_txnid	mru.mrx.mrb_txnid
      //#define mr_pid	mru.mrx.mrb_pid
      //#define mr_tid	mru.mrx.mrb_tid
      /** cache line alignment */
      public Byte[] pad = new Byte[ MDB_rxbody.@sizeof + CACHELINE - 1 & ~( CACHELINE - 1 ) ];
    }


    /** The header for the reader table.
     *	The table resides in a memory-mapped file. (This is a different file
     *	than is used for the main database.)
     *
     *	For POSIX the actual mutexes reside in the shared memory of this
     *	mapped file. On Windows, mutexes are named objects allocated by the
     *	kernel; we store the mutex names in this mapped file so that other
     *	processes can grab them. This same approach is also used on
     *	MacOSX/Darwin (using named semaphores) since MacOSX doesn't support
     *	process-shared POSIX mutexes. For these cases where a named object
     *	is used, the object name is derived from a 64 bit FNV hash of the
     *	environment pathname. As such, naming collisions are extremely
     *	unlikely. If a collision occurs, the results are unpredictable.
     */
    public class MDB_txbody
    {
      internal const Int32 @sizeof = sizeof( uint32_t ) + sizeof( uint32_t ) + sizeof( txnid_t ) + sizeof( uint32_t ) + sizeof( mdb_hash_t );

      /** Stamp identifying this as an LMDB file. It must be set
       *	to #MDB_MAGIC. */
      public uint32_t mtb_magic;
      /** Format of this lock file. Must be set to #MDB_LOCK_FORMAT. */
      public uint32_t mtb_format;
      /**	The ID of the last transaction committed to the database.
       *	This is recorded here only for convenience; the value can always
       *	be determined by reading the main database meta pages.
       */
      public volatile txnid_t mtb_txnid;
      /** The number of slots that have been used in the reader table.
       *	This always records the maximum count, it is not decremented
       *	when readers release their slots.
       */
      public volatile uint32_t mtb_numreaders;
      /** Binary form of names of the reader/writer locks */
      public mdb_hash_t mtb_mutexid;
    }

    /** @brief Opaque structure for a transaction handle.
     *
     * All database operations require a transaction handle. Transactions may be
     * read-only or read-write.
     */
    /** The actual reader table definition. */
    public class MDB_txninfo
    {
      // TODO: union
      //union {
      MDB_txbody mtb;
      //#define mti_magic	mt1.mtb.mtb_magic
      //#define mti_format	mt1.mtb.mtb_format
      //#define mti_rmutex	mt1.mtb.mtb_rmutex
      //#define mti_txnid	mt1.mtb.mtb_txnid
      //#define mti_numreaders	mt1.mtb.mtb_numreaders
      //#define mti_mutexid	mt1.mtb.mtb_mutexid

      public Byte[] pad = new Byte[ MDB_txbody.@sizeof + CACHELINE - 1 & ~( CACHELINE - 1 ) ];
      //}
      public MDB_reader[] mti_readers = new MDB_reader[ 1 ];
    }


    /** Lockfile format signature: version, features and field layout */
    public const UInt32 MDB_LOCK_FORMAT = MDB_LOCK_VERSION % ( 1U << MDB_LOCK_VERSION_BITS ) + MDB_lock_desc * ( 1U << MDB_LOCK_VERSION_BITS );

    /** Lock type and layout. Values 0-119. _WIN32 implies #MDB_PIDLOCK.
     *	Some low values are reserved for future tweaks.
     */
    public const Int32 MDB_LOCK_TYPE = 0; // TODO: is use of sizeof here correct?
    /* We do not know the inside of a POSIX mutex and how to check if mutexes
     * used by two executables are compatible. Just check alignment and size.
     */

    /* Default CACHELINE=64 vs. other values (have seen mention of 32-256) */
    public const Int32 MDB_lock_desc = 42;

    /** @} */

    /** Common header for all page types. The page type depends on #mp_flags.
     *
     * #P_BRANCH and #P_LEAF pages have unsorted '#MDB_node's at the end, with
     * sorted #mp_ptrs[] entries referring to them. Exception: #P_LEAF2 pages
     * omit mp_ptrs and pack sorted #MDB_DUPFIXED values after the page header.
     *
     * #P_OVERFLOW records occupy one or more contiguous pages where only the
     * first has a page header. They hold the real data of #F_BIGDATA nodes.
     *
     * #P_SUBP sub-pages are small leaf "pages" with duplicate data.
     * A node with flag #F_DUPDATA but not #F_SUBDATA contains a sub-page.
     * (Duplicate data can also go in sub-databases, which use normal pages.)
     *
     * #P_META pages contain #MDB_meta, the start point of an LMDB snapshot.
     *
     * Each non-metapage up to #MDB_meta.%mm_last_pg is reachable exactly once
     * in the snapshot: Either used by a database or listed in a freeDB record.
     */
    public class MDB_page
    {
      internal const Int32 offset_of_mp_ptrs = sizeof( pgno_t ) + sizeof( Int64 ) + sizeof( uint16_t ) + sizeof( uint16_t ) + sizeof( indx_t ) + sizeof( indx_t ) + sizeof( uint32_t );

      //#define	mp_pgno	mp_p.p_pgno
      //#define	mp_next	mp_p.p_next
      // TODO: union
      //	union {
      public pgno_t p_pgno;  /**< page number */
      public MDB_page p_next; /**< for in-memory list of freed pages */ // TODO: maybe use a weak reference
      //	} mp_p;
      public uint16_t mp_pad;     /**< key size if this is a LEAF2 page */
      /**	@defgroup mdb_page	Page Flags
       *	@ingroup internal
       *	Flags for the page headers.
       *	@{
       */
      public const Int32 P_BRANCH = 0x01;   /**< branch page */
      public const Int32 P_LEAF = 0x02;    /**< leaf page */
      public const Int32 P_OVERFLOW = 0x04;    /**< overflow page */
      public const Int32 P_META = 0x08;   /**< meta page */
      public const Int32 P_DIRTY = 0x10;   /**< dirty page, also set for #P_SUBP pages */
      public const Int32 P_LEAF2 = 0x20;   /**< for #MDB_DUPFIXED records */
      public const Int32 P_SUBP = 0x40;   /**< for #MDB_DUPSORT sub-pages */
      public const Int32 P_LOOSE = 0x4000;   /**< page was dirtied then freed, can be reused */
      public const Int32 P_KEEP = 0x8000;		/**< leave this page alone during spill */
      /** @} */
      public uint16_t mp_flags;    /**< @ref mdb_page */
      //#define mp_lower=	mp_pb.pb.pb_lower
      //#define mp_upper=	mp_pb.pb.pb_upper
      //#define mp_pages=	mp_pb.pb_pages
      // TODO: union
      //union {
      //struct {
      public indx_t pb_lower;    /**< lower bound of free space */
      public indx_t pb_upper;    /**< upper bound of free space */
      //}
      public uint32_t pb_pages;  /**< number of overflow pages */

      public indx_t[] mp_ptrs = new indx_t[ 1 ];    /**< dynamic size */
    }


    /** Size of the page header, excluding dynamic data at the end */
    public const UInt32 PAGEHDRSZ = MDB_page.offset_of_mp_ptrs;

    /** Address of first usable data byte in a page, after the header */
    public static Object METADATA( MDB_page p ) => throw new NotImplementedException(); // (void*)( (char*)( p ) + PAGEHDRSZ ) ); // return pointer to the mp_ptrs variable within the page

    // line 1013
    public const Boolean PAGEBASE = /*MDB_DEVEL ? PAGEHDSZ :*/ false;

    /** Number of nodes on a page */
    public static UInt32 NUMKEYS( MDB_page p ) => p.pb_lower - ( PAGEHDRSZ - 0 ) >> 1;

    /** The amount of space remaining in the page */
    public static Int32 SIZELEFT( MDB_page p ) => (indx_t)( p.pb_upper - p.pb_lower );

    /** The percentage of space used in the page, in tenths of a percent. */
    public static Int64 PAGEFILL( MDB_env env, MDB_page p ) => 1000L * ( env.me_psize - PAGEHDRSZ - SIZELEFT( p ) ) / ( env.me_psize - PAGEHDRSZ );
    /** The minimum page fill factor, in tenths of a percent.
     *	Pages emptier than this are candidates for merging.
     */
    public const Int32 FILL_THRESHOLD = 250;

    /** Test if a page is a leaf page */
    public static Boolean IS_LEAF( MDB_page p ) => F_ISSET( p.mp_flags, MDB_page.P_LEAF );
    /** Test if a page is a LEAF2 page */
    public static Boolean IS_LEAF2( MDB_page p ) => F_ISSET( p.mp_flags, MDB_page.P_LEAF2 );
    /** Test if a page is a branch page */
    public static Boolean IS_BRANCH( MDB_page p ) => F_ISSET( p.mp_flags, MDB_page.P_BRANCH );
    /** Test if a page is an overflow page */
    public static Boolean IS_OVERFLOW( MDB_page p ) => F_ISSET( p.mp_flags, MDB_page.P_OVERFLOW );
    /** Test if a page is a sub page */
    public static Boolean IS_SUBP( MDB_page p ) => F_ISSET( p.mp_flags, MDB_page.P_SUBP );

    /** The number of overflow pages needed to store the given size. */
    public static Int64 OVPAGES( Int32 size, Int32 psize ) => ( PAGEHDRSZ - 1 + size ) / psize + 1;

    /** Link in #MDB_txn.%mt_loose_pgs list.
     *  Kept outside the page header, which is needed when reusing the page.
     */
    [Obsolete( "This macro needs to be expanded to 'p.p_next'" )]
    public static MDB_page NEXT_LOOSE_PAGE( MDB_page p ) => throw new NotImplementedException();

    /** Header for a single key/data pair within a page.
     * Used in pages of type #P_BRANCH and #P_LEAF without #P_LEAF2.
     * We guarantee 2-byte alignment for 'MDB_node's.
     *
     * #mn_lo and #mn_hi are used for data size on leaf nodes, and for child
     * pgno on branch nodes.  On 64 bit platforms, #mn_flags is also used
     * for pgno.  (Branch nodes have no flags).  Lo and hi are in host byte
     * order in case some accesses can be optimized to 32-bit word access.
     *
     * Leaf node flags describe node contents.  #F_BIGDATA says the node's
     * data part is the page number of an overflow page with actual data.
     * #F_DUPDATA and #F_SUBDATA can be combined giving duplicate data in
     * a sub-page/sub-database, and named databases (just #F_SUBDATA).
     */
    public class MDB_node
    {
      internal const Int32 offsetof_mn_data = sizeof( UInt16 ) + sizeof( UInt16 ) + sizeof( UInt16 ) + sizeof( UInt16 );

      /** part of data size or pgno
       *	@{ */
      public UInt16 mn_lo;
      public UInt16 mn_hi;
      /** @} */
      /** @defgroup mdb_node Node Flags
       *	@ingroup internal
       *	Flags for node headers.
       *	@{
       */
      public const Int32 F_BIGDATA = 0x01;      /**< data put on overflow page */
      public const Int32 F_SUBDATA = 0x02;      /**< data is a sub-database */
      public const Int32 F_DUPDATA = 0x04;      /**< data has duplicates */

      /** valid flags for #mdb_node_add() */
      public const Int32 NODE_ADD_FLAGS = F_DUPDATA | F_SUBDATA | MDB_RESERVE | MDB_APPEND;

      /** @} */
      public UInt16 mn_flags;    /**< @ref mdb_node */
      public UInt16 mn_ksize;    /**< key size */
      public Byte[] mn_data = new Byte[ 1 ];     /**< key and data are appended here */
    }

    /** Size of the node header, excluding dynamic data at the end */
    public const Int32 NODESIZE = MDB_node.offsetof_mn_data;

    /** Bit position of top word in page number, for shifting mn_flags */
    public const Int32 PGNO_TOPWORD = 0;

    /** Size of a node in a branch page with a given key.
     *	This is just the node header plus the key, there is no data.
     */
    public static Int64 INDXSIZE( MDB_val k ) => NODESIZE + ( k == null ? 0 : k.mv_size );

    /** Size of a node in a leaf page with a given key and data.
     *	This is node header plus key plus data size.
     */
    public static Int64 LEAFSIZE( MDB_val k, MDB_val d ) => NODESIZE + k.mv_size + d.mv_size;

    /** Address of node \b i in page \b p */
    public static Int64 NODEPTR( MDB_page p, Int32 i ) => throw new NotImplementedException(); // p.mp_ptrs[ i ] + PAGEBASE;

    /** Address of the key for the node */
    public static Span<Byte> NODEKEY( MDB_node node ) => new Span<Byte>( node.mn_data, 0, node.mn_ksize );

    /** Address of the data for a node */
    public static Span<Byte> NODEDATA( MDB_node node ) => new Span<Byte>( node.mn_data, node.mn_ksize, node.mn_data.Length - node.mn_ksize );

    /** Get the page number pointed to by a branch node */
    public static Int64 NODEPGNO( MDB_node node ) => node.mn_lo | node.mn_hi << 16;
    /** Set the page number in a branch node */
    public static void SETPGNO( MDB_node node, UInt16 pgno )
    {
      //do
      //{
      node.mn_lo = (UInt16)( pgno & 0xffff );
      node.mn_hi = (UInt16)( pgno >> 16 );
      //} while ( true );
    }

    /** Get the size of the data in a leaf node */
    public static UInt32 NODEDSZ( MDB_node node ) => node.mn_lo | (UInt32)node.mn_hi << 16;
    /** Set the size of the data for a leaf node */
    public static void SETDSZ( MDB_node node, Int32 size )
    {
      //do
      //{
      node.mn_lo = (UInt16)( size & 0xffff );
      node.mn_hi = (UInt16)( size >> 16 );
      //} while ( 0 );
    }

    /** The size of a key in a node */
    public static Int64 NODEKSZ( MDB_node node ) => node.mn_ksize;

    /** Copy a page number from src to dst */
    public static void COPY_PGNO( ref UInt16 dst, ref UInt16 src ) => throw new NotImplementedException();
    //do { 
    //	unsigned short *s, *d;	
    //	s = (unsigned short *)&(src);	
    //	d = (unsigned short *)&(dst);	
    //	*d++ = *s++;	
    //	*d++ = *s++;	
    //	*d++ = *s++;	
    //	*d = *s;	
    //} while (0)

    /** The address of a key in a LEAF2 page.
     *	LEAF2 pages are used for #MDB_DUPFIXED sorted-duplicate sub-DBs.
     *	There are no node headers, keys are stored contiguously.
     */
    public static void LEAF2KEY( Object p, Object i, Object ks ) => throw new NotImplementedException(); //	((char *)(p) + PAGEHDRSZ + ((i)*(ks)))

    /** Set the \b node's key into \b keyptr, if requested. */
    public static void MDB_GET_KEY( MDB_node node, MDB_val keyptr )
    {
      throw new NotImplementedException();
      //if ( ( keyptr ) != null )
      //{
      //  keyptr.mv_size = NODEKSZ( node );
      //  keyptr.mv_data = NODEKEY( node );
      //}
    }

    /** Set the \b node's key into \b key. */
    public static void MDB_GET_KEY2( MDB_node node, MDB_val key )
    {
      throw new NotImplementedException();
      //key.mv_size = NODEKSZ( node );
      //key.mv_data = NODEKEY( node );
    }
    /** Information about a single database in the environment. */
    public class MDB_db
    {
      public uint32_t md_pad;    /**< also ksize for LEAF2 pages */
      public uint16_t md_flags;  /**< @ref mdb_dbi_open */
      public uint16_t md_depth;  /**< depth of this tree */
      public pgno_t md_branch_pages; /**< number of internal pages */
      public pgno_t md_leaf_pages;   /**< number of leaf pages */
      public pgno_t md_overflow_pages; /**< number of overflow pages */
      public mdb_size_t md_entries;    /**< number of data items */
      public pgno_t md_root;   /**< the root page of this tree */
    }

    public const Int32 MDB_VALID = 0x8000;    /**< DB handle is valid, for me_dbflags */
    public const Int32 PERSISTENT_FLAGS = 0xffff & ~MDB_VALID;
    /** #mdb_dbi_open() flags */
    public const Int32 VALID_FLAGS = MDB_REVERSEKEY | MDB_DUPSORT | MDB_INTEGERKEY | MDB_DUPFIXED | MDB_INTEGERDUP | MDB_REVERSEDUP | MDB_CREATE;

    /** Handle for the DB used to track free pages. */
    public const Int32 FREE_DBI = 0;
    /** Handle for the default DB. */
    public const Int32 MAIN_DBI = 1;
    /** Number of DBs in metapage (free and main) - also hardcoded elsewhere */
    public const Int32 CORE_DBS = 2;

    /** Number of meta pages - also hardcoded elsewhere */
    public const Int32 NUM_METAS = 2;

    /** Meta page content.
     *	A meta page is the start point for accessing a database snapshot.
     *	Pages 0-1 are meta pages. Transaction N writes meta page #(N % 2).
     */
    public class MDB_meta
    {
      /** Stamp identifying this as an LMDB file. It must be set
       *	to #MDB_MAGIC. */
      public uint32_t mm_magic;
      /** Version number of this file. Must be set to #MDB_DATA_VERSION. */
      public uint32_t mm_version;
      public Object mm_address;   /**< address for fixed mapping */

      public mdb_size_t mm_mapsize;     /**< size of mmap region */
      public MDB_db[] mm_dbs = new MDB_db[ CORE_DBS ]; /**< first is free space, 2nd is main db */
      /** The size of pages used in this DB */
      //#define	mm_psize	mm_dbs[FREE_DBI].md_pad
      /** Any persistent environment flags. @ref mdb_env */
      //#define	mm_flags	mm_dbs[FREE_DBI].md_flags
      /** Last used page in the datafile.
       *	Actually the file may be shorter if the freeDB lists the final pages.
       */
      public pgno_t mm_last_pg;
      public volatile txnid_t mm_txnid;  /**< txnid that committed this page */
    }

    /** Buffer for a stack-allocated meta page.
     *	The members define size and alignment, and silence type
     *	aliasing warnings.  They are not used directly; that could
     *	mean incorrectly using several union members in parallel.
     */
    public class MDB_metabuf
    {
      public MDB_page mb_page;
      //struct {
      public Byte[] mm_pad = new Byte[ PAGEHDRSZ ];
      public MDB_meta mm_meta;
      //} mb_metabuf;
    }

    /** Auxiliary DB info.
     *	The information here is mostly static/read-only. There is
     *	only a single copy of this record in the environment.
     */
    public class MDB_dbx
    {
      public MDB_val md_name;    /**< name of the database */
      public MDB_cmp_func md_cmp; /**< function for comparing keys */
      public MDB_cmp_func md_dcmp;  /**< function for comparing data items */
      public MDB_rel_func md_rel; /**< user relocate function */
      public Object md_relctx;    /**< user-provided context for md_rel */
    }

    /** A database transaction.
     *	Every operation requires a transaction handle.
     */
    public class MDB_txn
    {
      public MDB_txn mt_parent;   /**< parent of a nested txn */
      /** Nested txn under this txn, set together with flag #MDB_TXN_HAS_CHILD */
      public MDB_txn mt_child;
      public pgno_t mt_next_pgno; /**< next unallocated page */
      /** The ID of this transaction. IDs are integers incrementing from 1.
       *	Only committed write transactions increment the ID. If a transaction
       *	aborts, the ID may be re-used by the next writer.
       */
      public txnid_t mt_txnid;
      public MDB_env mt_env;    /**< the DB environment */
      /** The list of pages that became unused during this transaction.
       */
      public MDB_ID[] mt_free_pgs;
      /** The list of loose pages that became unused and may be reused
       *	in this transaction, linked through #NEXT_LOOSE_PAGE(page).
       */
      public MDB_page mt_loose_pgs;
      /** Number of loose pages (#mt_loose_pgs) */
      public Int32 mt_loose_count;
      /** The sorted list of dirty pages we temporarily wrote to disk
       *	because the dirty list was full. page numbers in here are
       *	shifted left by 1, deleted slots have the LSB set.
       */
      public MDB_ID[] mt_spill_pgs;
      //union {
      /** For write txns: Modified pages. Sorted when not MDB_WRITEMAP. */
      public MDB_ID2[] dirty_list;
      /** For read txns: This thread/txn's reader table slot, or NULL. */
      public MDB_reader reader;
      //} mt_u;
      /** Array of records for each DB known in the environment. */
      public MDB_dbx[] mt_dbxs;
      /** Array of MDB_db records for each known DB */
      public MDB_db mt_dbs;
      /** Array of sequence numbers for each DB handle */
      public UInt32[] mt_dbiseqs;
      /** @defgroup mt_dbflag	Transaction DB Flags
       *	@ingroup internal
       * @{
       */
      public const Int32 DB_DIRTY = 0x01;   /**< DB was written in this txn */
      public const Int32 DB_STALE = 0x02;   /**< Named-DB record is older than txnID */
      public const Int32 DB_NEW = 0x04;   /**< Named-DB handle opened in this txn */
      public const Int32 DB_VALID = 0x08;   /**< DB handle is valid, see also #MDB_VALID */
      public const Int32 DB_USRVALID = 0x10;    /**< As #DB_VALID, but not set for #FREE_DBI */
      public const Int32 DB_DUPDATA = 0x20;		/**< DB is #MDB_DUPSORT data */
      /** @} */
      /** In write txns, array of cursors for each DB */
      public MDB_cursor mt_cursors; // TODO array
      /** Array of flags for each DB */
      public Byte[] mt_dbflags;
      /**	Number of DB records in use, or 0 when the txn is finished.
       *	This number only ever increments until the txn finishes; we
       *	don't decrement it when individual DB handles are closed.
       */
      public MDB_dbi mt_numdbs;

      /** @defgroup mdb_txn	Transaction Flags
       *	@ingroup internal
       *	@{
       */
      /** #mdb_txn_begin() flags */
      public const Int32 MDB_TXN_BEGIN_FLAGS = MDB_NOMETASYNC | MDB_NOSYNC | MDB_RDONLY;
      public const Int32 MDB_TXN_NOMETASYNC = MDB_NOMETASYNC; /**< don't sync meta for this txn on commit */
      public const Int32 MDB_TXN_NOSYNC = MDB_NOSYNC; /**< don't sync this txn on commit */
      public const Int32 MDB_TXN_RDONLY = MDB_RDONLY; /**< read-only transaction */
      /* internal txn flags */
      public const Int32 MDB_TXN_WRITEMAP = MDB_WRITEMAP; /**< copy of #MDB_env flag in writers */
      public const Int32 MDB_TXN_FINISHED = 0x01;   /**< txn is finished or never began */
      public const Int32 MDB_TXN_ERROR = 0x02;    /**< txn is unusable after an error */
      public const Int32 MDB_TXN_DIRTY = 0x04;    /**< must write, even if dirty list is empty */
      public const Int32 MDB_TXN_SPILLS = 0x08;   /**< txn or a parent has spilled pages */
      public const Int32 MDB_TXN_HAS_CHILD = 0x10;    /**< txn has an #MDB_txn.%mt_child */
      /** most operations on the txn are currently illegal */
      public const Int32 MDB_TXN_BLOCKED = MDB_TXN_FINISHED | MDB_TXN_ERROR | MDB_TXN_HAS_CHILD;
      /** @} */
      public UInt32 mt_flags;    /**< @ref mdb_txn */
      /** #dirty_list room: Array size - \#dirty pages visible to this txn.
       *	Includes ancestor txns' dirty pages not hidden by other txns'
       *	dirty/spilled pages. Thus commit(nested txn) has room to merge
       *	dirty_list into mt_parent after freeing hidden mt_parent pages.
       */
      public UInt32 mt_dirty_room;
    }

    /** Enough space for 2^32 nodes with minimum of 2 keys per node. I.e., plenty.
 * At 4 keys per node, enough for 2^64 nodes, so there's probably no need to
 * raise this on a 64 bit machine.
 */
    public const Int32 CURSOR_STACK = 32;

    public class MDB_cursor
    {
      /** Next cursor on this DB in this txn */
      public MDB_cursor mc_next;
      /** Backup of the original cursor if this cursor is a shadow */
      public MDB_cursor mc_backup;
      /** Context used for databases with #MDB_DUPSORT, otherwise NULL */
      public MDB_xcursor mc_xcursor;
      /** The transaction that owns this cursor */
      public MDB_txn mc_txn;
      /** The database handle this cursor operates on */
      public MDB_dbi mc_dbi;
      /** The database record for this cursor */
      public MDB_db mc_db;
      /** The database auxiliary record for this cursor */
      public MDB_dbx mc_dbx;
      /** The @ref mt_dbflag for this database */
      public Byte mc_dbflag;
      public UInt16 mc_snum;  /**< number of pushed pages */
      public UInt16 mc_top;   /**< index of top page, normally mc_snum-1 */
      /** @defgroup mdb_cursor	Cursor Flags
       *	@ingroup internal
       *	Cursor state flags.
       *	@{
       */
      public const UInt32 C_INITIALIZED = 0x01;  /**< cursor has been initialized and is valid */
      public const UInt32 C_EOF = 0x02;      /**< No more data */
      public const UInt32 C_SUB = 0x04;      /**< Cursor is a sub-cursor */
      public const UInt32 C_DEL = 0x08;      /**< last op was a cursor_del */
      public const UInt32 C_UNTRACK = 0x40;    /**< Un-track cursor when closing */
      public const UInt32 C_WRITEMAP = MDB_txn.MDB_TXN_WRITEMAP; /**< Copy of txn flag */
      /** Read-only cursor into the txn's original snapshot in the map.
       *	Set for read-only txns, and in #mdb_page_alloc() for #FREE_DBI when
       *	#MDB_DEVEL & 2. Only implements code which is necessary for this.
       */
      public const Int32 C_ORIG_RDONLY = MDB_txn.MDB_TXN_RDONLY;
      /** @} */
      public UInt32 mc_flags;  /**< @ref mdb_cursor */
      public MDB_page[] mc_pg = new MDB_page[ CURSOR_STACK ];  /**< stack of pushed pages */
      public indx_t[] mc_ki = new indx_t[ CURSOR_STACK ]; /**< stack of page indices */
      public static Object MC_OVPG( Object mc ) => null;
      public static Object MC_SET_OVPG( Object mc, Object pg ) => null;
    }

    /** Context for sorted-dup records.
     *	We could have gone to a fully recursive design, with arbitrarily
     *	deep nesting of sub-databases. But for now we only handle these
     *	levels - main DB, optional sub-DB, sorted-duplicate DB.
     */
    public class MDB_xcursor
    {
      /** A sub-cursor for traversing the Dup DB */
      public MDB_cursor mx_cursor;
      /** The database record for this Dup DB */
      public MDB_db mx_db;
      /**	The auxiliary DB record for this Dup DB */
      public MDB_dbx mx_dbx;
      /** The @ref mt_dbflag for this Dup DB */
      public Byte mx_dbflag;
    }

    /** Check if there is an inited xcursor */
    public static Boolean XCURSOR_INITED( MDB_cursor mc ) => mc.mc_xcursor != null && ( mc.mc_xcursor.mx_cursor.mc_flags & MDB_cursor.C_INITIALIZED ) != 0;

    /** Update the xcursor's sub-page pointer, if any, in \b mc.  Needed
     *	when the node which contains the sub-page may have moved.  Called
     *	with leaf page \b mp = mc->mc_pg[\b top].
         */
    public static void XCURSOR_REFRESH( MDB_cursor mc, Int32 top, MDB_page mp )
    {
      throw new NotImplementedException();
      //do
      //{
      //var xr_pg = mp;
      //if ( !XCURSOR_INITED( mc ) || mc.mc_ki[ top ] >= NUMKEYS( xr_pg ) ) break;
      //var xr_node = (MDB_node)(Object)NODEPTR( xr_pg, mc.mc_ki[ top ] );
      //if ( ( xr_node.mn_flags & ( MDB_node.F_DUPDATA | MDB_node.F_SUBDATA ) ) == MDB_node.F_DUPDATA ) mc.mc_xcursor.mx_cursor.mc_pg[ 0 ] = NODEDATA( xr_node );
      //} while ( 0 );
    }

    /** State of FreeDB old pages, stored in the MDB_env */
    public class MDB_pgstate
    {
      public pgno_t mf_pghead;  /**< Reclaimed freeDB pages, or NULL before use */
      public txnid_t mf_pglast;  /**< ID of last used record, or 0 if !mf_pghead */
    }

    /** @brief Opaque structure for a database environment.
     *
     * A DB environment supports multiple databases, all residing in the same
     * shared-memory map.
     */
    /** The database environment. */
    public class MDB_env
    {
      public HANDLE me_fd;    /**< The main data file */
      public HANDLE me_lfd;   /**< The lock file */
      public HANDLE me_mfd;   /**< For writing and syncing the meta pages */
      HANDLE me_ovfd; /**< Overlapped/async with write-through file handle */
      /** Failed to update the meta page. Probably an I/O error. */
      public const UInt32 MDB_FATAL_ERROR = 0x80000000U;
      /** Some fields are initialized. */
      public const UInt32 MDB_ENV_ACTIVE = 0x20000000U;
      /** me_txkey is set */
      public const UInt32 MDB_ENV_TXKEY = 0x10000000U;
      /** fdatasync is unreliable */
      public const UInt32 MDB_FSYNCONLY = 0x08000000U;
      public uint32_t me_flags;    /**< @ref mdb_env */
      public UInt32 me_psize;  /**< DB page size, inited from me_os_psize */
      public UInt32 me_os_psize; /**< OS page size, from #GET_PAGESIZE */
      public UInt32 me_maxreaders; /**< size of the reader table */
      /** Max #MDB_txninfo.%mti_numreaders of interest to #mdb_env_close() */
      public volatile Int32 me_close_readers;
      public MDB_dbi me_numdbs;    /**< number of DBs opened */
      public MDB_dbi me_maxdbs;    /**< size of the DB table */
      public MDB_PID_T me_pid;   /**< process ID of this env */
      public String me_path;    /**< path to the DB files */
      public Byte[] me_map;   /**< the memory map of the data file */
      public MDB_txninfo me_txns;   /**< the memory map of the lock file or NULL */
      public MDB_meta[] me_metas = new MDB_meta[ NUM_METAS ];  /**< pointers to the two meta pages */
      public Byte[] me_pbuf;    /**< scratch area for DUPSORT put() */
      public MDB_txn me_txn;    /**< current write transaction */
      public MDB_txn me_txn0;   /**< prealloc'd write transaction */
      public mdb_size_t me_mapsize;    /**< size of the data memory map */
      public MDB_OFF_T me_size;    /**< current file size */
      public pgno_t me_maxpg;    /**< me_mapsize / me_psize */
      public MDB_dbx me_dbxs;   /**< array of static DB info */
      public uint16_t me_dbflags; /**< array of flags from MDB_db.md_flags */
      public UInt32[] me_dbiseqs; /**< array of dbi sequence numbers */
      public pthread_key_t me_txkey; /**< thread-key for readers */
      public txnid_t me_pgoldest;  /**< ID of oldest reader last time we looked */
      public MDB_pgstate me_pgstate;   /**< state of old pages from freeDB */
      // #define me_pglast	me_pgstate.mf_pglast
      // #define me_pghead	me_pgstate.mf_pghead
      public MDB_page me_dpages;    /**< list of malloc'd blocks for re-use */
      /** IDL of pages that became unused in a write txn */
      public MDB_ID[] me_free_pgs;
      /** ID2L of pages written during a write txn. Length MDB_IDL_UM_SIZE. */
      public MDB_ID2[] me_dirty_list;
      /** Max number of freelist items that can fit in a single overflow page */
      public Int32 me_maxfree_1pg;
      /** Max size of a node on a page */
      public UInt32 me_nodemax;
      public Int32 me_live_reader;   /**< have liveness lock in reader table */
      public Int32 me_pidquery;    /**< Used in OpenProcess */
      public Object ov;     /**< Used for for overlapping I/O requests */
      public Int32 ovs;        /**< Count of OVERLAPPEDs */
      public mdb_mutex_t me_rmutex;
      public mdb_mutex_t me_wmutex;
      public String me_mutexname;
      public Byte[] me_userctx;  /**< User-settable context */
      public MDB_assert_func me_assert_func; /**< Callback for assertion failures */
    }

    /** Nested transaction */
    public class MDB_ntxn
    {
      public MDB_txn mnt_txn;   /**< the transaction */
      public MDB_pgstate mnt_pgstate; /**< parent transaction's saved freestate */
    }

    public const Int32 MDB_COMMIT_PAGES = 64;

    /** max bytes to write in one call */
    public const UInt32 MAX_WRITE = 0x40000000U;

    public static Boolean TXN_DBI_EXIST( MDB_txn txn, Int32 dbi, Byte validity ) => txn != null && dbi < txn.mt_numdbs && ( txn.mt_dbflags[ dbi ] & validity ) != 0;

    /** Check for misused \b dbi handles */
    public static Boolean TXN_DBI_CHANGED( MDB_txn txn, Int32 dbi ) => txn.mt_dbiseqs[ dbi ] != txn.mt_env.me_dbiseqs[ dbi ];

    public static Int32 mdb_page_alloc( MDB_cursor mc, Int32 num, out MDB_page mp ) => throw new NotImplementedException();
    public static Int32 mdb_page_new( MDB_cursor mc, uint32_t flags, Int32 num, out MDB_page mp ) => throw new NotImplementedException();
    public static Int32 mdb_page_touch( MDB_cursor mc ) => throw new NotImplementedException();

    public static String[] MDB_END_NAMES = { "committed", "empty-commit", "abort", "reset", "reset-tmp", "fail-begin", "fail-beginchild" };

    /* mdb_txn_end operation number, for logging */
    public const Int32 MDB_END_COMMITTED = 0;
    public const Int32 MDB_END_EMPTY_COMMIT = 1;
    public const Int32 MDB_END_ABORT = 2;
    public const Int32 MDB_END_RESET = 3;
    public const Int32 MDB_END_RESET_TMP = 4;
    public const Int32 MDB_END_FAIL_BEGIN = 5;
    public const Int32 MDB_END_FAIL_BEGINCHILD = 6;

    public const Int32 MDB_END_OPMASK = 0x0F; /**< mask for #mdb_txn_end() operation number */
    public const Int32 MDB_END_UPDATE = 0x10; /**< update env state (DBIs) */
    public const Int32 MDB_END_FREE = 0x20; /**< free txn unless it is #MDB_env.%me_txn0 */
    public const Int32 MDB_END_SLOT = MDB_NOTLS; /**< release any reader slot if #MDB_NOTLS */

    public static void mdb_txn_end( MDB_txn txn, UInt32 mode ) => throw new NotImplementedException();
    public static Int32 mdb_page_get( MDB_cursor mc, pgno_t pgno, out MDB_page mp, ref Int32 lvl ) => throw new NotImplementedException();
    public static Int32 mdb_page_search_root( MDB_cursor mc, MDB_val key, Int32 modify ) => throw new NotImplementedException();

    public static Int32 MDB_PS_MODIFY = 1;
    public static Int32 MDB_PS_ROOTONLY = 2;
    public static Int32 MDB_PS_FIRST = 4;
    public static Int32 MDB_PS_LAST = 8;

    public static Int32 mdb_page_search( MDB_cursor mc, MDB_val key, Int32 flags ) => throw new NotImplementedException();
    public static Int32 mdb_page_merge( MDB_cursor csrc, MDB_cursor cdst ) => throw new NotImplementedException();

    public const Int32 MDB_SPLIT_REPLACE = MDB_APPENDDUP; /**< newkey is not new */
    public static Int32 mdb_page_split( MDB_cursor mc, MDB_val newkey, MDB_val newdata, pgno_t newpgno, UInt32 nflags ) => throw new NotImplementedException();

    public static Int32 mdb_env_read_header( MDB_env env, Int32 prev, MDB_meta meta ) => throw new NotImplementedException();
    public static MDB_meta mdb_env_pick_meta( MDB_env env ) => throw new NotImplementedException();
    public static Int32 mdb_env_write_meta( MDB_txn txn ) => throw new NotImplementedException();

    public static void mdb_env_close0( MDB_env env, Int32 excl ) => throw new NotImplementedException();

    public static MDB_node mdb_node_search( MDB_cursor mc, MDB_val key, ref Int32 exactp ) => throw new NotImplementedException();
    public static Int32 mdb_node_add( MDB_cursor mc, indx_t indx, MDB_val key, MDB_val data, pgno_t pgno, UInt32 flags ) => throw new NotImplementedException();
    public static void mdb_node_del( MDB_cursor mc, Int32 ksize ) => throw new NotImplementedException();
    public static void mdb_node_shrink( MDB_page mp, indx_t indx ) => throw new NotImplementedException();
    public static Int32 mdb_node_move( MDB_cursor csrc, MDB_cursor cdst, Int32 fromleft ) => throw new NotImplementedException();
    public static Int32 mdb_node_read( MDB_cursor mc, MDB_node leaf, MDB_val data ) => throw new NotImplementedException();
    public static size_t mdb_leaf_size( MDB_env env, MDB_val key, MDB_val data ) => throw new NotImplementedException();
    public static size_t mdb_branch_size( MDB_env env, MDB_val key ) => throw new NotImplementedException();

    public static Int32 mdb_rebalance( MDB_cursor mc ) => throw new NotImplementedException();
    public static Int32 mdb_update_key( MDB_cursor mc, MDB_val key ) => throw new NotImplementedException();

    public static void mdb_cursor_pop( MDB_cursor mc ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_push( MDB_cursor mc, MDB_page mp ) => throw new NotImplementedException();

    public static Int32 mdb_cursor_del0( MDB_cursor mc ) => throw new NotImplementedException();
    public static Int32 mdb_del0( MDB_txn txn, MDB_dbi dbi, MDB_val key, MDB_val data, UInt32 flags ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_sibling( MDB_cursor mc, Int32 move_right ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_next( MDB_cursor mc, MDB_val key, MDB_val data, MDB_cursor_op op ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_prev( MDB_cursor mc, MDB_val key, MDB_val data, MDB_cursor_op op ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_set( MDB_cursor mc, MDB_val key, MDB_val data, MDB_cursor_op op, ref Int32 exactp ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_first( MDB_cursor mc, MDB_val key, MDB_val data ) => throw new NotImplementedException();
    public static Int32 mdb_cursor_last( MDB_cursor mc, MDB_val key, MDB_val data ) => throw new NotImplementedException();

    public static void mdb_cursor_init( MDB_cursor mc, MDB_txn txn, MDB_dbi dbi, MDB_xcursor mx ) => throw new NotImplementedException();
    public static void mdb_xcursor_init0( MDB_cursor mc ) => throw new NotImplementedException();
    public static void mdb_xcursor_init1( MDB_cursor mc, MDB_node node ) => throw new NotImplementedException();
    public static void mdb_xcursor_init2( MDB_cursor mc, MDB_xcursor src_mx, Int32 force ) => throw new NotImplementedException();

    public static Int32 mdb_drop0( MDB_cursor mc, Int32 subs ) => throw new NotImplementedException();
    public static void mdb_default_cmp( MDB_txn txn, MDB_dbi dbi ) => throw new NotImplementedException();
    public static Int32 mdb_reader_check0( MDB_env env, Int32 rlocked, ref Int32 dead ) => throw new NotImplementedException();

    /** @cond */
    public static MDB_cmp_func mdb_cmp_memn;
    public static MDB_cmp_func mdb_cmp_memnr;
    public static MDB_cmp_func mdb_cmp_int;
    public static MDB_cmp_func mdb_cmp_cint;
    public static MDB_cmp_func mdb_cmp_long;
    /** @endcond */

    public static MDB_cmp_func mdb_cmp_clong = mdb_cmp_cint;

    /** True if we need #mdb_cmp_clong() instead of \b cmp for #MDB_INTEGERDUP */
    public static Boolean NEED_CMP_CLONG( MDB_cmp_func cmp, Int64 ksize ) => ( cmp == mdb_cmp_int && ( ksize ) == sizeof( mdb_size_t ) );

    public static Object mdb_null_sd; // SECURITY_DESCRIPTOR
    public static Object mdb_all_sa; // SECURITY_ATTRIBUTES
    public static Int32 mdb_sec_inited;

    public class MDB_name
    {
    }

    public static Int32 utf8_to_utf16( String src, MDB_name dst, Int32 xtra ) => throw new NotImplementedException();

    /** Return the library version info. */
    public static String mdb_version( ref Int32 major, ref Int32 minor, ref Int32 patch )
    {
      if ( major == 0 ) major = MDB_VERSION_MAJOR;
      if ( minor == 0 ) minor = MDB_VERSION_MINOR;
      if ( patch == 0 ) patch = MDB_VERSION_PATCH;
      return MDB_VERSION_STRING;
    }


    /** Table of descriptions for LMDB @ref errors */
    public static String[] mdb_errstr =
      {
        "MDB_KEYEXIST: Key/data pair already exists",
        "MDB_NOTFOUND: No matching key/data pair found",
        "MDB_PAGE_NOTFOUND: Requested page not found",
        "MDB_CORRUPTED: Located page was wrong type",
        "MDB_PANIC: Update of meta page failed or environment had fatal error",
        "MDB_VERSION_MISMATCH: Database environment version mismatch",
        "MDB_INVALID: File is not an LMDB file",
        "MDB_MAP_FULL: Environment mapsize limit reached",
        "MDB_DBS_FULL: Environment maxdbs limit reached",
        "MDB_READERS_FULL: Environment maxreaders limit reached",
        "MDB_TLS_FULL: Thread-local storage keys full - too many environments open",
        "MDB_TXN_FULL: Transaction has too many dirty pages - transaction too big",
        "MDB_CURSOR_FULL: Internal error - cursor stack limit reached",
        "MDB_PAGE_FULL: Internal error - page has no more space",
        "MDB_MAP_RESIZED: Database contents grew beyond environment mapsize",
        "MDB_INCOMPATIBLE: Operation and DB incompatible, or DB flags changed",
        "MDB_BAD_RSLOT: Invalid reuse of reader locktable slot",
        "MDB_BAD_TXN: Transaction must abort, has a child, or is invalid",
        "MDB_BAD_VALSIZE: Unsupported size of key/DB name/data, or wrong DUPFIXED size",
        "MDB_BAD_DBI: The specified DBI handle was closed/changed unexpectedly",
        "MDB_PROBLEM: Unexpected problem - txn should abort",
      };


    public const Int32 ENOENT = 2;  /* 2, FILE_NOT_FOUND */
    public const Int32 EIO = 5;   /* 5, ACCESS_DENIED */
    public const Int32 ENOMEM = 12;  /* 12, INVALID_ACCESS */
    public const Int32 EACCES = 13;  /* 13, INVALID_DATA */
    public const Int32 EBUSY = 16;   /* 16, CURRENT_DIRECTORY */
    public const Int32 EINVAL = 22;  /* 22, BAD_COMMAND */
    public const Int32 ENOSPC = 26;  /* 28, OUT_OF_PAPER */

    /** @brief Return a string describing a given error code.
      *
      * This function is a superset of the ANSI C X3.159-1989 (ANSI C) strerror(3)
      * function. If the error code is greater than or equal to 0, then the string
      * returned by the system function strerror(3) is returned. If the error code
      * is less than 0, an error string corresponding to the LMDB library error is
      * returned. See @ref errors for a list of LMDB-specific error codes.
      * @param[in] err The error code
      * @retval "error message" The description of the error
      */
    public static String mdb_strerror( Int32 err )
    {
      /** HACK: pad 4KB on stack over the buf. Return system msgs in buf.
       *	This works as long as no function between the call to mdb_strerror
       *	and the actual use of the message uses more than 4K of stack.
       */
      if ( err == 0 ) return ( "Successful return: 0" );

      if ( err >= MDB_KEYEXIST && err <= MDB_LAST_ERRCODE )
      {
        var i = err - MDB_KEYEXIST;
        return mdb_errstr[ i ];
      }

      /* These are the C-runtime error codes we use. The comment indicates
       * their numeric value, and the Win32 error they would correspond to
       * if the error actually came from a Win32 API. A major mess, we should
       * have used LMDB-specific error codes for everything.
       */
      switch ( err )
      {
        case ENOENT:  /* 2, FILE_NOT_FOUND */
        case EIO:   /* 5, ACCESS_DENIED */
        case ENOMEM:  /* 12, INVALID_ACCESS */
        case EACCES:  /* 13, INVALID_DATA */
        case EBUSY:   /* 16, CURRENT_DIRECTORY */
        case EINVAL:  /* 22, BAD_COMMAND */
        case ENOSPC:  /* 28, OUT_OF_PAPER */
          //return strerror( err );
          return err.ToString();
        default: break;
      }

      // FormatMessageA was here
      return err.ToString();
    }


    /** assert(3) variant in cursor context */
    public static void mdb_cassert( MDB_cursor mc, Boolean expr ) => mdb_assert0( mc.mc_txn.mt_env, expr, "" /*, #expr*/);
    /** assert(3) variant in transaction context */
    public static void mdb_tassert( MDB_txn txn, Boolean expr ) => mdb_assert0( txn.mt_env, expr, "" /*, #expr*/);
    /** assert(3) variant in environment context */
    public static void mdb_eassert( MDB_env env, Boolean expr ) => mdb_assert0( env, expr, ""/*, #expr*/);

    public static void mdb_assert0( MDB_env env, Boolean expr, String expr_txt )
    {
      if ( expr ) return;
      mdb_assert_fail( env, expr_txt, "mdb_func_", "__FILE__", "__LINE__" );
    }

    public static void mdb_assert_fail( MDB_env env, String expr_txt, String func, String file, /*Int32*/ String line )
    {
      var buf = $"{file}: Assertion '{expr_txt}' failed in {func}()";
      if ( env.me_assert_func != null ) env.me_assert_func( env, buf );
      Console.WriteLine( buf );
      throw new Exception( buf );
    }

    /** @brief Compare two data items according to a particular database.
     *
     * This returns a comparison as if the two data items were keys in the
     * specified database.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] a The first item to compare
     * @param[in] b The second item to compare
     * @return < 0 if a < b, 0 if a == b, > 0 if a > b
     */
    public static Int32 mdb_cmp( MDB_txn txn, MDB_dbi dbi, MDB_val a, MDB_val b )
    {
      return txn.mt_dbxs[ dbi ].md_cmp( a, b );
    }

    /** @brief Compare two data items according to a particular database.
     *
     * This returns a comparison as if the two items were data items of
     * the specified database. The database must have the #MDB_DUPSORT flag.
     * @param[in] txn A transaction handle returned by #mdb_txn_begin()
     * @param[in] dbi A database handle returned by #mdb_dbi_open()
     * @param[in] a The first item to compare
     * @param[in] b The second item to compare
     * @return < 0 if a < b, 0 if a == b, > 0 if a > b
     */
    public static Int32 mdb_dcmp( MDB_txn txn, MDB_dbi dbi, MDB_val a, MDB_val b )
    {
      var dcmp = txn.mt_dbxs[ dbi ].md_dcmp;
      if ( NEED_CMP_CLONG( dcmp, a.mv_size ) ) dcmp = mdb_cmp_clong;
      return dcmp( a, b );
    }

    /** Allocate memory for a page.
     * Re-use old malloc'd pages first for singletons, otherwise just malloc.
     * Set #MDB_TXN_ERROR on failure.
     */
    public static MDB_page mdb_page_malloc( MDB_txn txn, UInt32 num )
    {
      var env = txn.mt_env;
      var ret = env.me_dpages;
      var psize = env.me_psize;
      var sz = psize;
      //size_t off = 0;
      /* For ! #MDB_NOMEMINIT, psize counts how much to init.
       * For a single page alloc, we init everything after the page header.
       * For multi-page, we init the final page; if the caller needed that
       * many pages they will be filling in at least up to the last page.
       */
      if ( num == 1 )
      {
        if ( ret != null )
        {
          //VGMEMP_ALLOC( env, ret, sz );
          //VGMEMP_DEFINED( ret, sizeof( ret.mp_next ) );
          env.me_dpages = ret.p_next;
          return ret;
        }
        //off = PAGEHDRSZ; // TODO: check this one
        //psize -= off;
      }
      else
      {
        sz *= num;
        //off = sz - psize;
      }

      // TODO: num is most likely the number of new MDB_Page objects to create, so the return is more likely to be an array of MDB_page rather than a single one
      ret = new MDB_page();
      //if ( ret /*malloc( sz )*/ != null ) // TODO: malloc
      //{
      VGMEMP_ALLOC( env, ret, sz );
      if ( ( env.me_flags & MDB_NOMEMINIT ) == 0 )
      {
        //memset( ret + off, 0, psize ); // TODO: memset
        ret.mp_pad = 0;
      }
      //}
      //else
      //{
      //  txn.mt_flags |= MDB_txn.MDB_TXN_ERROR;
      //}
      return ret;
    }

    /** Free a single page.
 * Saves single pages to a list, for future reuse.
 * (This is not used for multi-page overflow pages.)
 */
    public static void mdb_page_free( MDB_env env, MDB_page mp )
    {
      mp.p_next = env.me_dpages;
      //VGMEMP_FREE(env, mp);
      env.me_dpages = mp;
    }

    /** Free a dirty page */
    public static void mdb_dpage_free( MDB_env env, MDB_page dp )
    {
      if ( !IS_OVERFLOW( dp ) || dp.pb_pages == 1 )
      {
        mdb_page_free( env, dp );
      }
      else
      {
        /* large pages just get freed directly */
        //VGMEMP_FREE( env, dp );
        //free( dp );
        // TODO: free the memory
      }
    }

    // line 2058

    /**	Return all dirty pages to dpage list */
    public static void mdb_dlist_free( MDB_txn txn )
    {
      var env = txn.mt_env;
      var dl = txn.dirty_list;
      var n = dl[ 0 ].mid;

      for ( var i = 1; i <= n; i++ )
      {
        mdb_dpage_free( env, dl[ i ].mptr );
      }
      dl[ 0 ].mid = 0;
    }

    public static void MDB_PAGE_UNREF( MDB_txn txn, MDB_page mp ) { }
    public static void MDB_CURSOR_UNREF( MDB_cursor mc, Boolean force ) { }


    /** Loosen or free a single page.
     * Saves single pages to a list for future reuse
     * in this same txn. It has been pulled from the freeDB
     * and already resides on the dirty list, but has been
     * deleted. Use these pages first before pulling again
     * from the freeDB.
     *
     * If the page wasn't dirtied in this txn, just add it
     * to this txn's free list.
     */
    public static Int32 mdb_page_loose( MDB_cursor mc, MDB_page mp )
    {
      var loose = 0;
      var pgno = mp.p_pgno;
      var txn = mc.mc_txn;

      if ( ( ( mp.mp_flags & MDB_page.P_DIRTY ) != 0 ) && mc.mc_dbi != FREE_DBI )
      {
        if ( txn.mt_parent != null )
        {
          var dl = txn.dirty_list;
          /* If txn has a parent, make sure the page is in our
           * dirty list.
           */
          if ( dl[ 0 ].mid != 0 )
          {
            var x = mdb_mid2l_search( dl, pgno );
            if ( x <= dl[ 0 ].mid && dl[ x ].mid == pgno )
            {
              if ( mp != dl[ x ].mptr )
              { /* bad cursor? */
                mc.mc_flags &= ~( MDB_cursor.C_INITIALIZED | MDB_cursor.C_EOF );
                txn.mt_flags |= MDB_txn.MDB_TXN_ERROR;
                return MDB_PROBLEM;
              }
              /* ok, it's ours */
              loose = 1;
            }
          }
        }
        else
        {
          /* no parent txn, so it's just ours */
          loose = 1;
        }
      }
      if ( loose != 0 )
      {
        DPRINTF( ("loosen db %d page %", DDBI( mc ), mp.p_pgno) );
        mp.p_next = txn.mt_loose_pgs;
        txn.mt_loose_pgs = mp;
        txn.mt_loose_count++;
        mp.mp_flags |= MDB_page.P_LOOSE;
      }
      else
      {
        var rc = mdb_midl_append( txn.mt_free_pgs, pgno );
        if ( rc != 0 ) return rc;
      }

      return MDB_SUCCESS;
    }

    // line 2173
  }
}

/** @brief A handle for an individual database in the DB environment. */

/** @brief Opaque structure for navigating through a database */
