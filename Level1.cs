namespace Blockchain.Level1
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;

  class Program
  {
    public static void Run()
    {
      var root = "level1/";
      var files = new[]
      {
        "level1-eg.txt",
        "level1-1.txt",
        "level1-2.txt",
        "level1-3.txt",
        "level1-4.txt"
      };

      Array.ForEach(files, file => new Program($"{root}{file}"));
    }

    Program(string filepath)
    {
      var lines = File.ReadAllLines(filepath);
      var na = lines[0].ToInt();

      RawAccs = lines.Skip(1).Take(na);
      RawTxs = lines.Skip(na + 2);

      File.WriteAllLines(filepath.Replace(".txt", ".out.txt"), Start());
    }

    IEnumerable<string> RawAccs;
    IEnumerable<string> RawTxs;

    IEnumerable<string> Start()
    {
      var book = new Dictionary<string, long>();
      foreach (var item in RawAccs)
      {
        var parts = item.Split(' ');
        book.Add(parts[0], parts[1].ToLong());
      }

      foreach (var item in RawTxs)
      {
        var pts = item.Split(' ');
        book[pts[0]] -= pts[2].ToLong();
        book[pts[1]] += pts[2].ToLong();
      }

      yield return book.Count.ToString();
      foreach (var i in book)
        yield return $"{i.Key} {i.Value}";
    }
  }
}
