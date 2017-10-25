namespace Blockchain
{
  using System;

  static class Utils
  {
    public static int ToInt(this string v) => Convert.ToInt32(v);
    public static long ToLong(this string v) => Convert.ToInt64(v);
    public static uint ToId(this string v) => Convert.ToUInt32(v.Substring(2), 16);
  }
}
