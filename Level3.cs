namespace Blockchain.Level3
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
      var root = "level3/";
      var files = new[]
      {
        "level3-eg.txt",
        "level3-1.txt",
        "level3-2.txt",
        "level3-3.txt",
        "level3-4.txt"
      };

      Array.ForEach(files, file => new Program($"{root}{file}"));
    }

    Program(string filepath)
    {
      var lines = File.ReadAllLines(filepath);
      var nt = lines[0].ToInt();

      RawTxs = lines.Skip(1)
                    .Take(nt)
                    .Select(Tx.Parse)
                    .OrderBy(x => x.Submitted);

      File.WriteAllLines(filepath.Replace(".txt", ".out.txt"), Start());
    }

    IEnumerable<Tx> RawTxs;

    struct In
    {
      public uint TxId;
      public string Owner;
      public int Amount;
      public In(uint txId, string owner, int amount)
      {
        TxId = txId;
        Owner = owner;
        Amount = amount;
      }
      public static In Parse(string line)
      {
        var p = line.Split(' ');
        return new In(p[0].ToId(), p[1], p[2].ToInt());
      }
      public override string ToString()
        => $"0x{TxId.ToString("X8")} {Owner} {Amount}";
    }

    struct Out
    {
      public string Owner;
      public int Amount;
      public Out(string owner, int amount)
      {
        Owner = owner;
        Amount = amount;
      }
      public In ToIn(uint txId) => new In(txId, Owner, Amount);
      public override string ToString() => $"{Owner} {Amount}";
    }

    class Tx
    {
      public uint Id;
      public List<In> Ins;
      public List<Out> Outs;
      public long Submitted;

      public static Tx Parse(string line)
      {
        var q = new Queue<string>(line.Split(' '));
        var id = q.Dequeue().ToId();

        var ni = q.Dequeue().ToInt();
        var ins = new List<In>();
        for (int i = 0; i < ni; i++)
          ins.Add(new In(
            q.Dequeue().ToId(),
            q.Dequeue(),
            q.Dequeue().ToInt()));

        var no = q.Dequeue().ToInt();
        var outs = new List<Out>();
        for (int i = 0; i < no; i++)
          outs.Add(new Out(
            q.Dequeue(),
            q.Dequeue().ToInt()));

        var sbm = q.Dequeue().ToLong();

        return new Tx
        {
          Id = id,
          Ins = ins,
          Outs = outs,
          Submitted = sbm
        };
      }

      public override string ToString()
        => $"0x{Id.ToString("X8")} {Ins.Count} {string.Join(" ", Ins)} {Outs.Count} {string.Join(" ", Outs)} {Submitted}";
    }

    IEnumerable<string> Start()
    {
      var log = new List<Tx>();

      foreach (var tx in RawTxs)
        if (IsValid(tx))
          AppendTo(log, tx);

      yield return log.Count.ToString();
      foreach (var item in log)
        yield return $"{item}";
    }

    List<In> FreeIns = new List<In>();

    void AppendTo(IList<Tx> log, Tx tx)
    {
      log.Add(tx);

      foreach (var el in tx.Outs)
        FreeIns.Add(el.ToIn(tx.Id));
    }

    bool IsValid(Tx tx)
    {
      var amountBalanced = 0;
      var dedup = new List<In>();
      foreach (var i in tx.Ins)
      {
        amountBalanced += i.Amount;

        if (i.Amount <= 0)
          return false;

        if (dedup.Contains(i))
          return false;
        dedup.Add(i);

        // origin txs can be unbalanced,
        // e.g. in.amount != out.amount 
        if (i.Owner == "origin")
          continue;

        if (!FreeIns.Contains(i))
          return false;
      }

      var dedupOwners = new List<string>();
      foreach (var i in tx.Outs)
      {
        amountBalanced -= i.Amount;

        if (i.Amount <= 0)
          return false;

        if (dedupOwners.Contains(i.Owner))
          return false;

        dedupOwners.Add(i.Owner);
      }

      return amountBalanced == 0;
    }

  }
}
