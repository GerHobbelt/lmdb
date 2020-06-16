using System;

namespace LMDB.CLR
{
  public class LineAttribute : Attribute
  {
    public Int32 LineNumber { get; }

    public LineAttribute( Int32 lineNumber )
    {
      LineNumber = lineNumber;
    }
  }
}