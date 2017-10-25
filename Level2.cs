namespace Blockchain.Level2
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text.RegularExpressions;

  class Program
  {
    public static void Run()
    {
      var root = "level2/";
      var files = new[]
      {
        "level2-eg.txt",
        "level2-1.txt",
        "level2-2.txt",
        "level2-3.txt",
        "level2-4.txt"
      };

      Array.ForEach(files, file => new Program($"{root}{file}"));
    }

    Program(string filepath)
    {
      var lines = File.ReadAllLines(filepath);
      var na = lines[0].ToInt();

      RawAccs = lines.Skip(1).Take(na).Select(Acc.Parse);
      RawTxs = lines.Skip(na + 2).Select(Tx.Parse).OrderBy(x => x.Submitted);

      File.WriteAllLines(filepath.Replace(".txt", ".out.txt"), Start());
    }

    IEnumerable<Acc> RawAccs;
    IEnumerable<Tx> RawTxs;

    class Acc
    {
      public string Name;
      public string No;
      public long Amount;
      public long Overdraft;

      public static Acc Parse(string line)
      {
        var p = line.Split(' ');
        return new Acc
        {
          Name = p[0],
          No = p[1],
          Amount = p[2].ToLong(),
          Overdraft = p[3].ToLong()
        };
      }
    }

    struct Tx
    {
      public string From;
      public string To;
      public long Amount;
      public long Submitted;

      public static Tx Parse(string line)
      {
        var p = line.Split(' ');
        return new Tx
        {
          From = p[0],
          To = p[1],
          Amount = p[2].ToLong(),
          Submitted = p[3].ToLong()
        };
      }
    }

    IEnumerable<string> Start()
    {
      var book = new Dictionary<string, Acc>();

      foreach (var a in RawAccs)
        if (IsValid(a.No))
          book[a.No] = a;

      foreach (var t in RawTxs)
      {
        if (!IsValid(t.From)) continue;
        if (!IsValid(t.To)) continue;
        if (t.Amount <= 0) continue;
        if (!book.ContainsKey(t.To)) continue;

        var fa = book[t.From];
        var ta = book[t.To];

        if (fa.Amount - t.Amount < -fa.Overdraft) continue;

        fa.Amount -= t.Amount;
        ta.Amount += t.Amount;
      }

      yield return book.Count.ToString();
      foreach (var i in book.Values)
        yield return $"{i.Name} {i.Amount}";
    }

    bool IsValid(string acc)
    {
      var m = Regex.Match(acc, @"CAT(\d{2})([a-zA-Z]{10})").Groups;
      var crc = m[1].Value.ToInt();
      var id = m[2].Value.ToCharArray();
      var sum = "CAT00".Aggregate(0, (s, c) => s += (int)c);

      for (int i = 0; i < id.Length; i++)
      {
        var c = id[i];

        if (char.IsWhiteSpace(c)) continue;

        var p = char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
        var j = Array.FindIndex(id, p.Equals);

        if (j == -1) return false;

        id[i] = ' ';
        id[j] = ' ';

        sum += (int)c + (int)p;
      }

      sum = 98 - (sum % 97);

      if (Array.TrueForAll(id, char.IsWhiteSpace) && sum == crc)
        return true;

      return false;
    }
  }
}
