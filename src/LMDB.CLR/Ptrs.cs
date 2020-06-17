using System;

// ReSharper disable IdentifierTypo

namespace LMDB.CLR
{
  public class Ptrs<T> where T : struct
  {
    public Ptrs( Int32 count ) => _values = new T[ count ];

    public Ptrs( UInt32 count ) => _values = new T[ count ];

    private readonly T[] _values;

    public ref T this[ Int32 index ] => ref _values[ index ];

    public ref T this[ UInt32 index ] => ref _values[ (Int32)index ];

    public ref T this[ UInt64 index ] => ref _values[ (Int32)index ];

    public ref T Deref => ref _values[ 0 ];

    public void Free( Int32 i )
    {
      // i being -1 likely refers to the memory location prior to the first element of the array
      throw new NotImplementedException();
    }

    public static Ptr<T> operator +( Ptrs<T> a, UInt64 b ) => throw new NotImplementedException();

    public static Ptr<T> operator +( Ptrs<T> a, UInt16 b ) => throw new NotImplementedException();

    public static implicit operator Ptrs<T>( Ptr<T> a ) => new Ptrs<T>( 1 );

    public static implicit operator Ptr<T>( Ptrs<T> a ) => new Ptr<T>( a._values[ 0 ] );
  }
}