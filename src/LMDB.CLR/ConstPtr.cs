using System;
// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace LMDB.CLR
{
  public class ConstPtr<T>
  {
    public T Deref { get; }
  }
}