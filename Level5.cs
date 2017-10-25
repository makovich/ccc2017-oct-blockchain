namespace Blockchain.Level5
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
      var root = "level5/";
      var files = new[]
      {
        "level5-eg.txt",
        "level5-1.txt",
        "level5-2.txt",
        "level5-3.txt",
        "level5-4.txt"
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

      RawBks = lines.Skip(nt + 2)
                    .Select(Bk.Parse)
                    .OrderBy(x => x.Submitted);

      File.WriteAllLines(filepath.Replace(".txt", ".out.txt"), Start());
    }

    IEnumerable<Tx> RawTxs;
    IEnumerable<Bk> RawBks;

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

      public static Tx FromId(string id)
        => new Tx { Id = id.ToId() };
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

    class Bk
    {
      public uint Id;
      public Bk Parent;
      public List<Tx> Txs;
      public long Submitted;
      public static bool IsRoot(Bk b) => b.Id == 0;
      public static Bk Root => new Bk { Id = 0 };
      public static Bk Parse(string line)
      {
        var p = line.Split();
        var nt = p[2].ToInt();

        return new Bk
        {
          Id = p[0].ToId(),
          Parent = new Bk { Id = p[1].ToId() },
          Txs = p.Skip(3).Take(nt).Select(Tx.FromId).ToList(),
          Submitted = p.Last().ToLong()
        };
      }
      public override string ToString()
        => Txs.Count > 0
            ? $"0b{Id.ToString("X8")} 0b{Parent.Id.ToString("X8")} {Txs.Count} {string.Join(" ", Txs.Select(x => "0x" + x.Id.ToString("X8")))} {Submitted}"
            : $"0b{Id.ToString("X8")} 0b{Parent.Id.ToString("X8")} {Txs.Count} {Submitted}";
    }

    IEnumerable<string> Start()
    {
      var txLog = new List<Tx>();
      var bkChain = new List<Bk>();

      foreach (var tx in RawTxs)
        if (IsValid(tx))
          AppendTo(txLog, tx);

      txLog.Clear();
      foreach (var bk in RawBks)
        if (EnsureValid(bk))
          WalkChainInto(ref bkChain, ref txLog, bk);

      yield return txLog.Count.ToString();
      foreach (var item in txLog)
        yield return $"{item}";
      
      yield return bkChain.Count.ToString();
      foreach (var item in bkChain)
        yield return $"{item}";
    }

    List<In> FreeIns = new List<In>();
    Dictionary<uint, Tx> TxIdx = new Dictionary<uint, Tx>();
    Dictionary<uint, Bk> BkIdx = new Dictionary<uint, Bk>{ { 0, Bk.Root } };

    bool EnsureValid(Bk bk)
    {
      if (BkIdx.ContainsKey(bk.Id))
        return true;

      if (!BkIdx.ContainsKey(bk.Parent.Id))
        return false;

      bk.Parent = BkIdx[bk.Parent.Id];

      for (int i = 0; i < bk.Txs.Count; i++)
      {
        if (!TxIdx.ContainsKey(bk.Txs[i].Id))
          return false;
        
        bk.Txs[i] = TxIdx[bk.Txs[i].Id];
      }

      BkIdx[bk.Id] = bk;
      return true;
    }

    void WalkChainInto(ref List<Bk> chain, ref List<Tx> log, Bk bk)
    {
      var b = bk;
      var l = 0;
      var c = new Stack<Bk>();
      var t = new Stack<Tx>();

      while (!Bk.IsRoot(b))
      {
        c.Push(b);
        b.Txs.OrderByDescending(x => x.Submitted).ToList().ForEach(t.Push);
        l += 1;
        b = b.Parent;
      }

      if (l == chain.Count && bk.Submitted < chain.Last().Submitted)
      {
        chain = new List<Bk>(c);
        log = new List<Tx>(t);
      }
      else if (l > chain.Count)
      {
        chain = new List<Bk>(c);
        log = new List<Tx>(t);
      }
    }

    void AppendTo(IList<Tx> log, Tx tx)
    {
      if (log.Count > 0 && log.Last().Submitted > tx.Submitted)
        return;

      log.Add(tx);
      TxIdx.Add(tx.Id, tx);

      foreach (var el in tx.Ins)
        FreeIns.Remove(el);

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
