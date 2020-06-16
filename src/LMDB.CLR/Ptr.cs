using System;
using System.Collections.Generic;

// ReSharper disable IdentifierTypo

namespace LMDB.CLR
{
  public class Ptr<T>
  {
    private T _value;

    public Ptr( T value ) => Deref = value;

    public Ptr( IReadOnlyList<T> typedList, Int32 i )
    {
      Deref = typedList[ i ];
    }

    public ref T Deref => ref _value;

    public static Ptr<T> Null => new Ptr<T>( default );

    public static Ptr<T> operator +( Ptr<T> a, UInt64 b ) => throw new NotImplementedException();

    public static Ptr<T> operator +( Ptr<T> a, UInt16 b ) => throw new NotImplementedException();

    public static Ptr<T> Create<TQ>( Ptr<TQ> other ) => throw new NotImplementedException();
  }
}
